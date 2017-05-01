using System;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage.Internal;

namespace EntityFrameworkCore.IncludeFilter
{
    class ReplaceSqlServerDatabaseProviderServices : SqlServerDatabaseProviderServices
    {
        public ReplaceSqlServerDatabaseProviderServices(IServiceProvider services) : base(services)
        {
        }

        public override IEntityQueryModelVisitorFactory EntityQueryModelVisitorFactory => GetService<ReplaceSqlServerQueryModelVisitorFactory>();
    }
}
