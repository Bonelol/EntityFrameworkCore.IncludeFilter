using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Internal;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceIncludeExpressionNode : ResultOperatorExpressionNodeBase
    {
        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods = new[]
        {
            typeof(QueryableExtensions)
                .GetTypeInfo().GetDeclaredMethods("IncludeWithFilter")
                .Single(mi => mi.GetParameters().Any(
                    pi => pi.Name == "navigationPropertyPath" && pi.ParameterType != typeof(string)))
        };

        private readonly LambdaExpression _navigationPropertyPathLambda;
        private readonly Dictionary<string, HashSet<Expression>> _expressions;

        public ReplaceIncludeExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression navigationPropertyPathLambda) : base(parseInfo, null, null)
        {
            _expressions = new Dictionary<string, HashSet<Expression>>();
            _navigationPropertyPathLambda = navigationPropertyPathLambda;
        }

        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext)
        {
            Expression body = _navigationPropertyPathLambda.Body;

            if (_navigationPropertyPathLambda.Body.NodeType == ExpressionType.Extension)
            {
                var sub = (SubQueryExpression)_navigationPropertyPathLambda.Body;
                var whereClause = (WhereClause)sub.QueryModel.BodyClauses.First();
                body = sub.QueryModel.MainFromClause.FromExpression;

                var member = body as MemberExpression;
                if (member != null)
                {
                    var name = $"{member.Member.DeclaringType.FullName}-{member.Member.Name}";
                    _expressions.Add(name, new HashSet<Expression>(new[] { whereClause.Predicate }));
                }
            }

            var navigationPropertyPath = Resolve(_navigationPropertyPathLambda.Parameters[0], body, clauseGenerationContext) as MemberExpression;

            if (navigationPropertyPath == null)
            {
                throw new InvalidOperationException(CoreStrings.InvalidComplexPropertyExpression(_navigationPropertyPathLambda));
            }

            var includeResultOperator = new ReplaceIncludeResultOperator(navigationPropertyPath, _expressions);

            clauseGenerationContext.AddContextInfo(this, includeResultOperator);
            return includeResultOperator;
        }

        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
        {
            return Source.Resolve(inputParameter, expressionToBeResolved, clauseGenerationContext);
        }
    }
}
