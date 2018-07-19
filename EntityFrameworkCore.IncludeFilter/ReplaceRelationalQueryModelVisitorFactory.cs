using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceRelationalQueryModelVisitorFactory : RelationalQueryModelVisitorFactory
    {
        public ReplaceRelationalQueryModelVisitorFactory(EntityQueryModelVisitorDependencies dependencies, RelationalQueryModelVisitorDependencies relationalDependencies) : base(dependencies, relationalDependencies)
        {
        }

        public override EntityQueryModelVisitor Create(
            QueryCompilationContext queryCompilationContext,
            EntityQueryModelVisitor parentEntityQueryModelVisitor)
            => new ReplaceRelationalQueryModelVisitor(
                Dependencies,
                RelationalDependencies,
                (RelationalQueryCompilationContext)queryCompilationContext,
                (ReplaceRelationalQueryModelVisitor)parentEntityQueryModelVisitor);
    }
}
