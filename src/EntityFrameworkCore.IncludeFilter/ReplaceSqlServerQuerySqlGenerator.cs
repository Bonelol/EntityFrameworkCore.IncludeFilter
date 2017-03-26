using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.ExpressionVisitors.Internal;
using Microsoft.EntityFrameworkCore.Query.Sql.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Parsing;

namespace EntityFrameworkCore.IncludeFilter
{
    public class ReplaceSqlServerQuerySqlGenerator : SqlServerQuerySqlGenerator
    {
        public ReplaceSqlServerQuerySqlGenerator(IRelationalCommandBuilderFactory relationalCommandBuilderFactory
            , ISqlGenerationHelper sqlGenerationHelper
            , IParameterNameGeneratorFactory parameterNameGeneratorFactory
            , IRelationalTypeMapper relationalTypeMapper
            , SelectExpression selectExpression) : base(relationalCommandBuilderFactory, sqlGenerationHelper, parameterNameGeneratorFactory, relationalTypeMapper, selectExpression)
        {
            
        }

        public override Expression VisitSelect(SelectExpression selectExpression)
        {
            if (selectExpression.Predicate != null)
            {
                var newExpr = new NullComparisonTransformingVisitor(this.ParameterValues).Visit(selectExpression.Predicate);

                var relationalNullsOptimizedExpandingVisitor = new RelationalNullsOptimizedExpandingVisitor2();
                var optimizedExpression = relationalNullsOptimizedExpandingVisitor.Visit(newExpr);

                //var optimizedPredicate = ApplyOptimizations(selectExpression.Predicate, searchCondition: true);
                //if (optimizedPredicate != null)
                //{
                //    Sql.AppendLine()
                //        .Append("WHERE ");

                //    Visit(optimizedPredicate);
                //}
            }

            return base.VisitSelect(selectExpression);
        }

        public override Expression Visit(Expression expression)
        {
            return base.Visit(expression);
        }

        //private Expression ApplyOptimizations(Expression expression, bool searchCondition)
        //{
        //    var newExpression
        //        = new NullComparisonTransformingVisitor(_parametersValues)
        //            .Visit(expression);

        //    var relationalNullsOptimizedExpandingVisitor = new RelationalNullsOptimizedExpandingVisitor();
        //    var optimizedExpression = relationalNullsOptimizedExpandingVisitor.Visit(newExpression);

        //    newExpression
        //        = relationalNullsOptimizedExpandingVisitor.IsOptimalExpansion
        //            ? optimizedExpression
        //            : new RelationalNullsExpandingVisitor().Visit(newExpression);

        //    newExpression = new PredicateReductionExpressionOptimizer().Visit(newExpression);
        //    newExpression = new PredicateNegationExpressionOptimizer().Visit(newExpression);
        //    newExpression = new ReducingExpressionVisitor().Visit(newExpression);
        //    var searchConditionTranslatingVisitor = new SearchConditionTranslatingVisitor(searchCondition);
        //    newExpression = searchConditionTranslatingVisitor.Visit(newExpression);

        //    if (searchCondition && !SearchConditionTranslatingVisitor.IsSearchCondition(newExpression))
        //    {
        //        var constantExpression = newExpression as ConstantExpression;
        //        if ((constantExpression != null)
        //            && (bool)constantExpression.Value)
        //        {
        //            return null;
        //        }
        //        return Expression.Equal(newExpression, Expression.Constant(true, typeof(bool)));
        //    }

        //    return newExpression;
        //}

        private class NullComparisonTransformingVisitor : RelinqExpressionVisitor
        {
            private readonly IReadOnlyDictionary<string, object> _parameterValues;

            public NullComparisonTransformingVisitor(IReadOnlyDictionary<string, object> parameterValues)
            {
                _parameterValues = parameterValues;
            }

            protected override Expression VisitBinary(BinaryExpression expression)
            {
                if (expression.NodeType == ExpressionType.Equal
                    || expression.NodeType == ExpressionType.NotEqual)
                {
                    var leftExpression = expression.Left.RemoveConvert();
                    var rightExpression = expression.Right.RemoveConvert();

                    var parameter
                        = rightExpression as ParameterExpression
                          ?? leftExpression as ParameterExpression;

                    object parameterValue;
                    if (parameter != null
                        && _parameterValues.TryGetValue(parameter.Name, out parameterValue))
                    {
                        if (parameterValue == null)
                        {
                            var columnExpression
                                = leftExpression.TryGetColumnExpression()
                                  ?? rightExpression.TryGetColumnExpression();

                            if (columnExpression != null)
                            {
                                return
                                    expression.NodeType == ExpressionType.Equal
                                        ? (Expression)new IsNullExpression(columnExpression)
                                        : Expression.Not(new IsNullExpression(columnExpression));
                            }
                        }

                        var constantExpression
                            = leftExpression as ConstantExpression
                              ?? rightExpression as ConstantExpression;

                        if (constantExpression != null)
                        {
                            if (parameterValue == null
                                && constantExpression.Value == null)
                            {
                                return
                                    expression.NodeType == ExpressionType.Equal
                                        ? Expression.Constant(true)
                                        : Expression.Constant(false);
                            }

                            if ((parameterValue == null && constantExpression.Value != null)
                                || (parameterValue != null && constantExpression.Value == null))
                            {
                                return
                                    expression.NodeType == ExpressionType.Equal
                                        ? Expression.Constant(false)
                                        : Expression.Constant(true);
                            }
                        }
                    }
                }

                return base.VisitBinary(expression);
            }
        }

