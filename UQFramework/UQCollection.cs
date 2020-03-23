using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UQFramework.Cache;
using UQFramework.DAO;
using UQFramework.Helpers;
using UQFramework.Queryables;

namespace UQFramework
{
    public partial class UQCollection<T> : QueryableData<T>, IUQCollection<T>, ISavableData<T>, IUQCollectionInitializer, IUQCollectionCacheRebuilder
    {
        private readonly PropertyInfo _keyProperty;
        private readonly Func<T, string> _identifierGetter;
        protected ICachedDataProvider<T> _cachedDataProvider;

        private object _dataAccessObject;

        // To be used when initialized with Reflection
        internal UQCollection()
        {
            _identifierGetter = GeneralHelper.GetIdentiferGetter<T>(out _keyProperty);
        }

        void IUQCollectionInitializer.Initialize(object dataAccessObject, object cacheProvider)
        {
            _dataAccessObject = dataAccessObject;
            _cachedDataProvider = cacheProvider as ICachedDataProvider<T>;
            Provider = GetQueryProvider();
        }

        private Dictionary<string, T> _deletedItems = new Dictionary<string, T>();
        private List<T> _addedItems = new List<T>();
        private Dictionary<string, T> _updatedItems = new Dictionary<string, T>();

        public void Add(T item)
        {
            _addedItems.Add(item);

            var itemKey = _identifierGetter(item);

            if (string.IsNullOrEmpty(itemKey))
                return;

            _updatedItems.Remove(itemKey);
            _deletedItems.Remove(itemKey);
        }

        public void Remove(T item)
        {
            var itemKey = _identifierGetter(item);

            if (string.IsNullOrEmpty(itemKey)) // we might want to remove an item we previously added
            {
                _addedItems.Remove(item);
            }
            else // updating the list using the item key
            {
                var addedItem = _addedItems.FirstOrDefault(i => _identifierGetter(i) == itemKey);
                if (addedItem != null)
                    _addedItems.Remove(addedItem);

                _updatedItems.Remove(itemKey);
                _deletedItems[itemKey] = item;
            }
        }

        public void Update(T item)
        {
            var itemKey = _identifierGetter(item);

            if (string.IsNullOrEmpty(itemKey))
                throw new InvalidOperationException("ID of an item being updated must not be empty");

            _updatedItems[itemKey] = item;

            var addedItem = _addedItems.FirstOrDefault(i => _identifierGetter(i) == itemKey);
            if (addedItem != null)
                _addedItems.Remove(addedItem);

            _deletedItems.Remove(itemKey);
        }

        public TResult Query<TResult>(string queryName, params object[] parameters)
        {
            // YSV: consider compilation the way it is done in the cache system
            var type = _dataAccessObject.GetType();
            var parameterTypes = parameters.Select(p => p.GetType()).ToArray();
            var methodInfo = _dataAccessObject.GetType().GetMethod(queryName, parameterTypes);
            if (methodInfo == null)
                throw new InvalidOperationException($"DAO of type {type} does not have a method {queryName} accepting the provided parameters");

            return (TResult)methodInfo.Invoke(_dataAccessObject, parameters);
        }

        IEnumerable<T> ISavableData<T>.CombineWithPendingChanges(IEnumerable<T> data, Func<T, bool> filter)
        {
            if (!_deletedItems.Any() && !_addedItems.Any() && !_updatedItems.Any())
                return data;

            return data
                    .Where(i => !_deletedItems.Keys.Contains(_identifierGetter(i))
                                && !_updatedItems.Keys.Contains(_identifierGetter(i))
                                && !_addedItems.Contains(i))
                    .Concat(filter == null ? _updatedItems.Values : _updatedItems.Values.Where(filter))
                    .Concat(filter == null ? _addedItems : _addedItems.Where(filter));

        }

