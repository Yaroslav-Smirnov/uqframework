using System;
using System.Linq.Expressions;
using System.Reflection;
using UQFramework.Queryables.ExpressionHelpers;

namespace UQFramework.Queryables.ExpressionAnalysis
{
    internal static class MethodCallExpressionAnalyser
    {
        internal static bool GetExressionInfo<T>(Expression expression, PropertyInfo keyProperty, out ExpressionInfo<T> expressionInfo)
        {
            expressionInfo = null;

            // get the inner most method
            var methodFinder = new InnermostMethodFinder(typeof(T));
            methodFinder.Visit(expression);

            var innerMostExpression = methodFinder.InnermostMethodExpression;
            var innerMostExpressionParent = methodFinder.InnermostMethodParentExpression;

            if (innerMostExpression.Arguments.Count > 2 || innerMostExpression.Arguments.Count < 1)
                return false;

            var firstArgument = innerMostExpression.Arguments[0];

            if (!(firstArgument is ConstantExpression) || !typeof(IUQCollection<T>).IsAssignableFrom(firstArgument.Type))
                return false;

            if (innerMostExpression.Arguments.Count == 1) //Empty filter, like Any(), FirstOrDefault() etc.
            {
                expressionInfo = new ExpressionInfo<T>()
                {
                    OriginalExpression = expression,
                    InnerMostExpression = innerMostExpression,
                    InnerMostParentExpression = innerMostExpressionParent,
                    Filter = null,
                    FilterInfo = FilterAnalisysResult.Empty,
                };
            }
            else
            {
                var secondArgument = innerMostExpression.Arguments[1];

                if ((secondArgument is UnaryExpression unaryExpression) && unaryExpression.Operand is Expression<Func<T, bool>> filter)
                {
                    expressionInfo = new ExpressionInfo<T>()
                    {
                        OriginalExpression = expression,
                        InnerMostExpression = innerMostExpression,
                        InnerMostParentExpression = innerMostExpressionParent,
                        Filter = filter,
                    };

                    var filterAnalyser = new FilterAnalizer();
                    expressionInfo.FilterInfo = filterAnalyser.Process(innerMostExpression, typeof(T), keyProperty);
                }
                else
                {
                    expressionInfo = new ExpressionInfo<T>()
                    {
                        OriginalExpression = expression,
                        InnerMostExpression = innerMostExpression,
                        InnerMostParentExpression = innerMostExpressionParent,
                        Filter = null,
                        FilterInfo = FilterAnalisysResult.Empty,
                    };
                }
            }
            return true;
        }
    }
}
