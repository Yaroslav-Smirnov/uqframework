using System;
using System.Linq.Expressions;
using UQFramework.DAO;
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

        private IQueryExecutor GetExecutor(Expression expression, bool isEnumerable)
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
            var daoCacheProvider = _dataAccessObject as IDataSourceCacheProvider<T>;
            var cacheUsage = _cachedDataProvider == null && daoCacheProvider == null 
                ? CacheUsageAnalysisResult.CannotUseCache 
                : CacheUsageAnalyser.CheckCacheUsage<T>(methodCall, _keyProperty);

            if (!MethodCallExpressionAnalyser.GetExressionInfo<T>(methodCall, _keyProperty, out var expressionInfo))
                throw new NotSupportedException($"Not supported method {methodCall.Method.Name}");

            expressionInfo.IsEnumerableResult = isEnumerable;

            if (_cachedDataProvider == null && cacheUsage != CacheUsageAnalysisResult.CanGetResultFromCache
                && cacheUsage != CacheUsageAnalysisResult.CanGetResultFromCacheWithoutCloning)
                cacheUsage = CacheUsageAnalysisResult.CannotUseCache;

            switch (cacheUsage)
            {
                case CacheUsageAnalysisResult.CannotUseCache:
                    return new DefaultQueryExecutor<T>(this, _dataAccessObject, expressionInfo);
                case CacheUsageAnalysisResult.CanGetResultFromIdentifiersOnly:
                    return new IdentifiersBasedQueryExecutor<T>(this, _cachedDataProvider, expressionInfo);
                case CacheUsageAnalysisResult.CanGetResultFromCache:
                case CacheUsageAnalysisResult.CanGetResultFromCacheWithoutCloning:
                    {
                        if(_cachedDataProvider!=null)
                        {
                            var canSkipCloning = (cacheUsage == CacheUsageAnalysisResult.CanGetResultFromCacheWithoutCloning);
                            return new CacheBasedQueryExecutor<T>(this, _cachedDataProvider, expressionInfo, canSkipCloning);
                        }
                        
                        if(daoCacheProvider !=null)
                            return new CacheProviderQueryExecutor<T>(this, _keyProperty, daoCacheProvider, expressionInfo);

                        throw new InvalidOperationException("Cache is not supported");
                    }
                case CacheUsageAnalysisResult.CanQueryCache:
                    return new DefaultWithCacheQueryExecutor<T>(this, _dataAccessObject, _cachedDataProvider, _identifierGetter, expressionInfo);
                default:
                    throw new NotSupportedException($"Unknown cache usage status {cacheUsage}");
            }
        }
    }
}
