public enum CacheUsageAnalysisResult
{
    // need use dao to calculate the result of the expression
    CannotUseCache = 0,
    // need dao to return result, but get query cache to get identifiers of the items
    CanQueryCache = 1,
    // can use only cache to calculate the result, no need to use DAO
    CanGetResultFromCache = 2,
    // can use only cache to calculate the result, no need to clone (only properties of value types are used in the query)
    CanGetResultFromCacheWithoutCloning = 3,
    // results can be obtained from cache, no need to load entities from cache, can use identifiers only
    CanGetResultFromIdentifiersOnly = 4
}