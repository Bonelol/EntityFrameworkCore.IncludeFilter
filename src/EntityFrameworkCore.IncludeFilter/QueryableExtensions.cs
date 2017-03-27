using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation;

namespace EntityFrameworkCore.IncludeFilter
{
    public static class QueryableExtensions
    {
        #region Include

        internal static readonly MethodInfo IncludeMethodInfo =
            typeof(QueryableExtensions).GetTypeInfo().GetDeclaredMethods(nameof(IncludeWithFilter)).Single(mi => mi.GetParameters().Any(pi => pi.Name == "navigationPropertyPath"));

        /// <summary>
        ///     Specifies and filters related entities to include in the query results. The navigation property to be included is specified starting with the
        ///     type of entity being queried (<typeparamref name="TEntity" />).
        /// </summary>
        /// <example>
        ///     <para>
        ///         The following query shows including a single level of related entities.
        ///         <code>
        ///             context.Blogs.IncludeWithFilter(blog => blog.Posts.Where(post=>post.Active));
        ///         </code>
        ///     </para>
        ///     <para>
        ///         The following query shows including multiple branches of related data.
        ///         <code>
        ///             context.Blogs
        ///                 .IncludeWithFilter(blog => blog.Posts.Where(post=>post.Active))
        ///                 .IncludeWithFilter(blog => blog.Contributors.Where(contributor=>contributor.Active));
        ///         </code>
        ///     </para>
        /// </example>
        /// <typeparam name="TEntity"> The type of entity being queried. </typeparam>
        /// <typeparam name="TProperty"> The type of the related entity to be included. </typeparam>
        /// <param name="source"> The source query. </param>
        /// <param name="navigationPropertyPath">
        ///     A lambda expression representing the navigation property to be included (<c>t => t.Property1.Where(p=>p.Id == 2)</c>).
        /// </param>
        /// <returns>
        ///     A new query with the related data included.
        /// </returns>
        public static IIncludableQueryable<TEntity, TProperty> IncludeWithFilter<TEntity, TProperty>(this IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> navigationPropertyPath)
            where TEntity : class
        {
            return new IncludableQueryable<TEntity, TProperty>(
                source.Provider.CreateQuery<TEntity>(
                    Expression.Call(
                        null,
                        IncludeMethodInfo.MakeGenericMethod(typeof(TEntity), typeof(TProperty)),
                        new[] { source.Expression, Expression.Quote(navigationPropertyPath) })));
        }

        public static IIncludableQueryable<TEntity, TProperty> ThenIncludeWithFilter<TEntity, TPreviousProperty, TProperty>(this IIncludableQueryable<TEntity, IEnumerable<TPreviousProperty>> source, Expression<System.Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            where TEntity : class
        {
            return new IncludableQueryable< TEntity, TProperty > (
                 source.Provider.CreateQuery<TEntity>(
                     Expression.Call(
                         null,
                         ThenIncludeAfterEnumerableMethodInfo.MakeGenericMethod(typeof(TEntity), typeof(TPreviousProperty), typeof(TProperty)),
                         new[] { source.Expression, Expression.Quote(navigationPropertyPath) })));
        }

        public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(this IIncludableQueryable<TEntity, IEnumerable<TPreviousProperty>> source, Expression<System.Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            where TEntity : class
        {
            return new IncludableQueryable<TEntity, TProperty>(
                 source.Provider.CreateQuery<TEntity>(
                     Expression.Call(
                         null,
                         ThenIncludeAfterEnumerableMethodInfo.MakeGenericMethod(typeof(TEntity), typeof(TPreviousProperty), typeof(TProperty)),
                         new[] { source.Expression, Expression.Quote(navigationPropertyPath) })));
        }

        internal static readonly MethodInfo ThenIncludeAfterEnumerableMethodInfo
            = typeof(QueryableExtensions)
                .GetTypeInfo().GetDeclaredMethods(nameof(ThenIncludeWithFilter))
                .Single(mi => !mi.GetParameters()[0].ParameterType.GenericTypeArguments[1].IsGenericParameter);

        //internal static readonly MethodInfo ThenIncludeAfterReferenceMethodInfo
        //    = typeof(QueryableExtensions)
        //        .GetTypeInfo().GetDeclaredMethods(nameof(QueryableExtensions.ThenInclude))
        //        .Single(mi => mi.GetParameters()[0].ParameterType.GenericTypeArguments[1].IsGenericParameter);

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
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
        #endregion

        public class ApiCompilationFilter : EvaluatableExpressionFilterBase { }
    }
}
