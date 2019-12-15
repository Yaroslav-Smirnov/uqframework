using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using UQFramework.Attributes;

namespace UQFramework.Cache.Providers
{
	public class SqlCacheProvider<T> : PersistentCacheProviderBase<T> where T : new()
	{
		private readonly string _connectionString;
		private readonly string _tableName;
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

			_cacheKey = $"{typeof(T).Name}_{dataSourceReader.GetType().Name.Replace("`","")}";
			_tableName = $"Table_{_cacheKey}";

			_dataBaseName = $"Database{_dataStoreSetId}";

			_daoVersion = dataSourceReader.GetType().GetCustomAttribute<DaoVersionAttribute>()?.Version ?? new Version(1, 0, 0, 0);

			_jsonContractResolver = new CachedPropertyContractResolver<T>(_cachedProperties);
		}

		public override DateTimeOffset LastChanged
		{
			get
			{
				using (var cn = new SqlConnection(_connectionString))
				using (var cm = cn.CreateCommand())
				{
					cn.Open();
					// check database
					EnsureDbExists(cn, cm);

					cm.CommandText = BuildGetLastTableChangedTime();

					var result = cm.ExecuteScalar();

					if (result == null || result is DBNull)
						return DateTimeOffset.MinValue;

					return Convert.ToDateTime(result);
				}
			}
		}

		protected override IDictionary<string, T> ReadAllDataFromCache()
		{
			using (var cn = new SqlConnection(_connectionString))
			using (var cm = cn.CreateCommand())
			{
				cn.Open();
				cn.ChangeDatabase(_dataBaseName);

				cm.CommandText = BuildSelectSQL();
				var rdr = cm.ExecuteReader();
				//var result = new Dictionary<string, T>();

				// quicly load everything in memory, then deserialize in parrallel;
				var list = new List<(string key, string value)>();
				while (rdr.Read())				
					list.Add((rdr["Id"].ToString().Trim(), rdr["Data"].ToString()));
				
				return list.AsParallel().ToDictionary(kvp => kvp.key, kvp => JsonConvert.DeserializeObject<T>(kvp.value, GetSerializerSettings()));
			}
		}

		protected override bool RebuildRequired()
		{
			using (var cn = new SqlConnection(_connectionString))
			using (var cm = cn.CreateCommand())
			{
				cn.Open();

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
		}

		protected override void ReplaceAll(IDictionary<string, T> newEntities)
		{
			using (var cn = new SqlConnection(_connectionString))
			using (var cm = cn.CreateCommand())
			{
				cn.Open();

				// check database
				EnsureDbExists(cn, cm);

				// re-create table
				cm.CommandText = BuildCreateTableSql();
				cm.ExecuteNonQuery();

				// re-create table type 
				cm.CommandText = BuildCreateTypeSql();
				cm.ExecuteNonQuery();

				using (var tran = cn.BeginTransaction())
				{
					cm.Transaction = tran;

					// insert data into _Info table
					cm.CommandText = BuildUpdateCacheInfoSQL();
					cm.ExecuteNonQuery();

					cm.CommandText = BuildInsertSql();
					var parameter = cm.Parameters.AddWithValue("@Entities", BuildEntitiesTableParameter(newEntities));
					parameter.SqlDbType = SqlDbType.Structured;
					parameter.TypeName = _dataTableType;
					cm.ExecuteNonQuery();

					tran.Commit();
				}
			}
		}

		protected override void ReplaceItems(IEnumerable<string> identifiers, IDictionary<string, T> newEntities)
		{
			using (var cn = new SqlConnection(_connectionString))
			using (var cm = cn.CreateCommand())
			{
				cn.Open();
				cn.ChangeDatabase(_dataBaseName);

				using (var tran = cn.BeginTransaction())
				{
					cm.Transaction = tran;
					// delete stuff
					cm.CommandText = BuildDeleteSql();
					var parameter = cm.Parameters.AddWithValue("@Identifiers", BuildIdentifiersTable(identifiers));
					parameter.SqlDbType = SqlDbType.Structured;
					parameter.TypeName = _identifiersTableType;
					cm.ExecuteNonQuery();

					// insert new
					cm.CommandText = BuildInsertSql();
					parameter = cm.Parameters.AddWithValue("@Entities", BuildEntitiesTableParameter(newEntities));
					parameter.SqlDbType = SqlDbType.Structured;
					parameter.TypeName = _dataTableType;
					cm.ExecuteNonQuery();

					tran.Commit();
				}
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

		private void EnsureDbExists(SqlConnection cn, SqlCommand cm)
		{
			lock(Locker.Lock)
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
		Id char(512) NOT NULL PRIMARY KEY CLUSTERED
	)

	CREATE TABLE [dbo].[{_infoTableName}](	
		[CacheKey] [varchar](512) NOT NULL,
		[TableName] [varchar](128) NOT NULL,
		[Version] [varchar](32) NOT NULL,
		[CachedProperties] [varchar](MAX) NOT NULL,
	CONSTRAINT [PK_{_infoTableName}] PRIMARY KEY CLUSTERED 
	(
		[CacheKey] ASC
	))";
				cm.ExecuteNonQuery();
			}
		}

		#region SQL Building

		private string BuildGetLastTableChangedTime()
		{
			return
$@"SELECT last_user_update
FROM   sys.dm_db_index_usage_stats us
       JOIN sys.tables t
         ON t.object_id = us.object_id
WHERE  database_id = db_id()
       AND t.object_id = object_id('dbo.{_tableName}') ";
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
		[CachedProperties] = '{CachedPropertiesString}'
	WHERE
		[CacheKey] = '{_cacheKey}'
END
ELSE
BEGIN
	INSERT INTO {_infoTableName} ([CacheKey], [TableName], [Version], [CachedProperties]) VALUES
	('{_cacheKey}', '{_tableName}', '{_daoVersion.ToString()}', '{CachedPropertiesString}')
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
	Id char(512) NOT NULL, 
	Data nvarchar(MAX),
CONSTRAINT [PK_{_tableName}] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))";
		}

		private string BuildCreateTypeSql()
		{
			return
$@"IF TYPE_ID(N'{_dataTableType}') IS NULL
BEGIN
	CREATE TYPE [dbo].[{_dataTableType}] As Table
	(
		Id char(512) NOT NULL PRIMARY KEY CLUSTERED, 
		Data nvarchar(MAX)
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
$@"INSERT INTO {_tableName} (Id, Data) 
SELECT * FROM @Entities";
		}

		private DataTable BuildIdentifiersTable(IEnumerable<string> indentifiers)
		{
			var table = new DataTable();

			table.Columns.Add("Id", typeof(string));

			foreach (var id in indentifiers)
				table.Rows.Add(id);

			return table;
		}

		private DataTable BuildEntitiesTableParameter(IEnumerable<KeyValuePair<string, T>> newEntities)
		{
			var table = new DataTable();

			table.Columns.Add("Id", typeof(string));
			table.Columns.Add("Data", typeof(string));

			// inserting rows
			foreach (var item in newEntities)
				table.Rows.Add(item.Key, JsonConvert.SerializeObject(item.Value, Formatting.None, GetSerializerSettings()));

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