        public class RelationalNullsOptimizedExpandingVisitor2 : RelationalNullsExpressionVisitorBase
        {
            /// <summary>
            ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            public virtual bool IsOptimalExpansion { get; private set; } = true;

            /// <summary>
            ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            protected override Expression VisitBinary(BinaryExpression node)
            {
                var newLeft = Visit(node.Left);
                var newRight = Visit(node.Right);

                if (!IsOptimalExpansion)
                {
                    return node;
                }

                if ((node.NodeType == ExpressionType.Equal)
                    || (node.NodeType == ExpressionType.NotEqual))
                {
                    var leftIsNull = BuildIsNullExpression(newLeft);
                    var rightIsNull = BuildIsNullExpression(newRight);

                    var leftNullable = leftIsNull != null;
                    var rightNullable = rightIsNull != null;

                    if ((node.NodeType == ExpressionType.Equal)
                        && leftNullable
                        && rightNullable)
                    {
                        return Expression.OrElse(
                            Expression.Equal(newLeft, newRight),
                            Expression.AndAlso(leftIsNull, rightIsNull));
                    }

                    if ((node.NodeType == ExpressionType.NotEqual)
                        && (leftNullable || rightNullable))
                    {
                        IsOptimalExpansion = false;
                    }
                }

                return node.Update(newLeft, node.Conversion, newRight);
            }

            /// <summary>
            ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            protected override Expression VisitUnary(UnaryExpression node)
            {
                var operand = Visit(node.Operand);

                if (!IsOptimalExpansion)
                {
                    return node;
                }

                if (node.NodeType == ExpressionType.Not)
                {
                    IsOptimalExpansion = false;
                }

                return node.Update(operand);
            }

            protected override Expression BuildIsNullExpression(Expression expression)
            {
                var isNullExpressionBuilder = new IsNullExpressionBuildingVisitor2();

                isNullExpressionBuilder.Visit(expression);

                return isNullExpressionBuilder.ResultExpression;
            }
        }

        public class IsNullExpressionBuildingVisitor2 : RelinqExpressionVisitor
        {
            private bool _nullConstantAdded;

            /// <summary>
            ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            public virtual Expression ResultExpression { get; private set; }

            /// <summary>
            ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (node.Value == null
                    && !_nullConstantAdded)
                {
                    AddToResult(new IsNullExpression(node));
                    _nullConstantAdded = true;
                }

                return node;
            }

            /// <summary>
            ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            protected override Expression VisitBinary(BinaryExpression node)
            {
                // a ?? b == null <-> a == null && b == null
                if (node.NodeType == ExpressionType.Coalesce)
                {
                    var current = ResultExpression;
                    ResultExpression = null;
                    Visit(node.Left);
                    var left = ResultExpression;

                    ResultExpression = null;
                    Visit(node.Right);
                    var right = ResultExpression;

                    var coalesce = CombineExpressions(left, right, ExpressionType.AndAlso);

                    ResultExpression = current;
                    AddToResult(coalesce);
                }

                // a && b == null <-> a == null && b != false || a != false && b == null
                // this transformation would produce a query that is too complex
                // so we just wrap the whole expression into IsNullExpression instead.
                if ((node.NodeType == ExpressionType.AndAlso)
                    || (node.NodeType == ExpressionType.OrElse))
                {
                    AddToResult(new IsNullExpression(node));
                }

                return node;
            }

            /// <summary>
            ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            protected override Expression VisitExtension(Expression node)
            {
                var aliasExpression = node as AliasExpression;
                if (aliasExpression != null)
                {
                    return Visit(aliasExpression.Expression);
                }

                var notNullableExpression = node as NotNullableExpression;
                if (notNullableExpression != null)
                {
                    return node;
                }

                var columnExpression = node as ColumnExpression
                                       ?? node.TryGetColumnExpression();

                if (columnExpression?.Property != null && columnExpression.Property.IsNullable)
                {
                    AddToResult(new IsNullExpression(node));

                    return node;
                }

                var isNullExpression = node as IsNullExpression;
                if (isNullExpression != null)
                {
                    return node;
                }

                var inExpression = node as InExpression;
                if (inExpression != null)
                {
                    return node;
                }

                return node;
            }

            /// <summary>
            ///     This API supports the Entity Framework Core infrastructure and is not intended to be used 
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            protected override Expression VisitConditional(ConditionalExpression node)
            {
                var current = ResultExpression;

                ResultExpression = null;
                Visit(node.IfTrue);
                var ifTrue = ResultExpression;

                ResultExpression = null;
                Visit(node.IfTrue);
                var ifFalse = ResultExpression;

                ResultExpression = current;

                // condition ? ifTrue : ifFalse == null <-> (condition == true && ifTrue == null) || condition != true && ifFalse == null)
                // this transformation would produce a query that is too complex
                // so we just wrap the whole expression into IsNullExpression instead.
                //
                // small optimization: expression can only be nullable if either (or both) of the possible results (ifTrue, ifFalse) can be nullable
                if ((ifTrue != null)
                    || (ifFalse != null))
                {
                    AddToResult(new IsNullExpression(node));
                }

                return node;
            }

            private static Expression CombineExpressions(
                Expression left, Expression right, ExpressionType expressionType)
            {
                if ((left == null)
                    && (right == null))
                {
                    return null;
                }

                if ((left != null)
                    && (right != null))
                {
                    return expressionType == ExpressionType.AndAlso
                        ? Expression.AndAlso(left, right)
                        : Expression.OrElse(left, right);
                }

                return left ?? right;
            }

            private void AddToResult(Expression expression)
                => ResultExpression = CombineExpressions(ResultExpression, expression, ExpressionType.OrElse);
        }
    }
}
