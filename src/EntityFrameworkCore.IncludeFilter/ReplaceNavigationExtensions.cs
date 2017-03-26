using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.IncludeFilter
{
    internal static class ReplaceNavigationExtensions
    {
        /// <summary>
        ///     Gets a value indicating whether the given navigation property is the navigation property on the dependent entity
        ///     type that points to the principal entity.
        /// </summary>
        /// <param name="navigation"> The navigation property to check. </param>
        /// <returns>
        ///     True if the given navigation property is the navigation property on the dependent entity
        ///     type that points to the principal entity, otherwise false.
        /// </returns>
        public static bool IsDependentToPrincipal(this ReplaceNavigation navigation)
            => navigation.ForeignKey.DependentToPrincipal == navigation.OriginalNavigation;

        /// <summary>
        ///     Gets a value indicating whether the given navigation property is a collection property.
        /// </summary>
        /// <param name="navigation"> The navigation property to check. </param>
        /// <returns>
        ///     True if this is a collection property, false if it is a reference property.
        /// </returns>
        public static bool IsCollection(this ReplaceNavigation navigation)
        {
            return !navigation.IsDependentToPrincipal() && !navigation.OriginalNavigation.ForeignKey.IsUnique;
        }

        /// <summary>
        ///     Gets the navigation property on the other end of the relationship. Returns null if
        ///     there is no navigation property defined on the other end of the relationship.
        /// </summary>
        /// <param name="navigation"> The navigation property to find the inverse of. </param>
        /// <returns>
        ///     The inverse navigation, or null if none is defined.
        /// </returns>
        public static INavigation FindInverse(this ReplaceNavigation navigation)
        {
            return navigation.IsDependentToPrincipal()
                ? navigation.OriginalNavigation.ForeignKey.PrincipalToDependent
                : navigation.OriginalNavigation.ForeignKey.DependentToPrincipal;
        }

        /// <summary>
        ///     Gets the entity type that a given navigation property will hold an instance of
        ///     (or hold instances of if it is a collection navigation).
        /// </summary>
        /// <param name="navigation"> The navigation property to find the target entity type of. </param>
        /// <returns> The target entity type. </returns>
        public static IEntityType GetTargetType(this ReplaceNavigation navigation)
        {
            return navigation.IsDependentToPrincipal()
                ? navigation.OriginalNavigation.ForeignKey.PrincipalEntityType
                : navigation.OriginalNavigation.ForeignKey.DeclaringEntityType;
        }
    }
}
