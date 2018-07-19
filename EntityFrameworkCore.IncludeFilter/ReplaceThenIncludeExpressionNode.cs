using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.ResultOperators.Internal;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceThenIncludeExpressionNode : IncludeExpressionNodeBase
    {
        private readonly LambdaExpression _filter;

        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods = new[]
        {
            QueryableExtensions.ThenIncludeAfterReferenceMethodInfo,
            QueryableExtensions.ThenIncludeAfterEnumerableMethodInfo
        };

        public ReplaceThenIncludeExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression navigationPropertyPathLambda, LambdaExpression filter)
            : base(parseInfo, navigationPropertyPathLambda)
        {
            _filter = filter;
        }

        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
        {
            var includeResultOperator
                = (IncludeResultOperator)clauseGenerationContext.GetContextInfo(Source);

            if (!NavigationPropertyPathLambda.TryGetComplexPropertyAccess(out var propertyPath))
            {
                throw new InvalidOperationException(
                    CoreStrings.InvalidIncludeLambdaExpression(
                        nameof(EntityFrameworkQueryableExtensions.ThenInclude),
                        NavigationPropertyPathLambda));
            }

            if (includeResultOperator is ReplaceIncludeResultOperator replace)
            {
                var type = _filter.Parameters[0].Type;

                if (!replace.Filters.ContainsKey(type))
                {
                    replace.Filters.Add(type, new List<LambdaExpression>());
                }

                replace.Filters[type].Add(_filter);
            }

            includeResultOperator.AppendToNavigationPath(propertyPath);

            clauseGenerationContext.AddContextInfo(this, includeResultOperator);
        }

        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext)
            => null;
    }
}
