using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceSqlServerQuerySqlGeneratorFactory : SqlServerQuerySqlGeneratorFactory
    {
        public ReplaceSqlServerQuerySqlGeneratorFactory(
               IRelationalCommandBuilderFactory commandBuilderFactory,
               ISqlGenerationHelper sqlGenerationHelper,
               IParameterNameGeneratorFactory parameterNameGeneratorFactory,
               IRelationalTypeMapper relationalTypeMapper)
            : base(
                commandBuilderFactory,
                sqlGenerationHelper,
                parameterNameGeneratorFactory,
                relationalTypeMapper)
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override IQuerySqlGenerator CreateDefault(SelectExpression selectExpression)
            => new ReplaceSqlServerQuerySqlGenerator(
                CommandBuilderFactory,
                SqlGenerationHelper,
                ParameterNameGeneratorFactory,
                RelationalTypeMapper,
                selectExpression);
    }
}
