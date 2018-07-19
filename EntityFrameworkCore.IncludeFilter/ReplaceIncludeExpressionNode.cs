using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.ResultOperators.Internal;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceIncludeExpressionNode : IncludeExpressionNodeBase
    {
        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods = new[]
        {
            QueryableExtensions.IncludeMethodInfo,

            typeof(EntityFrameworkQueryableExtensions)
                .GetTypeInfo().GetDeclaredMethods("Include")
                .Single(mi => mi.GetGenericArguments().Count() == 2
                              && mi.GetParameters()
                                  .Any(pi => pi.Name == "navigationPropertyPath" && pi.ParameterType != typeof(string)))
        };

        private readonly LambdaExpression _filter;

        public ReplaceIncludeExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression navigationPropertyPathLambda, LambdaExpression filter = null) 
            : base(parseInfo, navigationPropertyPathLambda)
        {
            _filter = filter;
        }

        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext)
        {
            var prm = Expression.Parameter(typeof(object));
            var pathFromQuerySource = Resolve(prm, prm, clauseGenerationContext);

            if (!NavigationPropertyPathLambda.TryGetComplexPropertyAccess(out var propertyPath))
            {
                throw new InvalidOperationException(
                    CoreStrings.InvalidIncludeLambdaExpression(
                        nameof(EntityFrameworkQueryableExtensions.Include),
                        NavigationPropertyPathLambda));
            }

            Dictionary<Type, ICollection<LambdaExpression>> filters;

            if (_filter == null)
            {
                filters = new Dictionary<Type, ICollection<LambdaExpression>>();
            }
            else
            {
                var type = _filter.Parameters[0].Type;
                filters = new Dictionary<Type, ICollection<LambdaExpression>>
                {
                    {type, new List<LambdaExpression>() {_filter}}
                };
            }

            var includeResultOperator = new ReplaceIncludeResultOperator(propertyPath.Select(p => p.Name), pathFromQuerySource, filters);
            clauseGenerationContext.AddContextInfo(this, includeResultOperator);
            return includeResultOperator;
        }
    }

    public class ReplaceIncludeResultOperator : IncludeResultOperator
    {
        public KeyValuePair<Type, LambdaExpression> Expression { get; set; }
        public Dictionary<Type, ICollection<LambdaExpression>> Filters { get; set; }

        public ReplaceIncludeResultOperator(IEnumerable<string> navigationPropertyPaths, Expression pathFromQuerySource, Dictionary<Type, ICollection<LambdaExpression>> filters) 
            : base(navigationPropertyPaths, pathFromQuerySource)
        {
            Filters = filters;
        }
    }
}
