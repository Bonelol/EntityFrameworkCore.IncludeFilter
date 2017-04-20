using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Internal;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing.Structure.IntermediateModel;

namespace EntityFrameworkCore.IncludeFilter
{
    /// <summary>
    /// Copyright (c) .NET Foundation. All rights reserved.
    ///
    /// Modified by Qinglin (Max) Meng
    ///  
    /// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
    /// these files except in compliance with the License. You may obtain a copy of the
    /// License at
    ///
    /// http://www.apache.org/licenses/LICENSE-2.0
    ///
    /// Unless required by applicable law or agreed to in writing, software distributed
    /// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
    /// CONDITIONS OF ANY KIND, either express or implied. See the License for the
    /// specific language governing permissions and limitations under the License.
    /// </summary>
    public class ReplaceThenIncludeExpressionNode : ResultOperatorExpressionNodeBase
    {
        private readonly Dictionary<string, HashSet<Expression>> _expressions;

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        public static readonly IReadOnlyCollection<MethodInfo> SupportedMethods = new[]
        {
            QueryableExtensions.ThenIncludeAfterEnumerableMethodInfo
        };

        private readonly LambdaExpression _navigationPropertyPathLambda;

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        public ReplaceThenIncludeExpressionNode(MethodCallExpressionParseInfo parseInfo, LambdaExpression navigationPropertyPathLambda)
            : base(parseInfo, null, null)
        {
            _navigationPropertyPathLambda = navigationPropertyPathLambda;
            _expressions = new Dictionary<string, HashSet<Expression>>();
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code. Modified to store predict expressions.
        /// </summary>
        protected override void ApplyNodeSpecificSemantics(QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
        {
            var includeResultOperator = (ReplaceIncludeResultOperator)clauseGenerationContext.GetContextInfo(Source);
            includeResultOperator.AppendToNavigationPath(GetComplexPropertyAccess(_navigationPropertyPathLambda));

            foreach (var expression in _expressions)
            {
                includeResultOperator.Expressions.Add(expression.Key, expression.Value);
            }
            
            clauseGenerationContext.AddContextInfo(this, includeResultOperator);
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code. Modified to store predict expressions.
        /// </summary>
        private PropertyInfo[] GetComplexPropertyAccess(LambdaExpression propertyAccessExpression)
        {
            Expression body = propertyAccessExpression.Body;

            if (propertyAccessExpression.Body.NodeType == ExpressionType.Extension)
            {
                var sub = (SubQueryExpression)propertyAccessExpression.Body;
                var whereClause = (WhereClause)sub.QueryModel.BodyClauses.First();

                body = sub.QueryModel.MainFromClause.FromExpression;

                var member = body as MemberExpression;
                if (member != null)
                {
                    var name = $"{member.Member.DeclaringType.FullName}-{member.Member.Name}";
                    _expressions.Add(name, new HashSet<Expression>(new[] { whereClause.Predicate }));
                }
            }

            var propertyPath = MatchPropertyAccess(propertyAccessExpression.Parameters.Single(), body);

            if (propertyPath == null)
            {
                throw new ArgumentException(
                    CoreStrings.InvalidComplexPropertyExpression(propertyAccessExpression));
            }

            return propertyPath;
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private static PropertyInfo[] MatchPropertyAccess(Expression parameterExpression, Expression propertyAccessExpression)
        {
            var propertyInfos = new List<PropertyInfo>();

            MemberExpression memberExpression;

            do
            {
                memberExpression = RemoveConvert(propertyAccessExpression) as MemberExpression;

                var propertyInfo = memberExpression?.Member as PropertyInfo;

                if (propertyInfo == null)
                {
                    return null;
                }

                propertyInfos.Insert(0, propertyInfo);

                propertyAccessExpression = memberExpression.Expression;
            }
            while (memberExpression.Expression.RemoveConvert() != parameterExpression);

            return propertyInfos.ToArray();
        }

        /// <summary>
        ///    Copy from EntityFramework Core source code
        /// </summary>
        private static Expression RemoveConvert(Expression expression)
        {
            while ((expression != null)
                   && ((expression.NodeType == ExpressionType.Convert)
                       || (expression.NodeType == ExpressionType.ConvertChecked)))
            {
                expression = RemoveConvert(((UnaryExpression)expression).Operand);
            }

            return expression;
        }

        protected override ResultOperatorBase CreateResultOperator(ClauseGenerationContext clauseGenerationContext)
            => null;

        public override Expression Resolve(ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
        {
            return Source.Resolve(inputParameter, expressionToBeResolved, clauseGenerationContext);
        }
    }
}
