using System;
using System.Collections.Generic;
using System.Linq;

namespace UQFramework.Cache
{
    internal abstract class CachedDataProviderBase<TEntity> : ICachedDataProvider<TEntity>, ICachedDataProviderEx
    {
        //YSV: consider creating memorycache on the fly, review locking in memory cache service
        private readonly IMemoryCacheService<TEntity> _memoryCacheService;
        protected CachedDataProviderBase(IPersistentCacheProvider<TEntity> persistentCacheProvider)
        {
            if (persistentCacheProvider == null)
                throw new ArgumentNullException(nameof(persistentCacheProvider));

            _memoryCacheService = new MemoryCacheService<TEntity>(persistentCacheProvider);
        }

        protected abstract TEntity CreateEntityFromCachedEntry(TEntity cachedEntry);

        public abstract IEnumerable<string> GetCachedProperties();

        public IEnumerable<TEntity> GetCachedEntities()
        {
            return _memoryCacheService.GetAllCache();
        }

        public IEnumerable<TEntity> GetCachedEntities(IEnumerable<string> identifiers)
        {
            return _memoryCacheService.GetCachedEntitiesById(identifiers);
        }

        public IEnumerable<TEntity> GetEntitiesBasedOnCache()
        {
            return _memoryCacheService.GetAllCache().Select(CreateEntityFromCachedEntry);
        }

        public IEnumerable<TEntity> GetEntitiesBasedOnCache(IEnumerable<string> identifiers)
        {
            return _memoryCacheService.GetCachedEntitiesById(identifiers).Select(CreateEntityFromCachedEntry);
        }

        public IEnumerable<TEntity> GetEntitiesBasedOnCache(Func<TEntity, bool> filter)
        {
            return _memoryCacheService.GetCache(filter).Select(CreateEntityFromCachedEntry);
        }

        // returns identifiers from cache based on incoming list (some identifiers might not exist)
        public IEnumerable<string> GetIdentifiersFromCache(IEnumerable<string> identifiers)
        {
            return _memoryCacheService.GetExistingIdentifiersFromRange(identifiers);
        }

        public IEnumerable<string> GetIdentifiersFromCache(Func<TEntity, bool> predicate)
        {
            return _memoryCacheService.GetExistingIdentifiers(predicate);
        }

        public IEnumerable<string> GetIdentifiersFromCache()
        {
            return _memoryCacheService.GetIdentifiersOfAllCachedItems();
        }

        void ICachedDataProviderEx.UpdateCacheService(IEnumerable<string> identifiers)
        {
            if (_memoryCacheService is IRefreshableData refreshableData)
                refreshableData.NotifyUpdated(identifiers);
        }

        void ICachedDataProviderEx.UpdateCacheServiceAll()
        {
            if (_memoryCacheService is IRefreshableData refreshableData)
                refreshableData.NotifyFullRefreshRequired();
        }
    }
}
