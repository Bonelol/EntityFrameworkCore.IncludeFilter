using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Remotion.Linq.Clauses;

namespace EntityFrameworkCore.IncludeFilter
{
    class ReplaceIncludeExpressionVisitorFactory : IIncludeExpressionVisitorFactory
    {
        private readonly ISelectExpressionFactory _selectExpressionFactory;
        private readonly ICompositePredicateExpressionVisitorFactory _compositePredicateExpressionVisitorFactory;
        private readonly IMaterializerFactory _materializerFactory;
        private readonly IShaperCommandContextFactory _shaperCommandContextFactory;
        private readonly IRelationalAnnotationProvider _relationalAnnotationProvider;
        private readonly IQuerySqlGeneratorFactory _querySqlGeneratorFactory;
        private readonly INavigationExpressionCollection _collection;


        public ReplaceIncludeExpressionVisitorFactory(ISelectExpressionFactory selectExpressionFactory
            , ICompositePredicateExpressionVisitorFactory compositePredicateExpressionVisitorFactory
            , IMaterializerFactory materializerFactory
            , IShaperCommandContextFactory shaperCommandContextFactory
            , IRelationalAnnotationProvider relationalAnnotationProvider
            , IQuerySqlGeneratorFactory querySqlGeneratorFactory
            , INavigationExpressionCollection collection)
        {
            _selectExpressionFactory = selectExpressionFactory;
            _compositePredicateExpressionVisitorFactory = compositePredicateExpressionVisitorFactory;
            _materializerFactory = materializerFactory;
            _shaperCommandContextFactory = shaperCommandContextFactory;
            _relationalAnnotationProvider = relationalAnnotationProvider;
            _querySqlGeneratorFactory = querySqlGeneratorFactory;
            _collection = collection;
        }

        public ExpressionVisitor Create(IQuerySource querySource, IReadOnlyList<INavigation> navigationPath, RelationalQueryCompilationContext relationalQueryCompilationContext, IReadOnlyList<int> queryIndexes,
            bool querySourceRequiresTracking)
        {
            return new ReplaceIncludeExpressionVisitor(_selectExpressionFactory,
                _compositePredicateExpressionVisitorFactory,
                _materializerFactory,
                _shaperCommandContextFactory,
                _relationalAnnotationProvider,
                _querySqlGeneratorFactory,
                querySource,
                navigationPath,
                relationalQueryCompilationContext,
                queryIndexes,
                querySourceRequiresTracking,
                _collection);
        }
    }
}
