using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UQFramework.Attributes;
using UQFramework.Cache.Providers;
using System.Diagnostics;
using System.Data.SqlClient;
using UQFramework.Cache;

namespace UQFramework.Test.Tests.CacheProviders
{
	[TestClass]
	public class SqlCacheProviderTest
	{
		private const string _connectionString = "Data Source=.;Integrated Security=True";
		private const string _dataStoreId = "42";
		private const int _numberOfItems = 100000;

		private static readonly string _dataBaseName = $"Database{_dataStoreId}";

		private static readonly IDictionary<string, string> _parameters = new Dictionary<string, string>
		{
			["connectionstring"] = _connectionString
		};

		private static readonly PropertyInfo[] _cachedProperties = typeof(DummyEnityWithCachedProperties).GetProperties().Where(p => p.GetCustomAttribute<CachedAttribute>() != null).ToArray();

		private static readonly DummyDataAccessObject<DummyEnityWithCachedProperties> _dataAccessObject =
			new DummyDataAccessObject<DummyEnityWithCachedProperties>(_numberOfItems);

		/// <summary>
		///  Gets or sets the test context which provides
		///  information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext { get; set; }

		[TestMethod]
		public void TestFullRebuid()
		{
			// Arrange
			var sqlCacheProvider = GetSqlCacheProvider();

			// Act
			var sw = new Stopwatch();
			sw.Start();

			sqlCacheProvider.FullRebuild();

			sw.Stop();
			TestContext.WriteLine($"Rebuild: {sw.ElapsedMilliseconds}");

			// Assert (TODO)
		}		

		[TestMethod]
		public void TestGetAllCachedItems()
		{			
			// Arrange
			var sqlCacheProvider = GetSqlCacheProvider();
			var sw = new Stopwatch();
			sw.Start();

			// Act
			var data = sqlCacheProvider.GetAllCachedItems();

			sw.Stop();
			TestContext.WriteLine($"Get all records: {sw.ElapsedMilliseconds.ToString()}");

			// Assert
			Assert.AreEqual(_numberOfItems, data.Count);
		}

		[TestMethod]
		public void TestReplaceItems()
		{
			// Arrange
			var sqlCacheProvider = GetSqlCacheProvider();

			var numberOfitemsToUpdate = _numberOfItems / 3;

			var entitiesToUpdate = Enumerable.Range(1, numberOfitemsToUpdate)
				.Select(x => new DummyEnityWithCachedProperties
				{
					Id = x.ToString(),
					Name = $"Entity {x}",
					TimeStamp = DateTime.Now
				})
				.ToList();

			_dataAccessObject.UpdateDataSource(null, entitiesToUpdate, null);

			var sw = new Stopwatch();
			sw.Start();

			// Act
			sqlCacheProvider.RebuildItems(entitiesToUpdate.Select(x => x.Id).ToList());
			var data = sqlCacheProvider.GetAllCachedItems();

			// Assert
			Assert.AreEqual(_numberOfItems, data.Count);
			foreach(var item in entitiesToUpdate)
			{
				Assert.IsTrue(data.ContainsKey(item.Id));
				var itemFromCache = data[item.Id];
				//Assert.AreEqual(item.Id, itemFromCache.Id);
				Assert.AreEqual(item.Name, itemFromCache.Name);
				Assert.AreEqual(item.TimeStamp, itemFromCache.TimeStamp);
			}
		}

		private PersistentCacheProviderBase<DummyEnityWithCachedProperties> GetSqlCacheProvider()
		{
			return new SqlCacheProvider<DummyEnityWithCachedProperties>(_dataStoreId, _parameters, _dataAccessObject, _cachedProperties);
		}


		[ClassCleanup]
		public static void ClassCleanup()
		{
			using (var cn = new SqlConnection(_connectionString))
			using (var cm = cn.CreateCommand())
			{
				cn.Open();
				cm.CommandText = 
$@"USE master;
IF EXISTS(
	SELECT 
		1 
	FROM 
		master.dbo.sysdatabases
	WHERE 
		[name] = '{_dataBaseName}')
BEGIN
	ALTER DATABASE [{_dataBaseName}]
	SET SINGLE_USER
	WITH ROLLBACK IMMEDIATE;

	DROP DATABASE [{_dataBaseName}]
END";
				cm.ExecuteNonQuery();
			}
		}
	}
}
