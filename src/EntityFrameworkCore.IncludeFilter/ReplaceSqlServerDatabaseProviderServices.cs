using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
