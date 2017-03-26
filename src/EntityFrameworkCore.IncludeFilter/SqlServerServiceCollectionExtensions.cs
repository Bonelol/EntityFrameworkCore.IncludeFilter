using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EntityFrameworkCore.IncludeFilter
{
    public static class SqlServerServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkSqlServerWithIncludeFilter(this IServiceCollection services)
        {
            services.AddRelational();

            services.TryAddEnumerable(ServiceDescriptor
                .Singleton<IDatabaseProvider, DatabaseProvider<ReplaceSqlServerDatabaseProviderServices, SqlServerOptionsExtension>>());

            services.TryAdd(new ServiceCollection()
                .AddSingleton<ISqlServerValueGeneratorCache, SqlServerValueGeneratorCache>()
                .AddSingleton<SqlServerTypeMapper>()
                .AddSingleton<SqlServerSqlGenerationHelper>()
                .AddSingleton<SqlServerModelSource>()
                .AddSingleton<SqlServerAnnotationProvider>()
                .AddSingleton<SqlServerMigrationsAnnotationProvider>()
                .AddScoped<SqlServerConventionSetBuilder>()
                .AddScoped<ISqlServerUpdateSqlGenerator, SqlServerUpdateSqlGenerator>()
                .AddScoped<ISqlServerSequenceValueGeneratorFactory, SqlServerSequenceValueGeneratorFactory>()
                .AddScoped<SqlServerModificationCommandBatchFactory>()
                .AddScoped<SqlServerValueGeneratorSelector>()
                //.AddScoped<ReplaceSqlServerDatabaseProviderServices>()
                .AddScoped<ISqlServerConnection, SqlServerConnection>()
                .AddScoped<SqlServerMigrationsSqlGenerator>()
                .AddScoped<SqlServerDatabaseCreator>()
                .AddScoped<SqlServerHistoryRepository>()
                //.AddScoped<ReplaceSqlServerQueryModelVisitorFactory>()
                .AddScoped<SqlServerCompiledQueryCacheKeyGenerator>()

                .AddQuery());
            
            //newly added
            services.AddScoped<ReplaceSqlServerDatabaseProviderServices>()
                .AddScoped<ReplaceSqlServerQueryModelVisitorFactory>()
                .AddScoped<IQueryBuffer, ReplaceQueryBuffer>()
                .AddScoped<IQueryCompiler, ReplaceQueryCompiler>()
                .AddScoped<IQueryContextFactory, ReplaceQueryContextFactory>()
                .AddScoped<IIncludeExpressionVisitorFactory, ReplaceIncludeExpressionVisitorFactory>()
                .AddScoped<INavigationExpressionCollection, NavigationExpressionCollection>()
                .AddScoped<ISqlTranslatingExpressionVisitorFactory, ReplaceSqlTranslatingExpressionVisitorFactory>();

            return services;
        }

        private static IServiceCollection AddQuery(this IServiceCollection serviceCollection)
            => serviceCollection
                .AddScoped<SqlServerQueryCompilationContextFactory>()
                .AddScoped<SqlServerCompositeMemberTranslator>()
                .AddScoped<SqlServerCompositeMethodCallTranslator>()
                .AddScoped<SqlServerQuerySqlGeneratorFactory, ReplaceSqlServerQuerySqlGeneratorFactory>();
    }

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
    }

    public interface INavigationExpressionCollection
    {
        void Add(INavigation navigation, ICollection<Expression> expressions);

        ICollection<Expression> TryGet(INavigation navigation);

        bool HasKey(INavigation navigation);

        ICollection<Expression> this[INavigation key] { get; set; }
}
}
