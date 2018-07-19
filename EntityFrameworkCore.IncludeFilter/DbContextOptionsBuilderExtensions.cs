using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EntityFrameworkCore.IncludeFilter
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder AddIncludeWithFilterMethods(this DbContextOptionsBuilder builder)
        {
            return builder.ReplaceService<IEntityQueryModelVisitorFactory, ReplaceRelationalQueryModelVisitorFactory>()
                .ReplaceService<INodeTypeProviderFactory, ReplaceMethodInfoBasedNodeTypeRegistryFactory>();
        }
    }
}