        IEnumerable<string> ISavableData<T>.CombineIdentifiersRangeWithPendingChanges(IEnumerable<string> identifiers, Func<T, bool> filter)
        {
            if (!_deletedItems.Any() && !_addedItems.Any() && !_updatedItems.Any())
                return identifiers;

            return identifiers.Except(_deletedItems.Keys)
                .Union(filter == null ? _updatedItems.Keys : _updatedItems.Where(kvp => filter(kvp.Value)).Select(x => x.Key))
                .Union(filter == null ? _addedItems.Select(_identifierGetter) : _addedItems.Where(filter).Select(_identifierGetter));
        }

        IEnumerable<T> ISavableData<T>.GetAllPendingChanges()
        {
            return _addedItems.Concat(
                _updatedItems.Values)
                .Concat(_deletedItems.Values);
        }

        IEnumerable<string> ISavableData<T>.GetAllPendingChangesIdentifiers()
        {
            return (this as ISavableData<T>).GetAllPendingChanges().Select(_identifierGetter);
        }

        IEnumerable<T> ISavableData<T>.PendingDelete => _deletedItems.Values;

        IEnumerable<T> ISavableData<T>.PendingUpdate => _updatedItems.Values;

        IEnumerable<T> ISavableData<T>.PendingAdd => _addedItems;

        IEnumerable<(Type type, string id, object entity)> ISavableDataEx.PendingAdd => _addedItems.Select(x => (typeof(T), _identifierGetter(x), (object)x)).ToList();

        IEnumerable<(Type type, string id, object entity)> ISavableDataEx.PendingUpdate => _updatedItems.Values.Select(x => (typeof(T), _identifierGetter(x), (object)x)).ToList();

        IEnumerable<(Type type, string id, object entity)> ISavableDataEx.PendingDelete => _deletedItems.Values.Select(x => (typeof(T), _identifierGetter(x), (object)x)).ToList();

        private IQueryProvider GetQueryProvider()
        {
            //var queryContext = new QueryContext<T>(GetEntitiesConsideringPending, GetEntitiesIdentifiersConsideringPending, GetEntitiesConsideringPending);
            return new QueryProvider(this);
        }


        #region Getting Entities By identifiers

        /*
        private IEnumerable<T> GetEntitiesConsideringPending(IEnumerable<string> identifiers, bool cacheUseAllowed)
        {
            // The method is called from QueryContext. 
            // The contract is: if identifiers is null then all the collection is queried

            var savable = (ISavableData<T>)this;

            var pendingItems = savable.GetAllPendingChanges().ToList();
            var identifiersToExclude = pendingItems.Select(_identifierGetter);

            if (identifiers == null)
            {
                var entities = GetAllEntities(cacheUseAllowed)
                                    .Where(x => !identifiersToExclude.Contains(_identifierGetter(x)));

                return savable.CombineWithPendingChanges(entities, null);
            }

            var filter = identifiers.Except(identifiersToExclude);

            if (!filter.Any())
                return savable.CombineWithPendingChanges(Enumerable.Empty<T>(), x => identifiers.Contains(_identifierGetter(x)));

            return savable.CombineWithPendingChanges(GetEntities(filter, cacheUseAllowed), x => identifiers.Contains(_identifierGetter(x)));
        }*/
        /*
        private IEnumerable<T> GetEntities(IEnumerable<string> identifiers, bool cacheUseAllowed)
        {
            if (_cachedDataProvider != null && cacheUseAllowed)
                return _cachedDataProvider.GetEntitiesBasedOnCache(identifiers);

            return GetEntitiesFromDao(identifiers);
        }

        private IEnumerable<T> GetAllEntities(bool cacheUseAllowed)
        {
            if (_cachedDataProvider != null && cacheUseAllowed)
                return _cachedDataProvider.GetEntitiesBasedOnCache();

            return GetAllEntitiesFromDao();
        }*/
        /*
        private IEnumerable<T> GetAllEntitiesFromDao()
        {
            if (_dataAccessObject is IDataSourceEnumeratorEx<T> dataSourceReaderAll)
                return dataSourceReaderAll.GetAllEntities();

            if (_dataAccessObject is IDataSourceEnumerator<T> dataSourceEnumerator)
            {
                var identifiers = dataSourceEnumerator.GetAllEntitiesIdentifiers();
                return GetEntitiesFromDao(identifiers);
            }

            throw new InvalidOperationException($"DataAccessObject {_dataAccessObject.GetType()} for type {typeof(T).FullName} does not implement {nameof(IDataSourceEnumerator<T>)}");
        }*/

