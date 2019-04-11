using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UQFramework.Attributes;
using UQFramework.Cache;
using UQFramework.Configuration;
using UQFramework.DAO;

namespace UQFramework
{
    public abstract class UQContext
    {
        private readonly ITransactionService _transactionService;
        private readonly IReadOnlyDictionary<string, object> _properties;
        private readonly string _dataStoreSetId;

        protected UQContext(string dataStoreSetId) : this(dataStoreSetId, null, null)
        {
        }

        protected UQContext(string dataStoreSetId, ITransactionService transactionService) : this(dataStoreSetId, transactionService, null)
        {
        }

        protected UQContext(string dataStoreSetId, IReadOnlyDictionary<string, object> properties) : this(dataStoreSetId, null, properties)
        {
        }

        protected UQContext(string dataStoreSetId, ITransactionService transactionService, IReadOnlyDictionary<string, object> properties)
        {
            if (string.IsNullOrEmpty(dataStoreSetId))
                throw new ArgumentNullException(nameof(dataStoreSetId));

            _dataStoreSetId = dataStoreSetId;
            _transactionService = transactionService;
            _properties = properties;

            InitializeCollections();
        }

        private IEnumerable<ISavableDataEx> GetCollections()
        {
            return GetType().GetProperties()
                        .Where(p => p.PropertyType.GetGenericTypeDefinition() == typeof(IUQCollection<>))
                        .Select(p => (ISavableDataEx)p.GetValue(this))
                        .Where(c => c != null); //pick only initialized collections
        }

        private void InitializeCollections()
        {
            var collectionsToInitialize = GetType().GetProperties()
                    .Where(p => p.PropertyType.GetGenericTypeDefinition() == typeof(IUQCollection<>))
                    .Select(p => new
                    {
                        Property = p,
                        Attribute = p.GetCustomAttributes(typeof(DataAccessObjectAttribute), false).FirstOrDefault() as DataAccessObjectAttribute
                    })
                    .Where(p => p.Attribute != null);

            foreach (var item in collectionsToInitialize)
            {
                var newCollection = Activator.CreateInstance(typeof(UQCollection<>).MakeGenericType(item.Property.PropertyType.GenericTypeArguments), true);
                var dao = Activator.CreateInstance(item.Attribute.DataAccessObjectType);

                if (dao is INeedDataSourceProperties dataSourcePropertiesRequestor)
                    dataSourcePropertiesRequestor.SetProperties(_properties);

                var persistentCache = item.Attribute.DisableCache ? null : CacheInitializer.GetCachedDataProvider(item.Property.PropertyType.GenericTypeArguments[0], _dataStoreSetId, UQConfiguration.Instance.HorizontalCacheConfiguration as IHorizontalCacheConfigurationInternal, dao);

                (newCollection as IUQCollectionInitializer).Initialize(dao, persistentCache);
                item.Property.SetValue(this, newCollection);
            }
        }

        public void SaveChanges(string commitMessage = null)
        {
            if (_transactionService != null)
                _transactionService.BeginTransaction();

            try
            {
                // get collections
                var collections = GetCollections().ToList();
                // 1. Save changes

                foreach (var collection in collections)
                    collection.SaveChanges();

                // 2. Update Cache
                Parallel.ForEach(collections, c => c.UpdateCacheWithPendingChanges());
                //foreach (var collection in collections)
                //    collection.UpdateCacheWithPendingChanges();

                // 3. Commit changes
                if (_transactionService != null)
                    _transactionService.CommitChanges(commitMessage);
            }
            catch
            {
                if (_transactionService != null)
                    _transactionService.Rollback();

                throw;
            }
        }

        public void NotifyCacheItemsExpired(string uqCollectionName, IEnumerable<string> identifiers)
        {
            var prop = GetType().GetProperty(uqCollectionName);

            if (prop == null)
                return;

            var collection = prop.GetValue(this);

            if (!(collection is IUQCollectionCacheRebuilder cacheRebuilder))
                return;

            cacheRebuilder.NotifyCacheItemsExpired(identifiers);
        }

        public void NotifyCacheExpired(string uqCollectionName)
        {
            var prop = GetType().GetProperty(uqCollectionName);

            if (prop == null)
                return;

            var collection = prop.GetValue(this);

            if (!(collection is IUQCollectionCacheRebuilder cacheRebuilder))
                return;

            cacheRebuilder.NotifyCacheExpired();
        }

        public void NotifyCacheExpiredAllCollections()
        {
            var collections = GetCollections().ToList();

            Parallel.ForEach(collections, c =>
            {
                if (c is IUQCollectionCacheRebuilder cacheRebuilder)
                    cacheRebuilder.NotifyCacheExpired();
            });
        }
    }
}
