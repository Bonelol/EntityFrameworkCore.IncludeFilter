using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.IncludeFilter
{
    public static class QueryableExtensions
    {
        internal static readonly MethodInfo IncludeMethodInfo =
            typeof(QueryableExtensions).GetTypeInfo().GetDeclaredMethods(nameof(IncludeWithFilter)).Single(mi => mi.GetParameters().Any(pi => pi.Name == "navigationPropertyPath"));

        public static IIncludableQueryable<TEntity, ICollection<TProperty>> IncludeWithFilter<TEntity, TProperty>(this IQueryable<TEntity> source
            , Expression<Func<TEntity, ICollection<TProperty>>> navigationPropertyPath
            , Expression<Func<TProperty, bool>> filter)
            where TEntity : class
        {
            return new IncludableQueryable<TEntity, ICollection<TProperty>>(
                source.Provider.CreateQuery<TEntity>(
                    Expression.Call(
                        null,
                        IncludeMethodInfo.MakeGenericMethod(typeof(TEntity), typeof(TProperty)),
                        new[] { source.Expression, Expression.Quote(navigationPropertyPath), filter })));
        }

        public static IIncludableQueryable<TEntity, ICollection<TProperty>> ThenIncludeWithFilter<TEntity, TPreviousProperty, TProperty>(this IIncludableQueryable<TEntity
            , IEnumerable<TPreviousProperty>> source
            , Expression<Func<TPreviousProperty, ICollection<TProperty>>> navigationPropertyPath
            , Expression<Func<TProperty, bool>> filter)
            where TEntity : class
        {
            return new IncludableQueryable<TEntity, ICollection<TProperty>>(
                source.Provider.CreateQuery<TEntity>(
                    Expression.Call(
                        null,
                        ThenIncludeAfterEnumerableMethodInfo.MakeGenericMethod(typeof(TEntity), typeof(TPreviousProperty), typeof(TProperty)),
                        new[] { source.Expression, Expression.Quote(navigationPropertyPath), filter })));
        }

        public static IIncludableQueryable<TEntity, ICollection<TProperty>> ThenIncludeWithFilter<TEntity, TPreviousProperty, TProperty>(this IIncludableQueryable<TEntity
                , TPreviousProperty> source
            , Expression<Func<TPreviousProperty, ICollection<TProperty>>> navigationPropertyPath
            , Expression<Func<TProperty, bool>> filter)
            where TEntity : class
        {
            return new IncludableQueryable<TEntity, ICollection<TProperty>>(
                source.Provider.CreateQuery<TEntity>(
                    Expression.Call(
                        null,
                        ThenIncludeAfterReferenceMethodInfo.MakeGenericMethod(typeof(TEntity), typeof(TPreviousProperty), typeof(TProperty)),
                        new[] { source.Expression, Expression.Quote(navigationPropertyPath), filter })));
        }

        internal static readonly MethodInfo ThenIncludeAfterEnumerableMethodInfo
            = typeof(QueryableExtensions)
                .GetTypeInfo().GetDeclaredMethods(nameof(ThenIncludeWithFilter))
                .Single(mi => !mi.GetParameters()[0].ParameterType.GenericTypeArguments[1].IsGenericParameter);

        internal static readonly MethodInfo ThenIncludeAfterReferenceMethodInfo
            = typeof(QueryableExtensions)
                .GetTypeInfo().GetDeclaredMethods(nameof(ThenIncludeWithFilter))
                .Single(mi => mi.GetParameters()[0].ParameterType.GenericTypeArguments[1].IsGenericParameter);


        private class IncludableQueryable<TEntity, TProperty> : IIncludableQueryable<TEntity, TProperty>, IAsyncEnumerable<TEntity>
        {
            private readonly IQueryable<TEntity> _queryable;

            public IncludableQueryable(IQueryable<TEntity> queryable)
            {
                _queryable = queryable;
            }

            public Expression Expression => _queryable.Expression;
            public Type ElementType => _queryable.ElementType;
            public IQueryProvider Provider => _queryable.Provider;

            public IEnumerator<TEntity> GetEnumerator() => _queryable.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            IAsyncEnumerator<TEntity> IAsyncEnumerable<TEntity>.GetEnumerator()
                => ((IAsyncEnumerable<TEntity>)_queryable).GetEnumerator();
        }
    }
}
