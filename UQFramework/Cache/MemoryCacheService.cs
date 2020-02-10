using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;

namespace UQFramework.Cache
{
    internal class MemoryCacheService<T> : ICacheService<T>, IRefreshableData
    {
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private readonly IPersistentCacheProvider<T> _cacheProvider;
        private readonly string _cacheKey;
        private readonly string _cacheUpdateNumberKey;

        public MemoryCacheService(IPersistentCacheProvider<T> cacheProvider)
        {
            if (cacheProvider == null)
                throw new ArgumentNullException(nameof(cacheProvider));

            _cacheKey = cacheProvider.UniqueCacheKey;
            _cacheUpdateNumberKey = $"Update_{_cacheKey}";
            _cacheProvider = cacheProvider;
        }

        public void NotifyUpdated(IEnumerable<string> identifiers)
        {
            if (!(identifiers?.Any() ?? false))
                return;

            _cacheLock.EnterWriteLock();
            try
            {
                // rebuild items anyway
                var lstIdentifiers = identifiers.ToList();
                var updatedItems = _cacheProvider.RebuildItems(lstIdentifiers);

                var cache = MemoryCache.Default;

                if (!(cache[_cacheKey] is IDictionary<string, T> data))
                    return; // nothing to update, cache will be read when next time requested;

                if (!(_cacheProvider is IPersistentCacheProviderEx<T> _cacheProviderWithDelta))
                {
                    ApplyDelta(data, lstIdentifiers, updatedItems);
                    return;
                }

                var lastUpdate = cache[_cacheUpdateNumberKey] as long?;
                var (replacedItemsIdentifiers, newItems, lastChange) = _cacheProviderWithDelta.GetDelta(lastUpdate);

                ApplyDelta(data, replacedItemsIdentifiers, newItems);
                SetCache(cache, lastChange);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        public IEnumerable<T> GetCachedEntitiesById(IEnumerable<string> identifiers)
        {
            return EnumerateCacheItemsFromGivenRange(identifiers, (id, result) => result);
        }

        public IEnumerable<string> GetExistingIdentifiersFromRange(IEnumerable<string> identifiers)
        {
            return EnumerateCacheItemsFromGivenRange(identifiers, (id, result) => id);
        }

        public IEnumerable<string> GetExistingIdentifiers(Func<T, bool> predicate)
        {
            if (predicate == null)
                return ReadAllCache(d => d.Keys);

            return ReadAllCache(d => d.Where(kvp => predicate(kvp.Value)).Select(kvp => kvp.Key));
        }

        public IEnumerable<string> GetIdentifiersOfAllCachedItems()
        {
            return ReadAllCache(d => d.Keys);
        }

        public IEnumerable<T> GetAllCache()
        {
            return ReadAllCache(d => d.Values);
        }

        public IEnumerable<T> GetCache(Func<T, bool> filter)
        {
            return ReadAllCache(d => d.Values.Where(filter));
        }

        public void NotifyCacheExpired()
        {
            var cache = MemoryCache.Default;
            cache.Remove(_cacheKey);
        }

        #region Helpers

        private void ApplyDelta(IDictionary<string, T> data, IEnumerable<string> replacedItemsIdentifiers, IDictionary<string, T> newItems)
        {
            // do incremental update
            foreach (var id in replacedItemsIdentifiers)
            {
                if (!newItems.ContainsKey(id))
                {
                    data.Remove(id);
                }
                else
                {
                    data[id] = newItems[id];
                }
            }
        }

        private TReturnType ReadAllCache<TReturnType>(Func<IDictionary<string, T>, TReturnType> func)
        {
            _cacheLock.EnterReadLock();
            try
            {
                var cache = MemoryCache.Default;
                var lastChange = _cacheProvider.LastChanged;
                var savedLastChange = cache[_cacheUpdateNumberKey] as long?;

                if (cache[_cacheKey] is IDictionary<string, T> result && savedLastChange == lastChange)
                    return func(result);

                var data = cache[_cacheKey] as IDictionary<string, T>;

                if (_cacheProvider is IPersistentCacheProviderEx<T> _cacheProviderWithDelta &&
                    savedLastChange != null && data != null)
                {
                    var (replacedItemsIdentifiers, updatedItems, lastUpdate) = 
                        _cacheProviderWithDelta.GetDelta(savedLastChange);

                    ApplyDelta(data, replacedItemsIdentifiers, updatedItems);
                    SetCache(cache, lastUpdate);
                }
                else
                {
                    data = _cacheProvider.GetAllCachedItems();
                    lastChange = _cacheProvider.LastChanged;
                    SetCache(cache, data, lastChange);
                }

                return func(data);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        private IEnumerable<TReturnType> EnumerateCacheItemsFromGivenRange<TReturnType>(IEnumerable<string> identifiers, Func<string, T, TReturnType> resultProvider)
        {
            if (identifiers == null)
                return Enumerable.Empty<TReturnType>();


            var list = identifiers.ToList();

            if (!list.Any())
                return Enumerable.Empty<TReturnType>();

            _cacheLock.EnterReadLock();
            try
            {
                var cache = MemoryCache.Default;
                var savedLastChange = cache[_cacheUpdateNumberKey] as long?;

                if (!(cache[_cacheKey] is IDictionary<string, T> data && savedLastChange == _cacheProvider.LastChanged))
                {
                    data = cache[_cacheKey] as IDictionary<string, T>;
                    if(_cacheProvider is IPersistentCacheProviderEx<T> _cacheProviderWithDelta && data!=null)
                    {                        
                        var (replacedItemsIdentifiers, updatedItems, lastUpdate) =
                            _cacheProviderWithDelta.GetDelta(savedLastChange);

                        ApplyDelta(data, replacedItemsIdentifiers, updatedItems);
                        SetCache(cache, lastUpdate);
                    }
                    data = _cacheProvider.GetAllCachedItems();                    
                    SetCache(cache, data, _cacheProvider.LastChanged);
                }

                return identifiers.Where(i => data.ContainsKey(i)).Select(id => resultProvider(id, data[id])).ToList();
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        #endregion

        private void SetCache(MemoryCache cache, IDictionary<string, T> data, long cacheTranNumber)
        {
            cache.Set(_cacheKey, data, ObjectCache.InfiniteAbsoluteExpiration);
            SetCache(cache, cacheTranNumber);
        }

        private void SetCache(MemoryCache cache, long cacheTranNumber)
        {
            cache.Set(_cacheUpdateNumberKey, cacheTranNumber, ObjectCache.InfiniteAbsoluteExpiration);
        }
    }
}
