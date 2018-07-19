using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceRelationalQueryModelVisitor : RelationalQueryModelVisitor
    {
        public ReplaceRelationalQueryModelVisitor(EntityQueryModelVisitorDependencies dependencies, RelationalQueryModelVisitorDependencies relationalDependencies, RelationalQueryCompilationContext queryCompilationContext, RelationalQueryModelVisitor parentQueryModelVisitor) : base(dependencies, relationalDependencies, queryCompilationContext, parentQueryModelVisitor)
        {
            var modelExpressionApplyingExpressionVisitor
                = new ReplaceModelExpressionApplyingExpressionVisitor(
                    queryCompilationContext,
                    dependencies.QueryModelGenerator,
                    this);

            var field = typeof(EntityQueryModelVisitor).GetField("_modelExpressionApplyingExpressionVisitor",
                BindingFlags.NonPublic | BindingFlags.Instance);

            field.SetValue(this, modelExpressionApplyingExpressionVisitor);
        }
    }
}
