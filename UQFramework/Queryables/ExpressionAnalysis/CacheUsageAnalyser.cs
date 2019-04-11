using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace UQFramework.Queryables.ExpressionAnalysis
{
    internal static class CacheUsageAnalyser
    {
        internal static CacheUsageAnalysisResult CheckCacheUsage<TCachedEntity>(MethodCallExpression expression, PropertyInfo keyProperty)
        {
            var expressionTreeAnalyser = new CacheFocusedExpressionVisitor<TCachedEntity>(keyProperty);
            expressionTreeAnalyser.Visit(expression);

            if (!expressionTreeAnalyser.UsesOnlyCachedProperties)
                return CacheUsageAnalysisResult.CannotUseCache;

            // if we return T or enumeration of T, then we can't use cache
            if (expression.Type == typeof(TCachedEntity) || typeof(IEnumerable<TCachedEntity>).IsAssignableFrom(expression.Type))
                return CacheUsageAnalysisResult.CanQueryCache;

            if (expressionTreeAnalyser.UsesOnlyKeyProperties)
                return CacheUsageAnalysisResult.CanGetResultFromIdentifiersOnly;

            if(!expressionTreeAnalyser.UsesCachedEnumerables)
                return CacheUsageAnalysisResult.CanGetResultFromCacheWithoutCloning;

            return CacheUsageAnalysisResult.CanGetResultFromCache;
        }
    }
}
