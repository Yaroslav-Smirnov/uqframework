using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;
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
		private IList<Type> _savingOrder;

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
						.Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(IUQCollection<>))
						.Select(p => (ISavableDataEx)p.GetValue(this))
						.Where(c => c != null) //; //pick only initialized collections
						.OrderBy(x =>
						{
							var enityType = x.GetType().GenericTypeArguments[0];
							return _savingOrder.IndexOf(enityType);
						});
		}

		private void InitializeCollections()
		{
			var collectionsToInitialize = GetType().GetProperties()
					.Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(IUQCollection<>))
					.Select(p => new
					{
						Property = p,
						Attribute = p.GetCustomAttributes(typeof(DataAccessObjectAttribute), false).FirstOrDefault() as DataAccessObjectAttribute
					})
					.Where(p => p.Attribute != null);

			var relations = new Dictionary<Type, IList<Type>>();

			foreach (var item in collectionsToInitialize)
			{
				var entityType = item.Property.PropertyType.GenericTypeArguments[0];

				var newCollection = Activator.CreateInstance(typeof(UQCollection<>).MakeGenericType(entityType), true);
				var dao = Activator.CreateInstance(item.Attribute.DataAccessObjectType);

				if (dao is INeedDataSourceProperties dataSourcePropertiesRequestor)
					dataSourcePropertiesRequestor.SetProperties(_properties);

				var persistentCache = item.Attribute.DisableCache ? null : CacheInitializer.GetCachedDataProvider(entityType, _dataStoreSetId, UQConfiguration.Instance.HorizontalCacheConfiguration as IHorizontalCacheConfigurationInternal, dao);

				(newCollection as IUQCollectionInitializer).Initialize(dao, persistentCache);
				item.Property.SetValue(this, newCollection);

				// get 'parent' entities:
				var entityRelations = entityType.GetProperties()
											.Select(p => p.GetCustomAttribute(typeof(EntityIdentifierAttribute)))
											.Where(a => a != null)
											.OfType<EntityIdentifierAttribute>()
											.Select(x => x.EntityType)
											.Where(x => x != entityType) // exclude self references
											.ToList();

				relations[entityType] = entityRelations.ToList();
			}

			BuildRelationsOrder(relations);
		}

		private void BuildRelationsOrder(IDictionary<Type, IList<Type>> dict)
		{
			// not expecting big collections here
			_savingOrder = new List<Type>();			
			// find an item which does not have
			while (dict.Any()) // self-referencing types here
			{
				var nextType = dict.FirstOrDefault(x => !x.Value.Any()).Key;

				if (nextType == null)
					throw new InvalidOperationException("There is circular references");

				// add 
				_savingOrder.Add(nextType);

				// remove
				dict.Remove(nextType);
				foreach (var d in dict)
					d.Value.Remove(nextType);
			}
		}

		protected IEnumerable<(Type type, string Id, object entity)> PendingAdd => GetCollections().SelectMany(c => c.PendingAdd).ToList();

		protected IEnumerable<(Type type, string Id, object entity)> PendingUpdate => GetCollections().SelectMany(c => c.PendingUpdate).ToList();

		protected IEnumerable<(Type type, string Id, object entity)> PendingDelete => GetCollections().SelectMany(c => c.PendingDelete).ToList();

		protected virtual void OnBeforeSaveChanges()
		{

		}

		protected virtual void OnBeforeTransactionCommit()
		{

		}

		public void SaveChanges(string commitMessage = null)
		{
			OnBeforeSaveChanges();

			if (_transactionService != null)
				_transactionService.BeginTransaction();

			try
			{
				// get collections
				var collections = GetCollections().ToList();

				using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
				{
					// 1. Save changes maintaining dependencies order
					// 1.a  Delete entities 
					foreach (var collection in collections.Reverse<ISavableDataEx>())
						collection.Delete();

					// 1.b  Create and update 
					foreach (var collection in collections)
						collection.CreateAndUpdate();

					// 2. Commit changes
					if (_transactionService != null)
					{
						OnBeforeTransactionCommit();
						_transactionService.CommitChanges(commitMessage);
					}

					// 3. Update Cache
					Parallel.ForEach(collections, c => c.UpdateCacheWithPendingChanges());

					scope.Complete();
				}
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
