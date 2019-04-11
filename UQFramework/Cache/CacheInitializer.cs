using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UQFramework.Attributes;
using UQFramework.Configuration;
using UQFramework.DAO;
using UQFramework.Helpers;

namespace UQFramework.Cache
{
    // helper class creating cacheprovider based on type
    internal static class CacheInitializer
    {
        // non-generic version
        public static object GetCachedDataProvider(Type entityType, string dataStoreSetId, IHorizontalCacheConfigurationInternal hconfig, object dao)
        {
            var keyProperty = GeneralHelper.GetIdentifierProperty(entityType);

            if (keyProperty == null) // should not be the case, but not throwing
                return null;

            var cachedProperties = GeneralHelper.GetPropertiesHavingAttribute(entityType, typeof(CachedAttribute));

            if (!cachedProperties.Any())
                return null;

            // include key property
            cachedProperties = cachedProperties.Prepend(keyProperty).ToArray();

            var persistentCacheProvider = CreateCacheProviderFromConfig(entityType, dataStoreSetId, hconfig, dao, cachedProperties);

            if (persistentCacheProvider == null)
                return null;

            var cachedDataProviderType = (hconfig.CacheDataProviderType ?? typeof(CachedDataProvider<>)).MakeGenericType(entityType);

            return Activator.CreateInstance(cachedDataProviderType, cachedProperties, keyProperty, persistentCacheProvider);
        }

        private static object CreateCacheProviderFromConfig(Type entityType, string dataStoreSetId, IHorizontalCacheConfiguration hconfig, object dao, IEnumerable<PropertyInfo> cachedProperties)
        {
            var cacheProviderType = GetProviderType(hconfig);

            if (cacheProviderType == null)
                return null;

            var dataSourceEnumerator = typeof(IDataSourceEnumerator<>).MakeGenericType(entityType);

            if (!dataSourceEnumerator.IsAssignableFrom(dao.GetType()))
                return null;

            var persistentCacheProviderType = cacheProviderType.MakeGenericType(entityType);

            return Activator.CreateInstance(persistentCacheProviderType, dataStoreSetId, hconfig.GetAllParameters(), dao, cachedProperties);
        }

        private static Type GetProviderType(IHorizontalCacheConfiguration hconfig)
        {
            if (hconfig == null)
                return null;

            if (!hconfig.IsEnabled)
                return null;

            return hconfig.ProviderType;
        }
    }
}
