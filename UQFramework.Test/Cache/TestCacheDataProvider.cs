using System.Collections.Generic;
using System.Reflection;
using UQFramework.Cache;

namespace UQFramework.Test
{
    internal class TestCacheDataProvider<T> : CachedDataProvider<T> where T : new()
    {
        public TestCacheDataProvider(IEnumerable<PropertyInfo> cachedProperties, PropertyInfo keyProperty, PersistentCacheProviderBase<T> persistentCacheProvider) : base(cachedProperties, keyProperty, persistentCacheProvider)
        {
        }

        protected override T CreateEntityFromCachedEntry(T cachedEntry)
        {
            CreateEntityFromCachedEntryCount++;
            return base.CreateEntityFromCachedEntry(cachedEntry);
        }

        public int CreateEntityFromCachedEntryCount { get; private set; }
    }
}
