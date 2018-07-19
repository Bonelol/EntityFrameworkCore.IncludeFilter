using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using Remotion.Linq.Parsing.ExpressionVisitors;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceModelExpressionApplyingExpressionVisitor : ModelExpressionApplyingExpressionVisitor
    {
        private readonly QueryCompilationContext _queryCompilationContext;
        private readonly IQueryModelGenerator _queryModelGenerator;
        private readonly EntityQueryModelVisitor _entityQueryModelVisitor;

        private readonly Parameters _parameters = new Parameters();

        private IQuerySource _querySource;

        public ReplaceModelExpressionApplyingExpressionVisitor(
            QueryCompilationContext queryCompilationContext,
            IQueryModelGenerator queryModelGenerator,
            EntityQueryModelVisitor entityQueryModelVisitor) : base(queryCompilationContext, queryModelGenerator, entityQueryModelVisitor)
        {
            _queryCompilationContext = queryCompilationContext;
            _queryModelGenerator = queryModelGenerator;
            _entityQueryModelVisitor = entityQueryModelVisitor;
        }

        public virtual bool IsViewTypeQuery { get; private set; }

        private static readonly MethodInfo _whereMethod
            = typeof(Queryable)
                .GetTypeInfo()
                .GetDeclaredMethods(nameof(Queryable.Where))
                .Single(
                    mi => mi.GetParameters().Length == 2
                          && mi.GetParameters()[1].ParameterType
                              .GetGenericArguments()[0]
                              .GetGenericArguments().Length == 2);

        public override void ApplyModelExpressions(QueryModel queryModel)
        {
            _querySource = queryModel.MainFromClause;

            queryModel.TransformExpressions(Visit);
        }

        protected override Expression VisitConstant(ConstantExpression constantExpression)
        {
            if (constantExpression.IsEntityQueryable())
            {
                var type = ((IQueryable)constantExpression.Value).ElementType;
                var entityType = _queryCompilationContext.Model.FindEntityType(type)?.RootType();

                var typeExpressions
                    = _queryCompilationContext.QueryAnnotations
                        .OfType<ReplaceIncludeResultOperator>()
                        .SelectMany(op => op.Filters)
                        .GroupBy(pair => pair.Key)
                        .Select(g =>
                        {
                            var values = g.SelectMany(gg => gg.Value).ToList();
                            return new KeyValuePair<Type, ICollection<LambdaExpression>>(g.Key, values);
                        })
                        .ToDictionary(pair => pair.Key, pair => pair.Value);


                if (entityType != null)
                {
                    Expression newExpression = constantExpression;

                    if (entityType.IsQueryType)
                    {
                        IsViewTypeQuery = true;

                        var query = entityType.DefiningQuery;

                        if (query != null
                            && _entityQueryModelVisitor.ShouldApplyDefiningQuery(entityType, _querySource))
                        {
                            var parameterizedQuery
                                = _queryModelGenerator
                                    .ExtractParameters(
                                        _queryCompilationContext.Logger,
                                        query.Body,
                                        _parameters,
                                        parameterize: false,
                                        generateContextAccessors: true);

                            var subQueryModel = _queryModelGenerator.ParseQuery(Visit(parameterizedQuery));

                            newExpression = new SubQueryExpression(subQueryModel);
                        }
                    }

                    if (!_queryCompilationContext.IgnoreQueryFilters
                        && entityType.QueryFilter != null)
                    {
                        var parameterizedFilter
                            = (LambdaExpression)_queryModelGenerator
                                .ExtractParameters(
                                    _queryCompilationContext.Logger,
                                    entityType.QueryFilter,
                                    _parameters,
                                    parameterize: false,
                                    generateContextAccessors: true);

                        var oldParameterExpression = parameterizedFilter.Parameters[0];
                        var newParameterExpression = Expression.Parameter(type, oldParameterExpression.Name);

                        var predicateExpression
                            = ReplacingExpressionVisitor
                                .Replace(
                                    oldParameterExpression,
                                    newParameterExpression,
                                    Visit(parameterizedFilter.Body));

                        var whereExpression
                            = Expression.Call(
                                _whereMethod.MakeGenericMethod(type),
                                newExpression,
                                Expression.Lambda(
                                    predicateExpression,
                                    newParameterExpression));

                        var subQueryModel = _queryModelGenerator.ParseQuery(whereExpression);

                        newExpression = new SubQueryExpression(subQueryModel);
                    }

                    if (typeExpressions.ContainsKey(type))
                    {
                        foreach (var lambdaExpression in typeExpressions[type])
                        {
                            var parameterizedFilter
                                = (LambdaExpression)_queryModelGenerator
                                    .ExtractParameters(
                                        _queryCompilationContext.Logger,
                                        lambdaExpression,
                                        _parameters,
                                        parameterize: false,
                                        generateContextAccessors: true);

                            var oldParameterExpression = parameterizedFilter.Parameters[0];
                            var newParameterExpression = Expression.Parameter(type, oldParameterExpression.Name);

                            var predicateExpression
                                = ReplacingExpressionVisitor
                                    .Replace(
                                        oldParameterExpression,
                                        newParameterExpression,
                                        Visit(parameterizedFilter.Body));

                            var whereExpression
                                = Expression.Call(
                                    _whereMethod.MakeGenericMethod(type),
                                    newExpression,
                                    Expression.Lambda(
                                        predicateExpression,
                                        newParameterExpression));

                            var subQueryModel = _queryModelGenerator.ParseQuery(whereExpression);

                            newExpression = new SubQueryExpression(subQueryModel);
                        }
                    }

                    return newExpression;
                }
            }

            return constantExpression;
        }

        private sealed class Parameters : IParameterValues
        {
            private readonly IDictionary<string, object> _parameterValues = new Dictionary<string, object>();

            public IReadOnlyDictionary<string, object> ParameterValues
                => (IReadOnlyDictionary<string, object>)_parameterValues;

            public void AddParameter(string name, object value)
            {
                _parameterValues.Add(name, value);
            }

            public object RemoveParameter(string name)
            {
                var value = _parameterValues[name];

                _parameterValues.Remove(name);

                return value;
            }

            public void SetParameter(string name, object value)
            {
                _parameterValues[name] = value;
            }
        }
    }
}
