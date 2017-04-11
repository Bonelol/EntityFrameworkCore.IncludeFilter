using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.ResultOperators.Internal;
using Remotion.Linq.Clauses.Expressions;

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
