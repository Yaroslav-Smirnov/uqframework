using System;
using System.Linq.Expressions;
using UQFramework.Queryables.ExpressionHelpers;

namespace UQFramework.Queryables.ExpressionAnalysis
{
    internal class ExpressionInfo<T>
    {
        internal Expression OriginalExpression { get; set; }
        internal bool IsEnumerableResult { get; set; }
        internal MethodCallExpression InnerMostExpression { get; set; }
        internal MethodCallExpression InnerMostParentExpression { get; set; }
        internal Expression<Func<T, bool>> Filter { get; set; }
        internal FilterAnalisysResult FilterInfo { get; set; }
    }
}
