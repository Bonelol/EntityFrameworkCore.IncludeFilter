using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.ResultOperators.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Clauses.StreamedData;
using Remotion.Linq.Parsing.ExpressionVisitors.Transformation;
using Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation;
using Remotion.Linq.Parsing.Structure;
using Remotion.Linq.Parsing.Structure.ExpressionTreeProcessors;
using Remotion.Linq.Parsing.Structure.NodeTypeProviders;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceQueryCompiler : IQueryCompiler
    {
        private static MethodInfo CompileQueryMethod { get; }
            = typeof(IDatabase).GetTypeInfo()
                .GetDeclaredMethod(nameof(IDatabase.CompileQuery));

        private static readonly IEvaluatableExpressionFilter _evaluatableExpressionFilter
            = new EvaluatableExpressionFilter();

        private readonly IQueryContextFactory _queryContextFactory;
        private readonly ICompiledQueryCache _compiledQueryCache;
        private readonly ICompiledQueryCacheKeyGenerator _compiledQueryCacheKeyGenerator;
        private readonly IDatabase _database;
        private readonly ISensitiveDataLogger _logger;
        private readonly MethodInfoBasedNodeTypeRegistry _methodInfoBasedNodeTypeRegistry;
        private readonly Type _contextType;

        private INodeTypeProvider _nodeTypeProvider;

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        public ReplaceQueryCompiler(
            IQueryContextFactory queryContextFactory,
            ICompiledQueryCache compiledQueryCache,
            ICompiledQueryCacheKeyGenerator compiledQueryCacheKeyGenerator,
            IDatabase database,
            ISensitiveDataLogger<QueryCompiler> logger,
            MethodInfoBasedNodeTypeRegistry methodInfoBasedNodeTypeRegistry,
            ICurrentDbContext currentContext)
        {
            _queryContextFactory = queryContextFactory;
            _compiledQueryCache = compiledQueryCache;
            _compiledQueryCacheKeyGenerator = compiledQueryCacheKeyGenerator;
            _database = database;
            _logger = logger;
            _methodInfoBasedNodeTypeRegistry = methodInfoBasedNodeTypeRegistry;
            _contextType = currentContext.Context.GetType();
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        protected virtual IDatabase Database => _database;

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        public virtual TResult Execute<TResult>(Expression query)
        {
            var queryContext = _queryContextFactory.Create();

            query = ExtractParameters(query, queryContext);

            return CompileQuery<TResult>(query)(queryContext);
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        public virtual IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression query)
        {
            var queryContext = _queryContextFactory.Create();

            query = ExtractParameters(query, queryContext);

            return CompileAsyncQuery<TResult>(query)(queryContext);
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        public virtual Task<TResult> ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken)
        {
            var queryContext = _queryContextFactory.Create();

            queryContext.CancellationToken = cancellationToken;

            query = ExtractParameters(query, queryContext);

            try
            {
                return CompileAsyncQuery<TResult>(query)(queryContext).First(cancellationToken);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        protected virtual Expression ExtractParameters(Expression query, QueryContext queryContext)
        {
            return ParameterExtractingExpressionVisitor
                .ExtractParameters(query, queryContext, _evaluatableExpressionFilter, _logger);
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        protected virtual Func<QueryContext, TResult> CompileQuery<TResult>(Expression query)
        {
            return _compiledQueryCache
                .GetOrAddQuery(_compiledQueryCacheKeyGenerator.GenerateCacheKey(query, async: false),
                    () =>
                    {
                        var queryModel = CreateQueryParser(NodeTypeProvider).GetParsedQuery(query);
                        var resultItemType = (queryModel.GetOutputDataInfo() as StreamedSequenceInfo)?.ResultItemType ?? typeof(TResult);

                        if (resultItemType == typeof(TResult))
                        {
                            var compiledQuery = _database.CompileQuery<TResult>(queryModel);

                            return qc =>
                            {
                                try
                                {
                                    return compiledQuery(qc).First();
                                }
                                catch
                                {
                                    throw;
                                }
                            };
                        }

                        try
                        {
                            return (Func<QueryContext, TResult>)CompileQueryMethod
                                .MakeGenericMethod(resultItemType)
                                .Invoke(_database, new object[] { queryModel });
                        }
                        catch (TargetInvocationException e)
                        {
                            ExceptionDispatchInfo.Capture(e.InnerException).Throw();

                            throw;
                        }
                    });
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        protected virtual Func<QueryContext, IAsyncEnumerable<TResult>> CompileAsyncQuery<TResult>(Expression query)
        {
            return _compiledQueryCache
                .GetOrAddAsyncQuery(_compiledQueryCacheKeyGenerator.GenerateCacheKey(query, async: true),
                    () =>
                    {
                        var queryModel
                            = CreateQueryParser(NodeTypeProvider)
                                .GetParsedQuery(query);

                        return _database.CompileAsyncQuery<TResult>(queryModel);
                    });
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private static QueryParser CreateQueryParser(INodeTypeProvider nodeTypeProvider)
            => new QueryParser(
                new ExpressionTreeParser(
                    nodeTypeProvider,
                    new CompoundExpressionTreeProcessor(new IExpressionTreeProcessor[]
                    {
                        new PartialEvaluatingExpressionTreeProcessor(_evaluatableExpressionFilter),
                        new TransformingExpressionTreeProcessor(ExpressionTransformerRegistry.CreateDefault())
                    })));

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private class EvaluatableExpressionFilter : EvaluatableExpressionFilterBase
        {
            private static readonly PropertyInfo _dateTimeNow
                = typeof(DateTime).GetTypeInfo().GetDeclaredProperty(nameof(DateTime.Now));

            private static readonly PropertyInfo _dateTimeUtcNow
                = typeof(DateTime).GetTypeInfo().GetDeclaredProperty(nameof(DateTime.UtcNow));

            private static readonly MethodInfo _guidNewGuid
                = typeof(Guid).GetTypeInfo().GetDeclaredMethod(nameof(Guid.NewGuid));

            private static readonly List<MethodInfo> _randomNext
                = typeof(Random).GetTypeInfo().GetDeclaredMethods(nameof(Random.Next)).ToList();

            public override bool IsEvaluatableMethodCall(MethodCallExpression methodCallExpression)
            {
                if ((methodCallExpression.Method == _guidNewGuid)
                    || _randomNext.Contains(methodCallExpression.Method))
                {
                    return false;
                }

                return base.IsEvaluatableMethodCall(methodCallExpression);
            }

            public override bool IsEvaluatableMember(MemberExpression memberExpression)
                => memberExpression.Member != _dateTimeNow && memberExpression.Member != _dateTimeUtcNow;
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private INodeTypeProvider NodeTypeProvider
            => _nodeTypeProvider
               ?? (_nodeTypeProvider
                   = CreateNodeTypeProvider(_methodInfoBasedNodeTypeRegistry));

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private static INodeTypeProvider CreateNodeTypeProvider(
            MethodInfoBasedNodeTypeRegistry methodInfoBasedNodeTypeRegistry)
        {
            methodInfoBasedNodeTypeRegistry
                .Register(TrackingExpressionNode.SupportedMethods, typeof(TrackingExpressionNode));

            //TODO move IncludeExpressionNode back
            methodInfoBasedNodeTypeRegistry
                .Register(IncludeExpressionNode.SupportedMethods, typeof(ReplaceIncludeExpressionNode));

            methodInfoBasedNodeTypeRegistry
                .Register(ThenIncludeExpressionNode.SupportedMethods, typeof(ThenIncludeExpressionNode));

            // register our IncludeWithFilter method.
            methodInfoBasedNodeTypeRegistry
                .Register(ReplaceIncludeExpressionNode.SupportedMethods, typeof(ReplaceIncludeExpressionNode));


            var innerProviders
                = new INodeTypeProvider[]
                {
                    methodInfoBasedNodeTypeRegistry,
                    MethodNameBasedNodeTypeRegistry.CreateFromRelinqAssembly()
                };

            return new CompoundNodeTypeProvider(innerProviders);
        }
    }
}
