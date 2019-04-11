using System;
using System.Linq.Expressions;
using UQFramework.Queryables;
using UQFramework.Queryables.ExpressionAnalysis;
using UQFramework.Queryables.QueryExecutors;

namespace UQFramework
{
    public partial class UQCollection<T> : IQueryContext
    {
        object IQueryContext.Execute(Expression expression, bool isEnumerable)
        {
            var executor = GetExecutor(expression, isEnumerable);
            return executor.Execute(expression, isEnumerable);
        }

        private QueryExecutorBase<T> GetExecutor(Expression expression, bool isEnumerable)
        {
            if (!(expression is MethodCallExpression methodCall))
                return new DefaultQueryExecutor<T>(this, _dataAccessObject, new ExpressionInfo<T>
                {
                    OriginalExpression = expression,
                    IsEnumerableResult = isEnumerable,
                    FilterInfo = FilterAnalisysResult.Empty
                });

            // now we have method call and can process it
            // detect if we can use cache
            var cacheUsage = _cachedDataProvider == null ? CacheUsageAnalysisResult.CannotUseCache : CacheUsageAnalyser.CheckCacheUsage<T>(methodCall, _keyProperty);

            if (!MethodCallExpressionAnalyser.GetExressionInfo<T>(methodCall, _keyProperty, out var expressionInfo))
                throw new NotSupportedException($"Not supported method {methodCall.Method.Name}");

            expressionInfo.IsEnumerableResult = isEnumerable;

            switch (cacheUsage)
            {
                case CacheUsageAnalysisResult.CannotUseCache:
                    return new DefaultQueryExecutor<T>(this, _dataAccessObject, expressionInfo);
                case CacheUsageAnalysisResult.CanGetResultFromIdentifiersOnly:
                    return new IdentifiersBasedQueryExecutor<T>(this, _cachedDataProvider, expressionInfo);
                case CacheUsageAnalysisResult.CanGetResultFromCache:
                    return new CacheBasedQueryExecutor<T>(this, _cachedDataProvider, expressionInfo, false);
                case CacheUsageAnalysisResult.CanGetResultFromCacheWithoutCloning:
                    return new CacheBasedQueryExecutor<T>(this, _cachedDataProvider, expressionInfo, true);
                case CacheUsageAnalysisResult.CanQueryCache:
                    return new DefaultWithCacheQueryExecutor<T>(this, _dataAccessObject, _cachedDataProvider, _identifierGetter, expressionInfo);
                default:
                    throw new NotSupportedException($"Unknown cache usage status {cacheUsage}");
            }
        }
    }
}
