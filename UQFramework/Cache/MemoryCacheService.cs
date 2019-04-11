using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;

namespace UQFramework.Cache
{
    internal class MemoryCacheService<T> : IMemoryCacheService<T>, IRefreshableData
    {
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private readonly IPersistentCacheProvider<T> _cacheProvider;
        private readonly string _cacheKey;

        public MemoryCacheService(IPersistentCacheProvider<T> cacheProvider)
        {
            if (cacheProvider == null)
                throw new ArgumentNullException(nameof(cacheProvider));

            _cacheKey = cacheProvider.UniqueCacheKey;
            _cacheProvider = cacheProvider;
        }

        public void NotifyUpdated(IEnumerable<string> identifiers)
        {
            if (!(identifiers?.Any() ?? false))
                return;

            _cacheLock.EnterWriteLock(); // YSV: this lock does not make sense for is it not singleton, make it static?
            try
            {
                var lstIdentifiers = identifiers.ToList();
                var updatedItems = _cacheProvider.RebuildItems(lstIdentifiers);

                var cache = MemoryCache.Default;

                if (!(cache[_cacheKey] is IDictionary<string, T> data))
                    return; // nothing to update, cache will be read when next time requested;

                // do incremental update
                foreach (var id in lstIdentifiers)
                {
                    if (!updatedItems.ContainsKey(id))
                    {
                        data.Remove(id);
                    }
                    else
                    {
                        data[id] = updatedItems[id];
                    }
                }
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

        public void NotifyFullRefreshRequired()
        {
            var cache = MemoryCache.Default;
            cache.Remove(_cacheKey);
            _cacheProvider.FullRebuild();
        }

        #region Helpers

        private TReturnType ReadAllCache<TReturnType>(Func<IDictionary<string, T>, TReturnType> func)
        {
            _cacheLock.EnterReadLock();
            try
            {
                var cache = MemoryCache.Default;

                if (cache[_cacheKey] is IDictionary<string, T> result)
                    return func(result);

                var data = _cacheProvider.GetAllCachedItems();

                SetCache(cache, data);

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
                return Enumerable.Empty<TReturnType>(); //yield break;


            var list = identifiers.ToList();

            if (!list.Any())
                return Enumerable.Empty<TReturnType>(); //yield break; // empty result on absense of a filter;

            _cacheLock.EnterReadLock();
            try
            {
                var cache = MemoryCache.Default;

                if (!(cache[_cacheKey] is IDictionary<string, T> data))
                {
                    data = _cacheProvider.GetAllCachedItems();

                    SetCache(cache, data);
                }

                // YSV: not happy doing ToList() here - but otherwise lock would not make sense, also it does not square well with iterators
                return identifiers.Where(i => data.ContainsKey(i)).Select(id => resultProvider(id, data[id])).ToList();

                //                foreach (var id in identifiers)
                //                {
                //                    if (data.TryGetValue(id, out var item))
                //                        yield return resultProvider(id, item);
                //                }
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }

        #endregion

        private void SetCache(MemoryCache cache, IDictionary<string, T> data)
        {
            cache.Set(_cacheKey, data, ObjectCache.InfiniteAbsoluteExpiration);
        }

    }
}
