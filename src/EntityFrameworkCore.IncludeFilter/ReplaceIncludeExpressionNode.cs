using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing.Structure;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceIncludeExpressionNode : ResultOperatorExpressionNodeBase
    {
        /// <summary>
        ///     Add ef support for our IncludeWithFilter method
        /// </summary>
        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods = new[]
        {
            typeof(QueryableExtensions)
                .GetTypeInfo().GetDeclaredMethods("IncludeWithFilter")
                .Single(mi => mi.GetParameters().Any(
                    pi => pi.Name == "navigationPropertyPath" && pi.ParameterType != typeof(string)))
        };

        private readonly LambdaExpression _navigationPropertyPathLambda;
        private readonly HashSet<Expression> _expressions;

        public ReplaceIncludeExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression navigationPropertyPathLambda) : base(parseInfo, null, null)
        {
            var visitor = new PrivateIncludeExpressionVisitor(navigationPropertyPathLambda.ReturnType);
            visitor.Visit(parseInfo.ParsedExpression);
            _expressions = new HashSet<Expression>();
            //_expressions = visitor.Expressions;
            _navigationPropertyPathLambda = navigationPropertyPathLambda;
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext)
        {
            Expression body = _navigationPropertyPathLambda.Body;

            if (_navigationPropertyPathLambda.Body.NodeType == ExpressionType.Extension)
            {
                var sub = (SubQueryExpression)_navigationPropertyPathLambda.Body;
                body = sub.QueryModel.MainFromClause.FromExpression;

                var whereClause = (WhereClause) sub.QueryModel.BodyClauses.First();
                _expressions.Add(whereClause.Predicate);

                //var ee = (QuerySourceReferenceExpression) ((UnaryExpression) whereClause.Predicate).Operand;
                //var querySource = (MainFromClause) ee.ReferencedQuerySource;
                //var newSource = Activator.CreateInstance(typeof(EntityQueryable<>).MakeGenericType(querySource.ItemType));
                //querySource.FromExpression = Expression.Constant(newSource);
            }

            var navigationPropertyPath = Resolve(_navigationPropertyPathLambda.Parameters[0], body, clauseGenerationContext) as MemberExpression;

            if (navigationPropertyPath == null)
            {
                throw new InvalidOperationException(
                    CoreStrings.InvalidComplexPropertyExpression(_navigationPropertyPathLambda));
            }

            var includeResultOperator = new ReplaceIncludeResultOperator(navigationPropertyPath, _expressions);

            clauseGenerationContext.AddContextInfo(this, includeResultOperator);
            return includeResultOperator;
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
        {
            return Source.Resolve(inputParameter, expressionToBeResolved, clauseGenerationContext);
        }

        class PrivateIncludeExpressionVisitor : ExpressionVisitor
        {
            private readonly Type _returnType;
            public HashSet<Expression> Expressions { get; }

            public PrivateIncludeExpressionVisitor(Type returnType)
            {
                _returnType = returnType;
                Expressions = new HashSet<Expression>();
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.Name == "IncludeWithFilter")
                {
                    var where = new PrivateIncludeWhereExpressionVisitor(_returnType);
                    where.Visit(node);

                    Expressions.UnionWith(where.Expressions);;

                    return node;
                }
                return base.VisitMethodCall(node);
            }
        }

        class PrivateIncludeWhereExpressionVisitor : ExpressionVisitor
        {
            private readonly Type _returnType;
            public HashSet<Expression> Expressions { get; }

            public PrivateIncludeWhereExpressionVisitor(Type returnType)
            {
                _returnType = returnType;
                Expressions = new HashSet<Expression>();
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.Name == "Where" && node.Type == _returnType)
                {
                    Expressions.Add(node.Arguments[1]);
                }
                return base.VisitMethodCall(node);
            }
        }
    }
}
