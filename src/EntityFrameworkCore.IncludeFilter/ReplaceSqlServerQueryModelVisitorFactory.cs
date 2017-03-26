using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EntityFrameworkCore.IncludeFilter
{
    class ReplaceSqlServerQueryModelVisitorFactory : SqlServerQueryModelVisitorFactory
    {
        public INavigationExpressionCollection ExpressionCollection { get; set; }

        public ReplaceSqlServerQueryModelVisitorFactory(IQueryOptimizer queryOptimizer
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
            , INavigationExpressionCollection collection) : base(queryOptimizer, navigationRewritingExpressionVisitorFactory, subQueryMemberPushDownExpressionVisitor, querySourceTracingExpressionVisitorFactory, entityResultFindingExpressionVisitorFactory, taskBlockingExpressionVisitor, memberAccessBindingExpressionVisitorFactory, orderingExpressionVisitorFactory, projectionExpressionVisitorFactory, entityQueryableExpressionVisitorFactory, queryAnnotationExtractor, resultOperatorHandler, entityMaterializerSource, expressionPrinter, relationalAnnotationProvider, includeExpressionVisitorFactory, sqlTranslatingExpressionVisitorFactory, compositePredicateExpressionVisitorFactory, conditionalRemovingExpressionVisitorFactory, queryFlattenerFactory, contextOptions)
        {
            ExpressionCollection = collection;
        }

        public override EntityQueryModelVisitor Create(
            QueryCompilationContext queryCompilationContext,
            EntityQueryModelVisitor parentEntityQueryModelVisitor)
            =>
                new ReplaceSqlServerQueryModelVisitor(
                    QueryOptimizer,
                    NavigationRewritingExpressionVisitorFactory,
                    SubQueryMemberPushDownExpressionVisitor,
                    QuerySourceTracingExpressionVisitorFactory,
                    EntityResultFindingExpressionVisitorFactory,
                    TaskBlockingExpressionVisitor,
                    MemberAccessBindingExpressionVisitorFactory,
                    OrderingExpressionVisitorFactory,
                    ProjectionExpressionVisitorFactory,
                    EntityQueryableExpressionVisitorFactory,
                    QueryAnnotationExtractor,
                    ResultOperatorHandler,
                    EntityMaterializerSource,
                    ExpressionPrinter,
                    RelationalAnnotationProvider,
                    IncludeExpressionVisitorFactory,
                    SqlTranslatingExpressionVisitorFactory,
                    CompositePredicateExpressionVisitorFactory,
                    ConditionalRemovingExpressionVisitorFactory,
                    QueryFlattenerFactory,
                    ContextOptions,
                    (RelationalQueryCompilationContext)queryCompilationContext,
                    (SqlServerQueryModelVisitor)parentEntityQueryModelVisitor,
                    ExpressionCollection);
    }
}
