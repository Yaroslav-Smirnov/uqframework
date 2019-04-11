using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UQFramework.Cache;
using UQFramework.Queryables.ExpressionAnalysis;

namespace UQFramework.Queryables.QueryExecutors
{
    internal class CacheBasedQueryExecutor<T> : QueryExecutorBase<T>
    {
        // used if the whole query can be executed on cache
        private readonly ISavableData<T> _savable;
        private readonly ICachedDataProvider<T> _cachedDataProvider;

        private readonly bool _canUseCacheWithoutClonning;// true if there is no danger of exposing references to some data in cache
        internal CacheBasedQueryExecutor(ISavableData<T> savable, ICachedDataProvider<T> cachedDataProvider, ExpressionInfo<T> expressionInfo, bool canUseCacheWithoutClonning)
            : base(expressionInfo)
        {
            _savable = savable;
            _cachedDataProvider = cachedDataProvider;
            _canUseCacheWithoutClonning = canUseCacheWithoutClonning;
        }

        protected override IEnumerable<string> GetIdentifiersToRequest()
        {
            var filterInfo = _expressionInfo.FilterInfo;

            if (filterInfo.IdentifiersRangeFound && filterInfo.IsContainsExpression)
                return _cachedDataProvider.GetIdentifiersFromCache(filterInfo.Identifiers);

            return _cachedDataProvider.GetIdentifiersFromCache(_predicate);
        }

        protected override IEnumerable GetEntities(IEnumerable<string> identifiers)
        {
            var identifiersToRequest = identifiers.Except(_savable.GetAllPendingChangesIdentifiers());

            var entities = GetEntitiesFromCache(identifiers);

            return _savable.CombineWithPendingChanges(entities, _predicate);
        }

        protected override Expression ModifyExpression(Expression expression, IQueryable queryableEntities)
        {
            var method = _expressionInfo.InnerMostExpression.Method;

            if (method.Name == nameof(Enumerable.Where) && _predicate !=null) // we already executed the predicate, so do not need to apply it again
                method = null;

            return ModifyExpressionAndReturnResult(expression, queryableEntities, _expressionInfo.InnerMostExpression, method);
        }

        private IEnumerable<T> GetEntitiesFromCache(IEnumerable<string> identifiers)
        {
            if (!identifiers.Any())
                return Enumerable.Empty<T>();

            if(_canUseCacheWithoutClonning)
                return _cachedDataProvider.GetCachedEntities(identifiers);

            //var returningType = _expressionInfo.OriginalExpression.Type;
            //
            //if (returningType == typeof(T) || typeof(IEnumerable<T>).IsAssignableFrom(returningType))
            return _cachedDataProvider.GetEntitiesBasedOnCache(identifiers); // get cloned entities

            // return from the cache directly for the returning type is different and therefore there is no danger of accidently modifying the cache.

            //return _cachedDataProvider.GetCachedEntities();
        }
    }
}
