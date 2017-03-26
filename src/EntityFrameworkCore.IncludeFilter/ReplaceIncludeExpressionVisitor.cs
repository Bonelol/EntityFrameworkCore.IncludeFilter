using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Extensions.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Utilities;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing.Structure;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceIncludeExpressionVisitor : ExpressionVisitorBase
    {
        private readonly ISelectExpressionFactory _selectExpressionFactory;
        private readonly ICompositePredicateExpressionVisitorFactory _compositePredicateExpressionVisitorFactory;
        private readonly IMaterializerFactory _materializerFactory;
        private readonly IShaperCommandContextFactory _shaperCommandContextFactory;
        private readonly IRelationalAnnotationProvider _relationalAnnotationProvider;
        private readonly IQuerySqlGeneratorFactory _querySqlGeneratorFactory;
        private readonly IQuerySource _querySource;
        private readonly IReadOnlyList<INavigation> _navigationPath;
        private readonly RelationalQueryCompilationContext _queryCompilationContext;
        private readonly IReadOnlyList<int> _queryIndexes;
        private readonly bool _querySourceRequiresTracking;
        private readonly INavigationExpressionCollection _collection;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public ReplaceIncludeExpressionVisitor(
            ISelectExpressionFactory selectExpressionFactory,
            ICompositePredicateExpressionVisitorFactory compositePredicateExpressionVisitorFactory,
            IMaterializerFactory materializerFactory,
            IShaperCommandContextFactory shaperCommandContextFactory,
            IRelationalAnnotationProvider relationalAnnotationProvider,
            IQuerySqlGeneratorFactory querySqlGeneratorFactory,
            IQuerySource querySource,
            IReadOnlyList<INavigation> navigationPath,
            RelationalQueryCompilationContext queryCompilationContext,
            IReadOnlyList<int> queryIndexes,
            bool querySourceRequiresTracking, 
            INavigationExpressionCollection collection)
        {
            _selectExpressionFactory = selectExpressionFactory;
            _compositePredicateExpressionVisitorFactory = compositePredicateExpressionVisitorFactory;
            _materializerFactory = materializerFactory;
            _shaperCommandContextFactory = shaperCommandContextFactory;
            _relationalAnnotationProvider = relationalAnnotationProvider;
            _querySqlGeneratorFactory = querySqlGeneratorFactory;
            _querySource = querySource;
            _navigationPath = navigationPath;
            _queryCompilationContext = queryCompilationContext;
            _queryIndexes = queryIndexes;
            _querySourceRequiresTracking = querySourceRequiresTracking;
            _collection = collection;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.MethodIsClosedFormOf(
                _queryCompilationContext.QueryMethodProvider.ShapedQueryMethod))
            {
                var shaper
                    = ((ConstantExpression)methodCallExpression.Arguments[2]).Value
                        as Shaper;

                if (shaper != null
                    && shaper.IsShaperForQuerySource(_querySource))
                {
                    var resultType = methodCallExpression.Method.GetGenericArguments()[0];
                    var entityAccessor = shaper.GetAccessorExpression(_querySource);

                    //var provider = new QueryMethodProvider2();

                    var expression =
                        Expression.Call(
                            _queryCompilationContext.QueryMethodProvider.IncludeMethod.MakeGenericMethod(resultType),
                            Expression.Convert(methodCallExpression.Arguments[0], typeof(RelationalQueryContext)),
                            methodCallExpression,
                            entityAccessor,
                            Expression.Constant(_navigationPath),
                            Expression.Constant(
                                _createRelatedEntitiesLoadersMethodInfo
                                    .MakeGenericMethod(_queryCompilationContext.QueryMethodProvider.RelatedEntitiesLoaderType)
                                    .Invoke(this, new object[] { _querySource, _navigationPath })),
                            Expression.Constant(_querySourceRequiresTracking));

                    return expression;
                }
            }
            else if (methodCallExpression.Method.MethodIsClosedFormOf(
                _queryCompilationContext.QueryMethodProvider.GroupJoinMethod))
            {
                var newMethodCallExpression = TryMatchGroupJoinShaper(methodCallExpression, 2);

                if (!ReferenceEquals(methodCallExpression, newMethodCallExpression))
                {
                    return newMethodCallExpression;
                }

                newMethodCallExpression = TryMatchGroupJoinShaper(methodCallExpression, 3);

                if (!ReferenceEquals(methodCallExpression, newMethodCallExpression))
                {
                    return newMethodCallExpression;
                }
            }

            return base.VisitMethodCall(methodCallExpression);
        }

        private Expression TryMatchGroupJoinShaper(MethodCallExpression methodCallExpression, int shaperArgumentIndex)
        {
            var shaper
                = ((ConstantExpression)methodCallExpression.Arguments[shaperArgumentIndex]).Value
                    as Shaper;

            if (shaper != null
                && shaper.IsShaperForQuerySource(_querySource))
            {
                var groupJoinIncludeArgumentIndex = shaperArgumentIndex + 4;

                var groupJoinInclude
                    = _queryCompilationContext.QueryMethodProvider
                        .CreateGroupJoinInclude(
                            _navigationPath,
                            _querySourceRequiresTracking,
                            (methodCallExpression.Arguments[groupJoinIncludeArgumentIndex] as ConstantExpression)?.Value,
                            _createRelatedEntitiesLoadersMethodInfo
                                .MakeGenericMethod(_queryCompilationContext.QueryMethodProvider.RelatedEntitiesLoaderType)
                                .Invoke(this, new object[]
                                {
                                    _querySource,
                                    _navigationPath
                                }));

                if (groupJoinInclude != null)
                {
                    var newArguments = methodCallExpression.Arguments.ToList();

                    newArguments[groupJoinIncludeArgumentIndex] = Expression.Constant(groupJoinInclude);

                    return methodCallExpression.Update(methodCallExpression.Object, newArguments);
                }
            }

            return methodCallExpression;
        }

        private static readonly MethodInfo _createRelatedEntitiesLoadersMethodInfo
            = typeof(ReplaceIncludeExpressionVisitor).GetTypeInfo()
                .GetDeclaredMethod(nameof(CreateRelatedEntitiesLoaders));

        private IReadOnlyList<Func<QueryContext, TRelatedEntitiesLoader>> CreateRelatedEntitiesLoaders<TRelatedEntitiesLoader>(
            IQuerySource querySource, IEnumerable<INavigation> navigationPath)
        {
            var relatedEntitiesLoaders = new List<Func<QueryContext, TRelatedEntitiesLoader>>();

            var selectExpression
                = _queryCompilationContext.FindSelectExpression(querySource);

            var compositePredicateExpressionVisitor
                = _compositePredicateExpressionVisitorFactory.Create();

            var targetTableExpression
                = selectExpression.GetTableForQuerySource(querySource);

            var canProduceInnerJoin = true;
            var navigationCount = 0;

            foreach (var navigation in navigationPath)
            {
                var queryIndex = _queryIndexes[navigationCount];
                navigationCount++;

                var targetEntityType = navigation.GetTargetType();
                var targetTableName = _relationalAnnotationProvider.For(targetEntityType).TableName;
                var targetTableAlias
                    = _queryCompilationContext
                        .CreateUniqueTableAlias(targetTableName[0].ToString().ToLowerInvariant());

                if (!navigation.IsCollection())
                {
                    var joinedTableExpression
                        = new TableExpression(
                            targetTableName,
                            _relationalAnnotationProvider.For(targetEntityType).Schema,
                            targetTableAlias,
                            querySource);

                    var valueBufferOffset = selectExpression.Projection.Count;

                    canProduceInnerJoin
                        = canProduceInnerJoin
                          && navigation.ForeignKey.IsRequired
                          && navigation.IsDependentToPrincipal();

                    var joinExpression
                        = canProduceInnerJoin
                            ? selectExpression.AddInnerJoin(joinedTableExpression)
                            : selectExpression.AddLeftOuterJoin(joinedTableExpression);

                    var oldPredicate = selectExpression.Predicate;

                    var materializer
                        = _materializerFactory
                            .CreateMaterializer(
                                targetEntityType,
                                selectExpression,
                                (p, se) => se.AddToProjection(
                                    new AliasExpression(
                                        new ColumnExpression(
                                            _relationalAnnotationProvider.For(p).ColumnName,
                                            p,
                                            joinedTableExpression))) - valueBufferOffset,
                                querySource: null);

                    if (selectExpression.Predicate != oldPredicate)
                    {
                        selectExpression.Predicate
                            = compositePredicateExpressionVisitor
                                .Visit(selectExpression.Predicate);

                        var newJoinExpression = AdjustJoinExpression(selectExpression, joinExpression);

                        selectExpression.Predicate = oldPredicate;
                        selectExpression.RemoveTable(joinExpression);
                        selectExpression.AddTable(newJoinExpression, createUniqueAlias: false);
                        joinExpression = newJoinExpression;
                    }

                    joinExpression.Predicate
                        = BuildJoinEqualityExpression(
                            navigation,
                            navigation.IsDependentToPrincipal() ? targetTableExpression : joinExpression,
                            navigation.IsDependentToPrincipal() ? joinExpression : targetTableExpression,
                            querySource);

                    targetTableExpression = joinedTableExpression;

                    relatedEntitiesLoaders.Add(qc =>
                        (TRelatedEntitiesLoader)_queryCompilationContext.QueryMethodProvider
                            .CreateReferenceRelatedEntitiesLoaderMethod
                            .Invoke(
                                null,
                                new object[]
                                {
                                    valueBufferOffset,
                                    queryIndex,
                                    materializer.Compile() // TODO: Used cached materializer?
                                }));
                }
                else
                {
                    var principalTable
                        = selectExpression.Tables.Count == 1
                          && selectExpression.Tables
                              .OfType<SelectExpression>()
                              .Any(s => s.Tables.Any(t => t.QuerySource == querySource))
                            // true when select is wrapped e.g. when RowNumber paging is enabled
                            ? selectExpression.Tables[0]
                            : selectExpression.Tables.Last(t => t.QuerySource == querySource);

                    var canGenerateExists
                        = (selectExpression.Predicate != null
                          || selectExpression.Offset == null)
                          && !IsOrderingOnNonPrincipalKeyProperties(
                              selectExpression.OrderBy,
                              navigation.ForeignKey.PrincipalKey.Properties);

                    foreach (var property in navigation.ForeignKey.PrincipalKey.Properties)
                    {
                        selectExpression
                            .AddToOrderBy(
                                _relationalAnnotationProvider.For(property).ColumnName,
                                property,
                                principalTable,
                                OrderingDirection.Asc);
                    }

                    var targetSelectExpression = _selectExpressionFactory.Create(_queryCompilationContext);

                    targetTableExpression
                        = new TableExpression(
                            targetTableName,
                            _relationalAnnotationProvider.For(targetEntityType).Schema,
                            targetTableAlias,
                            querySource);

                    targetSelectExpression.AddTable(targetTableExpression, createUniqueAlias: false);

                    var materializer
                        = _materializerFactory
                            .CreateMaterializer(
                                targetEntityType,
                                targetSelectExpression,
                                (p, se) => se.AddToProjection(
                                    _relationalAnnotationProvider.For(p).ColumnName,
                                    p,
                                    querySource),
                                querySource: null);

                    if (canGenerateExists)
                    {
                        var subqueryExpression = selectExpression.Clone();
                        subqueryExpression.ClearProjection();
                        subqueryExpression.ClearOrderBy();
                        subqueryExpression.IsProjectStar = false;

                        var subqueryTable
                            = subqueryExpression.Tables.Count == 1
                              && subqueryExpression.Tables
                                  .OfType<SelectExpression>()
                                  .Any(s => s.Tables.Any(t => t.QuerySource == querySource))
                                // true when select is wrapped e.g. when RowNumber paging is enabled
                                ? subqueryExpression.Tables[0]
                                : subqueryExpression.Tables.Last(t => t.QuerySource == querySource);

                        var existsPredicateExpression = new ExistsExpression(subqueryExpression);

                        AddToPredicate(targetSelectExpression, existsPredicateExpression);

                        var p = _collection.TryGet(navigation);

                        var alias = TryParseExpression(targetTableExpression, targetEntityType, p);

                        AddToPredicate(targetSelectExpression, alias);
                        AddToPredicate(subqueryExpression, BuildJoinEqualityExpression(navigation, targetTableExpression, subqueryTable, querySource));

                        subqueryExpression.Predicate
                            = compositePredicateExpressionVisitor
                                .Visit(subqueryExpression.Predicate);

                        var pkPropertiesToFkPropertiesMap = navigation.ForeignKey.PrincipalKey.Properties
                            .Zip(navigation.ForeignKey.Properties, (k, v) => new { PkProperty = k, FkProperty = v })
                            .ToDictionary(x => x.PkProperty, x => x.FkProperty);

                        foreach (var ordering in selectExpression.OrderBy)
                        {
                            // ReSharper disable once PossibleNullReferenceException
                            var principalKeyProperty = ((ordering.Expression as AliasExpression)?.Expression as ColumnExpression).Property;
                            var referencedForeignKeyProperty = pkPropertiesToFkPropertiesMap[principalKeyProperty];
                            targetSelectExpression
                                .AddToOrderBy(
                                    _relationalAnnotationProvider.For(referencedForeignKeyProperty).ColumnName,
                                    referencedForeignKeyProperty,
                                    targetTableExpression,
                                    ordering.OrderingDirection);
                        }
                    }
                    else
                    {
                        var innerJoinSelectExpression
                            = selectExpression.Clone(
                                selectExpression.OrderBy
                                    .Select(o => o.Expression)
                                    .Last(o => o.IsAliasWithColumnExpression())
                                    .TryGetColumnExpression().TableAlias);

                        innerJoinSelectExpression.ClearProjection();

                        var innerJoinExpression = targetSelectExpression.AddInnerJoin(innerJoinSelectExpression);

                        LiftOrderBy(innerJoinSelectExpression, targetSelectExpression, innerJoinExpression);

                        innerJoinSelectExpression.IsDistinct = true;

                        innerJoinExpression.Predicate
                            = BuildJoinEqualityExpression(
                                navigation,
                                targetTableExpression,
                                innerJoinExpression,
                                querySource);
                    }

                    ////add filter
                    //var expressions = _collection.TryGet(navigation);

                    //if (expressions != null)
                    //{
                    //    var expression = expressions.First();
                    //    targetSelectExpression.Predicate = expression;// Expression.AndAlso(targetSelectExpression.Predicate, expression);
                    //}

                    targetSelectExpression.Predicate
                        = compositePredicateExpressionVisitor
                            .Visit(targetSelectExpression.Predicate);

                    selectExpression = targetSelectExpression;

                    relatedEntitiesLoaders.Add(qc =>
                        (TRelatedEntitiesLoader)_queryCompilationContext.QueryMethodProvider
                            .CreateCollectionRelatedEntitiesLoaderMethod
                            .Invoke(
                                null,
                                new object[]
                                {
                                    qc,
                                    _shaperCommandContextFactory.Create(() =>
                                        _querySqlGeneratorFactory.CreateDefault(targetSelectExpression)),
                                    queryIndex,
                                    materializer.Compile() // TODO: Used cached materializer?
                                }));
                }
            }

            return relatedEntitiesLoaders;
        }

        private Expression TryParseExpression(TableExpressionBase table, IEntityType entityType,  ICollection<Expression> expressions)
        {
            var expression = expressions.FirstOrDefault();

            if (expression != null)
            {
                var visitor = new PrivateParseExpressionVisitor(table, entityType);
                return visitor.Visit(expression);
            }

            var column = new ColumnExpression("Active", typeof(bool), table);
            var alias = new AliasExpression(column);

            return alias;
        }

        class PrivateParseExpressionVisitor : ExpressionVisitor
        {
            private readonly IEntityType _entityType;
            private readonly TableExpressionBase _table;
            private readonly IEnumerable<IProperty> _properties;

            public PrivateParseExpressionVisitor(TableExpressionBase table, IEntityType entityType)
            {
                _table = table;
                _entityType = entityType;
                _properties = _entityType.GetProperties();
            }

            public override Expression Visit(Expression node)
            {
                switch (node.NodeType)
                {
                    case ExpressionType.Add:
                        break;
                    case ExpressionType.AddAssign:
                        break;
                    case ExpressionType.And:
                        break;
                    case ExpressionType.AndAlso:
                        break;
                    case ExpressionType.Conditional:
                        break;
                    case ExpressionType.Constant:
                        break;
                    case ExpressionType.Divide:
                        break;
                    case ExpressionType.DivideAssign:
                        break;
                    case ExpressionType.Dynamic:
                        break;
                    case ExpressionType.Equal:
                        break;
                    case ExpressionType.GreaterThan:
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        break;
                    case ExpressionType.LessThan:
                        break;
                    case ExpressionType.LessThanOrEqual:
                        break;
                    case ExpressionType.MemberAccess:
                        break;
                    case ExpressionType.Multiply:
                        break;
                    case ExpressionType.MultiplyAssign:
                        break;
                    case ExpressionType.Negate:
                        break;
                    case ExpressionType.NegateChecked:
                        break;
                    case ExpressionType.Not:
                        break;
                    case ExpressionType.NotEqual:
                        break;
                    case ExpressionType.Or:
                        break;
                    case ExpressionType.OrElse:
                        break;
                    case ExpressionType.UnaryPlus:
                        break;
                }

                return base.Visit(node);
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                var left = Visit(node.Left);
                var right = Visit(node.Right);

                switch (node.NodeType)
                {
                    case ExpressionType.AndAlso:
                        return Expression.AndAlso(left, right);
                    case ExpressionType.Equal:
                        return Expression.Equal(left, right);
                    case ExpressionType.GreaterThan:
                        return Expression.GreaterThan(left, right);
                    case ExpressionType.GreaterThanOrEqual:
                        return Expression.GreaterThanOrEqual(left, right);
                    case ExpressionType.LessThan:
                        return Expression.LessThan(left, right);
                    case ExpressionType.LessThanOrEqual:
                        return Expression.LessThanOrEqual(left, right);
                    case ExpressionType.NotEqual:
                        return Expression.NotEqual(left, right);
                    case ExpressionType.OrElse:
                        return Expression.OrElse(left, right);
                }

                throw new NotImplementedException();
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                var property = _properties.First(p => p.Name == node.Member.Name);
                var columnAttr = node.Member.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(ColumnAttribute));
                var columnName = columnAttr?.ConstructorArguments[0].Value.ToString() ?? node.Member.Name;
                var column = new ColumnExpression(columnName, property, _table);
                return new AliasExpression(column);
            }
        }

        private JoinExpressionBase AdjustJoinExpression(
            SelectExpression selectExpression, JoinExpressionBase joinExpression)
        {
            var subquery = new SelectExpression(_querySqlGeneratorFactory, _queryCompilationContext) { Alias = joinExpression.Alias };
            // Don't create new alias when adding tables to subquery
            subquery.AddTable(joinExpression.TableExpression, createUniqueAlias: false);
            subquery.IsProjectStar = true;
            subquery.Predicate = selectExpression.Predicate;

            var newJoinExpression
                = joinExpression is LeftOuterJoinExpression
                    ? (JoinExpressionBase)new LeftOuterJoinExpression(subquery)
                    : new InnerJoinExpression(subquery);

            newJoinExpression.QuerySource = joinExpression.QuerySource;
            newJoinExpression.Alias = joinExpression.Alias;

            return newJoinExpression;
        }

        private static void LiftOrderBy(
            SelectExpression innerJoinSelectExpression,
            SelectExpression targetSelectExpression,
            TableExpressionBase innerJoinExpression)
        {
            var needOrderingChanges
                = innerJoinSelectExpression.OrderBy
                    .Any(x => x.Expression is SelectExpression
                              || x.Expression.IsAliasWithColumnExpression()
                              || x.Expression.IsAliasWithSelectExpression());

            var orderings = innerJoinSelectExpression.OrderBy.ToList();
            if (needOrderingChanges)
            {
                innerJoinSelectExpression.ClearOrderBy();
            }

            foreach (var ordering in orderings)
            {
                var orderingExpression = ordering.Expression;

                var aliasExpression = ordering.Expression as AliasExpression;

                if (aliasExpression?.Alias != null)
                {
                    var columnExpression = aliasExpression.TryGetColumnExpression();
                    if (columnExpression != null)
                    {
                        orderingExpression
                            = new ColumnExpression(
                                aliasExpression.Alias,
                                columnExpression.Property,
                                columnExpression.Table);
                    }
                }

                var index = orderingExpression is SelectExpression
                    ? innerJoinSelectExpression.AddAliasToProjection(innerJoinSelectExpression.Alias + "_" + innerJoinSelectExpression.Projection.Count, orderingExpression)
                    : innerJoinSelectExpression.AddToProjection(orderingExpression);

                var expression = innerJoinSelectExpression.Projection[index];

                if (needOrderingChanges)
                {
                    innerJoinSelectExpression.AddToOrderBy(new Ordering(expression.TryGetColumnExpression() ?? expression, ordering.OrderingDirection));
                }

                var newExpression
                    = targetSelectExpression.UpdateColumnExpression(expression, innerJoinExpression);

                targetSelectExpression.AddToOrderBy(new Ordering(newExpression, ordering.OrderingDirection));
            }

            if ((innerJoinSelectExpression.Limit == null)
                && (innerJoinSelectExpression.Offset == null))
            {
                innerJoinSelectExpression.ClearOrderBy();
            }
        }

        private Expression BuildJoinEqualityExpression(
            INavigation navigation,
            TableExpressionBase targetTableExpression,
            TableExpressionBase joinExpression,
            IQuerySource querySource)
        {
            Expression joinPredicateExpression = null;

            var targetTableProjections = ExtractProjections(targetTableExpression).ToList();
            var joinTableProjections = ExtractProjections(joinExpression).ToList();

            for (var i = 0; i < navigation.ForeignKey.Properties.Count; i++)
            {
                var principalKeyProperty = navigation.ForeignKey.PrincipalKey.Properties[i];
                var foreignKeyProperty = navigation.ForeignKey.Properties[i];

                var foreignKeyColumnExpression
                    = BuildColumnExpression(
                        targetTableProjections, targetTableExpression, foreignKeyProperty, querySource);

                var primaryKeyColumnExpression
                    = BuildColumnExpression(
                        joinTableProjections, joinExpression, principalKeyProperty, querySource);

                var primaryKeyExpression = primaryKeyColumnExpression;

                if (foreignKeyColumnExpression.Type != primaryKeyExpression.Type)
                {
                    if (foreignKeyColumnExpression.Type.IsNullableType2()
                        && !primaryKeyExpression.Type.IsNullableType2())
                    {
                        primaryKeyExpression
                            = Expression.Convert(primaryKeyExpression, foreignKeyColumnExpression.Type);
                    }
                    else if (primaryKeyExpression.Type.IsNullableType2()
                             && !foreignKeyColumnExpression.Type.IsNullableType2())
                    {
                        foreignKeyColumnExpression
                            = Expression.Convert(foreignKeyColumnExpression, primaryKeyColumnExpression.Type);
                    }
                }

                var equalExpression
                    = Expression.Equal(foreignKeyColumnExpression, primaryKeyExpression);

                joinPredicateExpression
                    = joinPredicateExpression == null
                        ? equalExpression
                        : Expression.AndAlso(joinPredicateExpression, equalExpression);
            }

            return joinPredicateExpression;
        }

        private Expression BuildColumnExpression(
            IReadOnlyCollection<Expression> projections,
            TableExpressionBase tableExpression,
            IProperty property,
            IQuerySource querySource)
        {
            if (projections.Count == 0)
            {
                return new ColumnExpression(
                    _relationalAnnotationProvider.For(property).ColumnName,
                    property,
                    tableExpression);
            }

            var aliasExpressions
                = projections
                    .OfType<AliasExpression>()
                    .Where(p => p.TryGetColumnExpression()?.Property == property)
                    .ToList();

            var aliasExpression
                = aliasExpressions.Count == 1
                    ? aliasExpressions[0]
                    : aliasExpressions.Last(ae => ae.TryGetColumnExpression().Table.QuerySource == querySource);

            return new ColumnExpression(
                aliasExpression.Alias ?? aliasExpression.TryGetColumnExpression().Name,
                property,
                tableExpression);
        }

        private static IEnumerable<Expression> ExtractProjections(TableExpressionBase tableExpression)
        {
            var selectExpression = tableExpression as SelectExpression;

            if (selectExpression != null)
            {
                if (selectExpression.IsProjectStar)
                {
                    return selectExpression.Tables.SelectMany(ExtractProjections);
                }
                else
                {
                    return selectExpression.Projection.ToList();
                }
            }

            var joinExpression = tableExpression as JoinExpressionBase;

            return joinExpression != null
                ? ExtractProjections(joinExpression.TableExpression)
                : Enumerable.Empty<Expression>();
        }

        private static void AddToPredicate(SelectExpression selectExpression, Expression predicateToAdd)
            => selectExpression.Predicate
                = selectExpression.Predicate == null
                    ? predicateToAdd
                    : Expression.AndAlso(selectExpression.Predicate, predicateToAdd);

        private static bool IsOrderingOnNonPrincipalKeyProperties(
            IEnumerable<Ordering> orderings, IReadOnlyList<IProperty> properties)
        {
            return
                orderings
                    .Select(ordering => ((ordering.Expression as AliasExpression)?.Expression as ColumnExpression)?.Property)
                    .Any(property => !properties.Contains(property));
        }
    }

    public static class TypeHelper
    {
        //internal static bool IsEnumerableType(Type enumerableType)
        //{
        //    return FindGenericType(typeof(IEnumerable<>), enumerableType) != null;
        //}
        //internal static bool IsKindOfGeneric(Type type, Type definition)
        //{
        //    return FindGenericType(definition, type) != null;
        //}
        //internal static Type GetElementType(Type enumerableType)
        //{
        //    Type ienumType = FindGenericType(typeof(IEnumerable<>), enumerableType);
        //    if (ienumType != null)
        //        return ienumType.GetGenericArguments()[0];
        //    return enumerableType;
        //}
        //internal static Type FindGenericType(Type definition, Type type)
        //{
        //    while (type != null && type != typeof(object))
        //    {
        //        if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
        //            return type;
        //        if (definition.IsInterface)
        //        {
        //            foreach (Type itype in type.GetInterfaces())
        //            {
        //                Type found = FindGenericType(definition, itype);
        //                if (found != null)
        //                    return found;
        //            }
        //        }
        //        type = type.BaseType;
        //    }
        //    return null;
        //}
        internal static bool IsNullableType22(this Type type)
        {
            return type != null && type.GenericTypeArguments.Length > 0 && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        public static Type GetNonNullableType(this Type type)
        {
            if (IsNullableType22(type))
            {
                return type.GetGenericArguments()[0];
            }
            return type;
        }
    }
}
