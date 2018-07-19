using Microsoft.EntityFrameworkCore.Query.Internal;
using Remotion.Linq.Parsing.Structure;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceMethodInfoBasedNodeTypeRegistryFactory : DefaultMethodInfoBasedNodeTypeRegistryFactory
    {
        public override INodeTypeProvider Create()
        {
            // Summary:
            //     Registers the specific methods with the given nodeType. The given methods must
            //     either be non-generic or open generic method definitions. If a method has already
            //     been registered before, the later registration overwrites the earlier one.
            var provider = base.Create();
            RegisterMethods(ReplaceIncludeExpressionNode.SupportedMethods, typeof(ReplaceIncludeExpressionNode));
            RegisterMethods(ReplaceThenIncludeExpressionNode.SupportedMethods, typeof(ReplaceThenIncludeExpressionNode));
            return provider;
        }
    }
}
