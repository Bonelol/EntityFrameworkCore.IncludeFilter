using EntityFrameworkCore.IncludeFilter;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceQueryContextFactory : RelationalQueryContextFactory
    {
        INavigationExpressionCollection ExpressionCollection { get; set; }

        public ReplaceQueryContextFactory(IStateManager stateManager
            , IConcurrencyDetector concurrencyDetector
            , IRelationalConnection connection
            , IChangeDetector changeDetector
            , INavigationExpressionCollection collection) : base(stateManager, concurrencyDetector, connection, changeDetector)
        {
            ExpressionCollection = collection;
        }

        protected override IQueryBuffer CreateQueryBuffer()
        {
            return new ReplaceQueryBuffer(StateManager, ChangeDetector, ExpressionCollection);
        }
    }
}
