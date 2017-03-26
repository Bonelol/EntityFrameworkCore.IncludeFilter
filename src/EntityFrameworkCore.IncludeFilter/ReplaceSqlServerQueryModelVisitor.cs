﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.StreamedData;

namespace EntityFrameworkCore.IncludeFilter
{
    class ReplaceSqlServerQueryModelVisitor : SqlServerQueryModelVisitor
    {
        public INavigationExpressionCollection ExpressionCollection { get; set; }
        private ISqlTranslatingExpressionVisitorFactory _sqlTranslatingExpressionVisitorFactory;

        public ReplaceSqlServerQueryModelVisitor(IQueryOptimizer queryOptimizer
            , INavigationRewritingExpressionVisitorFactory navigationRewritingExpressionVisitorFactory
            , ISubQueryMemberPushDownExpressionVisitor subQueryMemberPushDownExpressionVisitor
            , IQuerySourceTracingExpressionVisitorFactory querySourceTracingExpressionVisitorFactory
            , IEntityResultFindingExpressionVisitorFactory entityResultFindingExpressionVisitorFactory
            , ITaskBlockingExpressionVisitor taskBlockingExpressionVisitor
            , IMemberAccessBindingExpressionVisitorFactory memberAccessBindingExpressionVisitorFactory
            , IOrderingExpressionVisitorFactory orderingExpressionVisitorFactory
            , IProjectionExpressionVisitorFactory projectionExpressionVisitorFactory
            , IEntityQueryableExpressionVisitorFactory entityQueryableExpressionVisitorFactory
            , IQueryAnnotationExtractor queryAnnotationExtractor
            , IResultOperatorHandler resultOperatorHandler
            , IEntityMaterializerSource entityMaterializerSource
            , IExpressionPrinter expressionPrinter
            , IRelationalAnnotationProvider relationalAnnotationProvider
            , IIncludeExpressionVisitorFactory includeExpressionVisitorFactory
            , ISqlTranslatingExpressionVisitorFactory sqlTranslatingExpressionVisitorFactory
            , ICompositePredicateExpressionVisitorFactory compositePredicateExpressionVisitorFactory
            , IConditionalRemovingExpressionVisitorFactory conditionalRemovingExpressionVisitorFactory
            , IQueryFlattenerFactory queryFlattenerFactory
            , IDbContextOptions contextOptions
            , RelationalQueryCompilationContext queryCompilationContext
            , SqlServerQueryModelVisitor parentQueryModelVisitor
            , INavigationExpressionCollection collection) : base(queryOptimizer, navigationRewritingExpressionVisitorFactory, subQueryMemberPushDownExpressionVisitor, querySourceTracingExpressionVisitorFactory, entityResultFindingExpressionVisitorFactory, taskBlockingExpressionVisitor, memberAccessBindingExpressionVisitorFactory, orderingExpressionVisitorFactory, projectionExpressionVisitorFactory, entityQueryableExpressionVisitorFactory, queryAnnotationExtractor, resultOperatorHandler, entityMaterializerSource, expressionPrinter, relationalAnnotationProvider, includeExpressionVisitorFactory, sqlTranslatingExpressionVisitorFactory, compositePredicateExpressionVisitorFactory, conditionalRemovingExpressionVisitorFactory, queryFlattenerFactory, contextOptions, queryCompilationContext, parentQueryModelVisitor)
        {
            ExpressionCollection = collection;
            _sqlTranslatingExpressionVisitorFactory = sqlTranslatingExpressionVisitorFactory;
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        protected override void IncludeNavigations(QueryModel queryModel)
        {
            if (queryModel.GetOutputDataInfo() is StreamedScalarValueInfo)
            {
                return;
            }

            var includeSpecifications
                = QueryCompilationContext.QueryAnnotations
                    .OfType<ReplaceIncludeResultOperator>()
                    .Select(includeResultOperator =>
                    {
                        var navigationPath
                            = BindNavigationPathPropertyExpression(
                                includeResultOperator.NavigationPropertyPath,
                                (ps, _) =>
                                {
                                    var properties = ps.ToArray();
                                    var navigations = properties.OfType<INavigation>().ToArray();

                                    if (properties.Length != navigations.Length)
                                    {
                                        throw new InvalidOperationException(
                                                CoreStrings.IncludeNonBindableExpression(
                                                    includeResultOperator.NavigationPropertyPath));
                                    }

                                    return BindChainedNavigations2(
                                            navigations,
                                            includeResultOperator)
                                            .ToArray();
                                });

                        if (navigationPath == null)
                        {
                            throw new InvalidOperationException(
                                CoreStrings.IncludeNonBindableExpression(
                                    includeResultOperator.NavigationPropertyPath));
                        }

                        return new
                        {
                            specification = new IncludeSpecification(includeResultOperator.QuerySource, navigationPath),
                            order = string.Concat(navigationPath.Select(n => n.IsCollection() ? "1" : "0"))
                        };
                    })
                    .OrderByDescending(e => e.order)
                    .ThenBy(e => e.specification.NavigationPath.First().IsDependentToPrincipal())
                    .Select(e => e.specification)
                    .ToList();

            IncludeNavigations(queryModel, includeSpecifications);
        }

        /// <summary>
        ///    Copyed from EntityFramework Core source code
        /// </summary>
        private IEnumerable<INavigation> BindChainedNavigations2(
            IEnumerable<INavigation> boundNavigations, ReplaceIncludeResultOperator includeResultOperator)
        {
            var boundNavigationsList = boundNavigations.ToList();

            if (includeResultOperator.ChainedNavigationProperties != null)
            {
                foreach (var propertyInfo in includeResultOperator.ChainedNavigationProperties)
                {
                    var lastNavigation = boundNavigationsList.Last();
                    var navigation = lastNavigation.GetTargetType().FindNavigation(propertyInfo.Name);

                    if (navigation == null)
                    {
                        return null;
                    }
                    boundNavigationsList.Add(navigation);
                }
            }

            // match expressions to navigation
            var f = boundNavigationsList.First();
            var expressions = includeResultOperator.Expressions;

            var expr = expressions.FirstOrDefault();

            if (expr != null)
            {
                var visitor = _sqlTranslatingExpressionVisitorFactory.Create(this);
                var translated = visitor.Visit(expr);

                this.ExpressionCollection[f] = new List<Expression> {translated};
            }

            if (ExpressionCollection.HasKey(f))
            {
                this.ExpressionCollection[f] = expressions;
            }
            else
            {
                this.ExpressionCollection.Add(f, expressions);
            }

            return boundNavigationsList;
        }

        public override SelectExpression TryGetQuery(IQuerySource querySource)
        {
            SelectExpression selectExpression;
            if (QueriesBySource.TryGetValue(querySource, out selectExpression))
                return selectExpression;
            return QueriesBySource.Values.SingleOrDefault(se => se.HandlesQuerySource(querySource));
        }
    }
}
