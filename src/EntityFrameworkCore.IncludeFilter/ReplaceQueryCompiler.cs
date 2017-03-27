using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Parsing.Structure.NodeTypeProviders;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceQueryCompiler : QueryCompiler
    {
        public ReplaceQueryCompiler(
               IQueryContextFactory queryContextFactory,
               ICompiledQueryCache compiledQueryCache,
               ICompiledQueryCacheKeyGenerator compiledQueryCacheKeyGenerator,
               IDatabase database,
               ISensitiveDataLogger<QueryCompiler> logger,
               MethodInfoBasedNodeTypeRegistry methodInfoBasedNodeTypeRegistry,
               ICurrentDbContext currentContext) : base(queryContextFactory, compiledQueryCache, compiledQueryCacheKeyGenerator, database, logger, methodInfoBasedNodeTypeRegistry, currentContext)
        {
            methodInfoBasedNodeTypeRegistry.Register(ReplaceIncludeExpressionNode.SupportedMethods, typeof(ReplaceIncludeExpressionNode));
            methodInfoBasedNodeTypeRegistry.Register(ReplaceThenIncludeExpressionNode.SupportedMethods, typeof(ReplaceThenIncludeExpressionNode));
        }
    }
}
