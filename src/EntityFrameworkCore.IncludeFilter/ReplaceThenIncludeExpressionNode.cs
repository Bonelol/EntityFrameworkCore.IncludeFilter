using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.ResultOperators.Internal;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceThenIncludeExpressionNode : ResultOperatorExpressionNodeBase
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods = new[]
        {
            QueryableExtensions.ThenIncludeAfterEnumerableMethodInfo,
            //EntityFrameworkQueryableExtensions.ThenIncludeAfterReferenceMethodInfo
        };

        private readonly LambdaExpression _navigationPropertyPathLambda;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public ReplaceThenIncludeExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression navigationPropertyPathLambda)
            : base(parseInfo, null, null)
        {
            _navigationPropertyPathLambda = navigationPropertyPathLambda;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
        {
            var includeResultOperator
                = (IncludeResultOperator)clauseGenerationContext.GetContextInfo(Source);

            includeResultOperator
                .AppendToNavigationPath(_navigationPropertyPathLambda.GetComplexPropertyAccess());

            clauseGenerationContext.AddContextInfo(this, includeResultOperator);
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext)
            => null;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
        {
            return Source.Resolve(inputParameter, expressionToBeResolved, clauseGenerationContext);
        }

        class PrivateIncludeExpressionVisitor : ExpressionVisitor
        {
            private readonly Type _returnType;
            private HashSet<Expression> Expressions { get; }

            public PrivateIncludeExpressionVisitor(Type returnType)
            {
                _returnType = returnType;
                Expressions = new HashSet<Expression>();
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.Name == "ThenIncludeWithFilter")
                {
                    var where = new PrivateIncludeWhereExpressionVisitor(_returnType);
                    where.Visit(node);

                    Expressions.UnionWith(where.Expressions); ;

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
