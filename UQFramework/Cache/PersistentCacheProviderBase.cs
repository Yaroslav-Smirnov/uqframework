using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UQFramework.DAO;
using UQFramework.Helpers;

namespace UQFramework.Cache
{
    public abstract class PersistentCacheProviderBase<T> : IPersistentCacheProvider<T>
    {
        private readonly IDataSourceReader<T> _dataSourceReader;
        private readonly IDataSourceBulkReader<T> _dataSourceBulkReader;
        private readonly IDataSourceEnumerator<T> _dataSourceEnumerator;
        private readonly Func<T, string> _keyGetter;
        protected readonly IEnumerable<PropertyInfo> _cachedProperties;
		protected readonly string _dataStoreSetId;

        public PersistentCacheProviderBase(string dataStoreSetId, object dataSourceReader, IEnumerable<PropertyInfo> cachedProperties)
        {
            if (dataSourceReader == null)
                throw new ArgumentNullException(nameof(dataSourceReader));

			_dataStoreSetId = dataStoreSetId;

			_dataSourceEnumerator = (dataSourceReader as IDataSourceEnumerator<T>)
                ?? throw new InvalidOperationException("DataAccessObject must implement IDataSourceEnumerator in order to use persistent cache");

            ValidateCacheProperties(cachedProperties);
            _cachedProperties = cachedProperties;

            _keyGetter = GeneralHelper.GetIdentiferGetter<T>(out var notused);

            _dataSourceReader = dataSourceReader as IDataSourceReader<T>;
            _dataSourceBulkReader = dataSourceReader as IDataSourceBulkReader<T>;

            if (_dataSourceBulkReader == null && _dataSourceReader == null)
                throw new InvalidOperationException($"Data Access Object for type {typeof(T)} must implement {nameof(IDataSourceReader<T>)} or {nameof(IDataSourceBulkReader<T>)}");
        }

        protected abstract bool RebuildRequired();

        protected abstract void ReplaceItems(IEnumerable<string> identifiers, IDictionary<string, T> newEntities);

        protected abstract void ReplaceAll(IDictionary<string, T> newEntities);

        protected abstract IDictionary<string, T> ReadAllDataFromCache();

		public abstract DateTimeOffset LastChanged { get; }

		protected string CachedPropertiesString => string.Join(":", _cachedProperties
            .OrderBy(p => p.Name)
            .ThenBy(p => p.PropertyType.FullName)
            .Select(p => $"{p.Name}|{p.PropertyType}")
            );

        public string UniqueCacheKey => _dataStoreSetId + this.GetType().FullName + (_dataSourceReader?.GetType() ?? _dataSourceBulkReader.GetType()).FullName;

		public void FullRebuild()
        {
            var dict = default(IDictionary<string, T>);
            if (_dataSourceEnumerator is IDataSourceEnumeratorEx<T> dataSourceEnumeratorEx)
                dict = dataSourceEnumeratorEx.GetAllEntities().ToDictionary(_keyGetter, item => item);
            else
                dict = GetEntitiesFromDao(_dataSourceEnumerator.GetAllEntitiesIdentifiers())
                        .ToDictionary(_keyGetter, item => item);

            ReplaceAll(dict);
        }

        public IDictionary<string, T> GetAllCachedItems()
        {
            if (RebuildRequired())
                FullRebuild();

            return ReadAllDataFromCache();
        }

        public IDictionary<string, T> RebuildItems(IEnumerable<string> identifiers)
        {
            if (RebuildRequired())
                FullRebuild();

            var items = GetEntitiesFromDao(identifiers.Distinct());

            var cachedData = items.ToDictionary(_keyGetter, item => item);
            ReplaceItems(identifiers, cachedData);

            return cachedData;
        }

        private IEnumerable<T> GetEntitiesFromDao(IEnumerable<string> identifiers)
        {
            if (_dataSourceBulkReader != null)
                return _dataSourceBulkReader.GetEntities(identifiers);

            return identifiers
                        .AsParallel()
                        .Select(i => _dataSourceReader.GetEntity(i))
                        .Where(x => x != null);
        }

        private static void ValidateCacheProperties(IEnumerable<PropertyInfo> cachedProperties)
        {
            if (cachedProperties == null && !cachedProperties.Any())
                throw new ArgumentException($"Cached properties must not be null or empty");

            var nonsupportedProperty = cachedProperties.FirstOrDefault(p => !IsValidPropertyType(p.PropertyType));

            if (nonsupportedProperty != null)
                throw new NotSupportedException($"Cannot cache property {nonsupportedProperty.Name} for caching of type {nonsupportedProperty.PropertyType} is not supported");

        }

        private static bool IsValidPropertyType(Type propertyType)
        {
            if (propertyType.IsValueType || propertyType == typeof(string))
                return true;

            // if not value type allow only IEnumerable<AnyValueTypeHere>
            if (!propertyType.IsGenericType)
                return false;

            var type = typeof(IEnumerable<>).MakeGenericType(propertyType.GenericTypeArguments[0]);

            if (type != propertyType)
                return false;

            if (propertyType.GenericTypeArguments[0].IsValueType || propertyType.GenericTypeArguments[0] == typeof(string))
                return true;

            return false;
        }
    }
}
