using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.ResultOperators.Internal;

namespace EntityFrameworkCore.IncludeFilter
{
    class ReplaceIncludeResultOperator : IncludeResultOperator
    {
        public ICollection<Expression> Expressions { get; }

        public ReplaceIncludeResultOperator(MemberExpression navigationPropertyPath, ICollection<Expression> expressions) : base(navigationPropertyPath)
        {
            Expressions = expressions;
        }
    }
}
