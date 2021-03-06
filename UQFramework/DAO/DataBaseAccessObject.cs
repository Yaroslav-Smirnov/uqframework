﻿using System;
using System.Collections.Generic;
using System.Data.Common;

namespace UQFramework.DAO
{
	public abstract class DataBaseAccessObject<T> : IDataSourceBulkReader<T>, IDataSourceBulkWriter<T>, INeedDataSourceProperties, IDataSourceEnumeratorEx<T>
	{
		public abstract IEnumerable<T> GetEntities(IEnumerable<string> identifiers);
		public abstract void UpdateDataSource(IEnumerable<T> entitiesToAdd, IEnumerable<T> entitiesToUpdate, IEnumerable<T> entitesToDelete);
		protected DbConnection Connection { get; private set; }

		public void SetProperties(IReadOnlyDictionary<string, object> properties)
		{
			if (properties == null || !(properties["dbconnection"] is DbConnection connection))
				throw new InvalidOperationException("Must provide valid SQL connection into 'dbconnection' parameter");

			Connection = connection;
		}

		public abstract IEnumerable<T> GetAllEntities();

		public int Count() => throw new NotImplementedException();

		public abstract IEnumerable<string> GetAllEntitiesIdentifiers();
	}
}
