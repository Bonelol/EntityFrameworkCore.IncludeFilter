using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
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
                .AddScoped<IQueryCompiler, ReplaceQueryCompiler>()
                .AddScoped<IIncludeExpressionVisitorFactory, ReplaceIncludeExpressionVisitorFactory>()
                .AddScoped<INavigationExpressionCollection, NavigationExpressionCollection>();

            return services;
        }

        private static IServiceCollection AddQuery(this IServiceCollection serviceCollection)
            => serviceCollection
                .AddScoped<SqlServerQueryCompilationContextFactory>()
                .AddScoped<SqlServerCompositeMemberTranslator>()
                .AddScoped<SqlServerCompositeMethodCallTranslator>()
                .AddScoped<SqlServerQuerySqlGeneratorFactory>();
    }
}