        //YSV: forceNonParallel - quick patch for FirstOrDefault(), need to proper refactor then
        //private IEnumerable<T> GetEntitiesFromDao(IEnumerable<string> identifiers)
        //{
        //    if (_dataAccessObject is IDataSourceBulkReader<T> bulkReader)
        //        return bulkReader.GetEntities(identifiers);

        //    if (_dataAccessObject is IDataSourceReader<T> justReader)
        //        return identifiers.AsParallel().Select(justReader.GetEntity).Where(x => x != null);

        //    throw new InvalidOperationException($"DataAccessObject {_dataAccessObject.GetType()} for type {typeof(T).FullName} does not implement neither {nameof(IDataSourceBulkReader<T>)} nor {nameof(IDataSourceReader<T>)}");
        //}

        #endregion

        void ISavableDataEx.Delete()
        {
            if (!_deletedItems.Any())
                return;

            // Note: assume that the data access object will take care of updating order
            if (_dataAccessObject is IDataSourceBulkWriter<T> bulkWriter)
            {
                bulkWriter.UpdateDataSource(Enumerable.Empty<T>(), Enumerable.Empty<T>(), _deletedItems.Values);
                return;
            }

            // Note: it is assumed that if data access object does not support bulk operations then order is not essential and parallel is fine
            if (_dataAccessObject is IDataSourceWriter<T> justWriter)
            {
                // Delete
                Parallel.ForEach(_deletedItems, item => justWriter.DeleteEntity(item.Value));
            }
        }

        void ISavableDataEx.CreateAndUpdate()
        {
            // if there is nothing to add, update or delete then return
            if (!_updatedItems.Any() && !_addedItems.Any())
                return;

            if (_dataAccessObject is IDataSourceBulkWriter<T> bulkWriter)
            {
                bulkWriter.UpdateDataSource(_addedItems, _updatedItems.Values, Enumerable.Empty<T>());
                return;
            }

            if (_dataAccessObject is IDataSourceWriter<T> justWriter)
            {
                // YSV: assume that parrallel is safe which is true in case of files but 
                // might not be true in case of SQL (e.g. self-referencing table where order is essential)

                // TODO: check self-referencing properties and run in parallel only if there is no problem

                // 2. Add
                Parallel.ForEach(_addedItems, justWriter.AddEntity);

                // 3. Update
                Parallel.ForEach(_updatedItems, item => justWriter.UpdateEntity(item.Value));
            }
        }

        void ISavableDataEx.UpdateCacheWithPendingChanges()
        {
            if (!(_cachedDataProvider is ICachedDataProviderEx cachedDataProviderEx))
                return;

            var pendingObjectsIdentifies = ((ISavableData<T>)this)
                        .GetAllPendingChanges()
                        .Select(_identifierGetter)
                        .Distinct();

            cachedDataProviderEx.UpdateCacheService(pendingObjectsIdentifies);
        }

        void IUQCollectionCacheRebuilder.NotifyCacheExpired()
        {
            if (!(_cachedDataProvider is ICachedDataProviderEx cachedDataProviderEx))
                return;

            cachedDataProviderEx.UpdateCacheServiceAll();
        }

        void IUQCollectionCacheRebuilder.NotifyCacheItemsExpired(IEnumerable<string> identifiers)
        {
            if (!(_cachedDataProvider is ICachedDataProviderEx cachedDataProviderEx))
                return;

            cachedDataProviderEx.UpdateCacheService(identifiers);
        }
    }
}
