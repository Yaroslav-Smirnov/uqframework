using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq;
using System.Data.SqlClient;
using Newtonsoft.Json;
using UQFramework.Attributes;
using System.Data.Common;

namespace UQFramework.Cache.Providers
{
	public class SqlCacheProvider<T> : PersistentCacheProviderBase<T>, IPersistentCacheProviderEx<T> 
        where T : new()
	{
		private readonly string _connectionString;
		private readonly string _tableName;
        private readonly string _updatesTableName;
		private readonly Version _daoVersion;
		private const string _infoTableName = "_Info";
		private const string _identifiersTableType = "TypeTableIdentifiers";
		private const string _dataTableType = "TypeData";
		private readonly string _dataBaseName;

		private readonly string _cacheKey;
		private readonly CachedPropertyContractResolver<T> _jsonContractResolver;
		public SqlCacheProvider(string dataStoreSetId, IDictionary<string, string> parameters, object dataSourceReader, IEnumerable<PropertyInfo> cachedProperties)
			: base(dataStoreSetId, dataSourceReader, cachedProperties)
		{
			if (string.IsNullOrEmpty(dataStoreSetId))
				throw new ArgumentNullException(nameof(dataStoreSetId));

			if (!parameters.TryGetValue("connectionstring", out _connectionString))
				throw new ArgumentException("Required key 'connectionstring' missing in parameters", nameof(parameters));

			_cacheKey = $"{typeof(T).Name}_{dataSourceReader.GetType().Name.Replace("`", "")}";
			_tableName = $"Table_{_cacheKey}";
            _updatesTableName = $"Updates_{_cacheKey}";

			_dataBaseName = $"Database{_dataStoreSetId}";

			_daoVersion = dataSourceReader.GetType().GetCustomAttribute<DaoVersionAttribute>()?.Version ?? new Version(1, 0, 0, 0);

			_jsonContractResolver = new CachedPropertyContractResolver<T>(_cachedProperties);
		}

        // returns delta based on the passed updateNumber
        
        public (IEnumerable<string> replacedItemsIdentifiers, IDictionary<string, T> newItems, long lastUpdate) GetDelta(long? updateNumber)
        {
            var lastUpdate = (long)-1;
            var items = (IDictionary<string, T>)null;
            var identifiers = (IEnumerable<string>)null;

            if (updateNumber == null)
                return (null, GetAllCachedItems(), -1);

            ExecuteSqlDelegate(cn =>
            {
                using(var cm = cn.CreateCommand())
                {
                    cm.Transaction = CacheGlobals.Transaction as SqlTransaction;

                    // check database
                    EnsureDbExists(cn, cm);

                    // get last number
                    cm.CommandText = BuildGetLastTableChangedTime();
                    var result = cm.ExecuteScalar();

                    if (result == null || result is DBNull)
                        lastUpdate = 0;

                    lastUpdate = Convert.ToInt64(result);

                    // get items
                    cm.CommandText = BuildGetDeltaSql(updateNumber.Value);

                    var list = new List<(string key, string value)>();
                    using (var rdr = cm.ExecuteReader())
                    {
                        while (rdr.Read())
                            list.Add((rdr["Id"].ToString().Trim(), rdr["Data"].ToString()));
                    }

                    items = list.AsParallel().ToDictionary(kvp => kvp.key, kvp => JsonConvert.DeserializeObject<T>(kvp.value, GetSerializerSettings()));

                    // get change items
                    cm.CommandText = BuildGetReplacedItems(updateNumber.Value, lastUpdate);

                    var listId = new List<string>();
                    using(var rdr = cm.ExecuteReader())
                    {
                        while(rdr.Read())
                        {
                            listId.Add(rdr.GetString(0).Trim());
                        }
                    }
                    identifiers = listId.AsEnumerable();
                }
            });

            return (identifiers, items, lastUpdate);
        }

        public override long LastChanged
		{
			get
			{
				return ExecuteSqlDelegate(cn =>
				{
					using (var cm = cn.CreateCommand())
					{
						cm.Transaction = CacheGlobals.Transaction as SqlTransaction;
						// check database
						EnsureDbExists(cn, cm);

						cm.CommandText = BuildGetLastTableChangedTime();

						var result = cm.ExecuteScalar();

						if (result == null || result is DBNull)
							return 0;

						return Convert.ToInt64(result);
					}
				});
			}
		}

		protected override IDictionary<string, T> ReadAllDataFromCache()
		{
			return ExecuteSqlDelegate(cn =>
			{
				using (var cm = cn.CreateCommand())
				{
					cm.Transaction = CacheGlobals.Transaction as SqlTransaction;
					cn.ChangeDatabase(_dataBaseName);

					cm.CommandText = BuildSelectSQL();
                    var list = new List<(string key, string value)>();
                    using (var rdr = cm.ExecuteReader())
                    {
                        while (rdr.Read())
                            list.Add((rdr["Id"].ToString().Trim(), rdr["Data"].ToString()));
                    }
					return list.AsParallel().ToDictionary(kvp => kvp.key, kvp => JsonConvert.DeserializeObject<T>(kvp.value, GetSerializerSettings()));
				}
			});
		}

		protected override bool RebuildRequired()
		{
			return ExecuteSqlDelegate(cn =>
			{
				using (var cm = cn.CreateCommand())
				{
					cm.Transaction = CacheGlobals.Transaction as SqlTransaction;
					// check database
					EnsureDbExists(cn, cm);

					cm.CommandText = BuildGetCacheInfoSQL();
					using (var rdr = cm.ExecuteReader())
					{
						if (!rdr.HasRows)
							return true;

						rdr.Read();

						var version = Version.Parse((string)rdr["Version"]);
						var cachedProperties = (string)rdr["CachedProperties"];

						return version != _daoVersion || cachedProperties != CachedPropertiesString;
					}
				}
			});
		}

		protected override void ReplaceAll(IDictionary<string, T> newEntities)
		{
			ExecuteSqlDelegate(cn =>
			{
				using (var cm = cn.CreateCommand())
				{
					cm.Transaction = CacheGlobals.Transaction as SqlTransaction;
					// check database
					EnsureDbExists(cn, cm);

					// re-create table
					cm.CommandText = BuildCreateTableSql();
					cm.ExecuteNonQuery();

                    // re-create updates table

                    cm.CommandText = BuildUpdatesTableSql();
                    cm.ExecuteNonQuery();

                    // re-create table type 
                    cm.CommandText = BuildCreateTypeSql();
					cm.ExecuteNonQuery();

					void UpdateCache()
					{
						// insert data into _Info table
						cm.CommandText = BuildUpdateCacheInfoSQL();
						var updateNumber = (int)cm.ExecuteScalar();

						cm.CommandText = BuildInsertSql();
						var parameter = cm.Parameters.AddWithValue("@Entities", BuildEntitiesTableParameter(newEntities, updateNumber));
						parameter.SqlDbType = SqlDbType.Structured;
						parameter.TypeName = _dataTableType;
						cm.ExecuteNonQuery();
					}

					if (CacheGlobals.Transaction is SqlTransaction currentTransaction)
					{
						cm.Transaction = currentTransaction;
						UpdateCache();
					}
					else
					{
						using (var tran = cn.BeginTransaction())
						{
							cm.Transaction = tran;
							UpdateCache();
							tran.Commit();
						}
					}
				}
			});
		}

		protected override void ReplaceItems(IEnumerable<string> identifiers, IDictionary<string, T> newEntities)
		{
			void UpdateDb(SqlCommand cm)
			{
				// delete stuff
				cm.CommandText = BuildDeleteSql();
				var parameter = cm.Parameters.AddWithValue("@Identifiers", BuildIdentifiersTable(identifiers));
				parameter.SqlDbType = SqlDbType.Structured;
				parameter.TypeName = _identifiersTableType;
				cm.ExecuteNonQuery();

				// get latest transaction
				cm.CommandText = BuildIncreaseUpdateNumberSQL();
				var updateNumber = (int)cm.ExecuteScalar();

				// insert new
				cm.CommandText = BuildInsertSql();
				parameter = cm.Parameters.AddWithValue("@Entities", BuildEntitiesTableParameter(newEntities, updateNumber));
				parameter.SqlDbType = SqlDbType.Structured;
				parameter.TypeName = _dataTableType;
				cm.ExecuteNonQuery();
			}

			ExecuteSqlDelegate(cn =>
			{
				cn.ChangeDatabase(_dataBaseName);

				using (var cm = cn.CreateCommand())
				{
					if (CacheGlobals.Transaction is SqlTransaction currentTransaction)
					{
						cm.Transaction = currentTransaction;
						UpdateDb(cm);
					}
					else
					{
						using (var tran = cn.BeginTransaction())
						{
							cm.Transaction = tran;
							UpdateDb(cm);
							tran.Commit();
						}
					}
				}
			});
		}

		private void ExecuteSqlDelegate(Action<SqlConnection> sqlAction)
		{
			if (CacheGlobals.Transaction?.Connection is SqlConnection sqlConnetion)
			{
				if (sqlConnetion.State == ConnectionState.Closed)
					sqlConnetion.Open();

				sqlAction(sqlConnetion);
				return;
			}

			using (var cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				sqlAction(cn);
			}
		}

		private U ExecuteSqlDelegate<U>(Func<SqlConnection, U> sqlFunc)
		{
			if (CacheGlobals.Transaction?.Connection is SqlConnection sqlConnetion)
			{
				if (sqlConnetion.State == ConnectionState.Closed)
					sqlConnetion.Open();

				return sqlFunc(sqlConnetion);
			}

			using (var cn = new SqlConnection(_connectionString))
			{
				cn.Open();
				return sqlFunc(cn);
			}
				
		}


		private JsonSerializerSettings GetSerializerSettings()
		{
			return new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore,
				ContractResolver = _jsonContractResolver,
			};
		}

		private void EnsureDbExists(DbConnection cn, DbCommand cm)
		{
			lock (Locker.Lock)
			{
				cm.CommandText =
$@"SELECT 
	1 
FROM 
	master.dbo.sysdatabases
WHERE 
	[name] = '{_dataBaseName}'";

				var result = cm.ExecuteScalar();

				if (result != null)
				{
					cn.ChangeDatabase(_dataBaseName);
					return;
				}

				cm.CommandText = $"CREATE DATABASE [{ _dataBaseName}]";
				cm.ExecuteNonQuery();

				cn.ChangeDatabase(_dataBaseName);

				cm.CommandText =
$@"CREATE TYPE [dbo].[{_identifiersTableType}] As Table
	(
		Id varchar(512) NOT NULL PRIMARY KEY CLUSTERED
	)

	CREATE TABLE [dbo].[{_infoTableName}](	
		[CacheKey] [varchar](512) NOT NULL,
		[TableName] [varchar](128) NOT NULL,
		[Version] [varchar](32) NOT NULL,
		[CachedProperties] [varchar](MAX) NOT NULL,
		[LastUpdateNumber] int NOT NULL
	CONSTRAINT [PK_{_infoTableName}] PRIMARY KEY CLUSTERED 
	(
		[CacheKey] ASC
	))";
				cm.ExecuteNonQuery();
			}
		}

		#region SQL Building
        // Using string building instead of parameters for all parameters are internal 
        // and there is no threat of SQL injection

        private string BuildGetReplacedItems(long startUpdateNumber, long endUpdateNumber)
        {
            return
$@"SELECT
    ItemId
FROM
    {_updatesTableName}
WHERE
    UpdateNumber BETWEEN {startUpdateNumber + 1} AND {endUpdateNumber}";
        }

        private string BuildGetDeltaSql(long updateNumber)
        {
            return
$@"{BuildSelectSQL()}
WHERE
    UpdateNumber > {updateNumber}";
        }
		private string BuildGetLastTableChangedTime()
		{
			return
$@"SELECT
	i.LastUpdateNumber
FROM
	dbo._Info i
WHERE
	i.CacheKey = '{_cacheKey}'";
/*$@"SELECT last_user_update
FROM   sys.dm_db_index_usage_stats us
       JOIN sys.tables t
         ON t.object_id = us.object_id
WHERE  database_id = db_id()
       AND t.object_id = object_id('dbo.{_tableName}') ";
	   */
		}

		private string BuildIncreaseUpdateNumberSQL()
		{
			return
$@"UPDATE 
	{_infoTableName}
SET
	[TableName] = '{_tableName}',
	[Version] = '{_daoVersion.ToString()}',
	[CachedProperties] = '{CachedPropertiesString}',
	[LastUpdateNumber] = [LastUpdateNumber] + 1
OUTPUT
	inserted.LastUpdateNumber
WHERE
	[CacheKey] = '{_cacheKey}'";
		}

		private string BuildUpdateCacheInfoSQL()
		{
			return
$@"IF EXISTS (SELECT 1 FROM {_infoTableName} WHERE [TableName] = '{_tableName}')
BEGIN
	UPDATE 
		{_infoTableName}
	SET
		[TableName] = '{_tableName}',
		[Version] = '{_daoVersion.ToString()}',
		[CachedProperties] = '{CachedPropertiesString}',
		[LastUpdateNumber] = [LastUpdateNumber] + 1
	OUTPUT
		inserted.LastUpdateNumber
	WHERE
		[CacheKey] = '{_cacheKey}'

END
ELSE
BEGIN
	INSERT INTO {_infoTableName} ([CacheKey], [TableName], [Version], [CachedProperties], [LastUpdateNumber]) 
	OUTPUT
		inserted.LastUpdateNumber
	VALUES
	('{_cacheKey}', '{_tableName}', '{_daoVersion.ToString()}', '{CachedPropertiesString}', 1)
END";
		}

		private string BuildGetCacheInfoSQL()
		{
			return
$@"SELECT
	[TableName],
	[Version],
	[CachedProperties]	
FROM 
	[dbo].[{_infoTableName}]
WHERE 
	[CacheKey] = '{_cacheKey}'";
		}

		private string BuildCreateTableSql()
		{
			return
$@"IF EXISTS 
	(SELECT 
		1 
	FROM 
		INFORMATION_SCHEMA.TABLES
	WHERE 
		TABLE_NAME = '{_tableName}')
BEGIN
	DROP TABLE [dbo].[{_tableName}]
END

CREATE TABLE [dbo].[{_tableName}](	
	Id varchar(512) NOT NULL, 
	Data nvarchar(MAX),
	[UpdateNumber] int NOT NULL
CONSTRAINT [PK_{_tableName}] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))

CREATE NONCLUSTERED INDEX [IX_{_tableName}_UpdateNumber] ON [dbo].[{_tableName}]
(
	[UpdateNumber] ASC
)";
		}

        private string BuildUpdatesTableSql()
        {
            return
$@"IF EXISTS 
	(SELECT 
		1 
	FROM 
		INFORMATION_SCHEMA.TABLES
	WHERE 
		TABLE_NAME = '{_updatesTableName}')
BEGIN
	DROP TABLE [dbo].[{_updatesTableName}]
END

CREATE TABLE [dbo].[{_updatesTableName}](
	[UpdateNumber] [int] NOT NULL,
	[ItemId] [char](512) NOT NULL,
 CONSTRAINT [PK_{_updatesTableName}] PRIMARY KEY CLUSTERED 
(
	[UpdateNumber] ASC,
	[ItemId] ASC
))

CREATE NONCLUSTERED INDEX [IX_{_updatesTableName}_UpdateNumber] ON [dbo].[{_updatesTableName}]
(
	[UpdateNumber] ASC
)";
        }

		private string BuildCreateTypeSql()
		{
			return
$@"IF TYPE_ID(N'{_dataTableType}') IS NULL
BEGIN
	CREATE TYPE [dbo].[{_dataTableType}] As Table
	(
		Id varchar(512) NOT NULL PRIMARY KEY CLUSTERED, 
		Data nvarchar(MAX),
		UpdateNumber int NOT NULL
	)
END";
		}

		private string BuildSelectSQL()
		{
			return $@"SELECT 
						Id, Data
					FROM 
						[{_tableName}]";
		}

		private string BuildInsertSql()
		{
			// TODO: Key property
			return
$@"INSERT INTO {_tableName} (Id, Data, UpdateNumber) 
SELECT * FROM @Entities;

INSERT INTO {_updatesTableName} ([UpdateNumber], [ItemId])
SELECT UpdateNumber, Id FROM @Entities";
		}

		private DataTable BuildIdentifiersTable(IEnumerable<string> indentifiers)
		{
			var table = new DataTable();

			table.Columns.Add("Id", typeof(string));

			foreach (var id in indentifiers)
				table.Rows.Add(id);

			return table;
		}

		private DataTable BuildEntitiesTableParameter(IEnumerable<KeyValuePair<string, T>> newEntities, int updateNumber)
		{
			var table = new DataTable();

			table.Columns.Add("Id", typeof(string));
			table.Columns.Add("Data", typeof(string));
			table.Columns.Add("UpdateNumber", typeof(int));

			// inserting rows
			foreach (var item in newEntities)
				table.Rows.Add(item.Key, JsonConvert.SerializeObject(item.Value, Formatting.None, GetSerializerSettings()), updateNumber);

			return table;
		}

		private string BuildDeleteSql()
		{
			return
				$@"DELETE FROM {_tableName} WHERE Id IN 
				(SELECT Id FROM @Identifiers)";
		}

        #endregion
    }
}
