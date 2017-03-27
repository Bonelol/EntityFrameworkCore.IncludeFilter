using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.IncludeFilter
{
    public class NavigationExpressionCollection : Dictionary<INavigation, ICollection<Expression>>, INavigationExpressionCollection
    {
        public ICollection<Expression> TryGet(INavigation navigation)
        {
            ICollection<Expression> expressions;

            if (this.TryGetValue(navigation, out expressions))
            {

            }

            return expressions;
        }

        public bool HasKey(INavigation navigation)
        {
            return this.ContainsKey(navigation);
        }

        public void AddOrUpdate(INavigation navigation, ICollection<Expression> expressions)
        {
            if (this.HasKey(navigation))
                this[navigation] = expressions;
            else
                this.Add(navigation, expressions);
        }

        void INavigationExpressionCollection.Remove(INavigation navigation)
        {
            this.Remove(navigation);
        }
    }

    public interface INavigationExpressionCollection
    {
        void Add(INavigation navigation, ICollection<Expression> expressions);

        ICollection<Expression> TryGet(INavigation navigation);

        bool HasKey(INavigation navigation);

        ICollection<Expression> this[INavigation key] { get; set; }

        void AddOrUpdate(INavigation navigation, ICollection<Expression> expressions);

        void Remove(INavigation navigation);
    }
}
