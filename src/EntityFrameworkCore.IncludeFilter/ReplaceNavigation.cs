using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace EntityFrameworkCore.IncludeFilter
{
    internal class ReplaceNavigation : Navigation
    {
        internal readonly Navigation OriginalNavigation;

        public ReplaceNavigation(Navigation navigation) : base(navigation.GetPropertyInfo(), navigation.ForeignKey)
        {
            OriginalNavigation = navigation;
            Expressions = new List<Expression>();
        }

        public ReplaceNavigation(Navigation navigation, ForeignKey foreignKey) : base(navigation.GetPropertyInfo(), foreignKey)
        {
            OriginalNavigation = navigation;
            Expressions = new List<Expression>();
        }

        private ReplaceNavigation(string navigationName, ForeignKey foreignKey) : base(navigationName, foreignKey)
        {
            Expressions = new List<Expression>();
        }

        public ICollection<Expression> Expressions { get; set; }

        /// <summary>
        /// Use original one to compare
        /// </summary>
        public override EntityType DeclaringEntityType => OriginalNavigation.IsDependentToPrincipal()
            ? ForeignKey.DeclaringEntityType
            : ForeignKey.PrincipalEntityType;

        public override IClrCollectionAccessor CollectionAccessor => OriginalNavigation.GetCollectionAccessor();

        public override ForeignKey ForeignKey => OriginalNavigation.ForeignKey;
    }
}
