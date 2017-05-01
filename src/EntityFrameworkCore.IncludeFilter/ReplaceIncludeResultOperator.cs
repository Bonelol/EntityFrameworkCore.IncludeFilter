using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.ResultOperators.Internal;

namespace EntityFrameworkCore.IncludeFilter
{
    class ReplaceIncludeResultOperator : IncludeResultOperator
    {
        public Dictionary<string, HashSet<Expression>> Expressions { get; }

        public ReplaceIncludeResultOperator(MemberExpression navigationPropertyPath, Dictionary<string, HashSet<Expression>> expressions) : base(navigationPropertyPath)
        {
            Expressions = expressions;
        }
    }
}
