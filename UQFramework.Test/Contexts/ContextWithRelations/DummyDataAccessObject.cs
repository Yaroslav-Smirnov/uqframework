using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using UQFramework.Attributes;
using UQFramework.DAO;
using UQFramework.Test.Helpers;

namespace UQFramework.Test
{
	// generic data access object which works for any entity
	internal class DummyDataAccessObject<T> : IDataSourceBulkReader<T>, IDataSourceBulkWriter<T>, INeedDataSourceProperties,
		IDataSourceEnumeratorEx<T>
	{
		private readonly int _numberOfEntities;
		private readonly Action<T, string> _setter;
		private readonly Func<T, string> _getter;

		private readonly IDictionary<string, T> _entities;

		private MethodCallsRecorder _methodCallsRecorder;

		public DummyDataAccessObject() : this(100)
		{
		}

		public DummyDataAccessObject(int numberOfEntities)
		{
			_numberOfEntities = numberOfEntities;
			(_getter, _setter) = GetIdentiferGetterAndSetter();

			_entities = Enumerable.Range(1, _numberOfEntities).Select(i =>
			{
				var item = Activator.CreateInstance<T>();
				_setter(item, i.ToString());
				return new
				{
					id = i.ToString(),
					item
				};
			}).ToDictionary(x => x.id, x => x.item);
		}

		public void SetProperties(IReadOnlyDictionary<string, object> properties)
		{
			_methodCallsRecorder = properties["methodCallsRecorder"] as MethodCallsRecorder;
		}

		public IEnumerable<T> GetEntities(IEnumerable<string> identifiers)
		{		
			foreach(var id in identifiers)
			{
				if (_entities.ContainsKey(id))
					yield return _entities[id];
			}
		}

		public IEnumerable<T> GetAllEntities()
		{
			return _entities.Values;
		}

		public int Count()
		{
			return _entities.Count;
		}

		public IEnumerable<string> GetAllEntitiesIdentifiers()
		{
			return _entities.Keys;
		}

		public void UpdateDataSource(IEnumerable<T> entitiesToAdd, IEnumerable<T> entitiesToUpdate, IEnumerable<T> entitiesToDelete)
		{
			_methodCallsRecorder?.AddUpdateDataSourceCall(typeof(T), entitiesToAdd, entitiesToUpdate, entitiesToDelete);

			entitiesToDelete = entitiesToDelete ?? Enumerable.Empty<T>();
			entitiesToUpdate = entitiesToUpdate ?? Enumerable.Empty<T>();
			entitiesToAdd = entitiesToAdd ?? Enumerable.Empty<T>();

			var keysToRemove = entitiesToDelete.Union(entitiesToUpdate).Select(x => _getter(x)).Distinct();

			foreach (var key in keysToRemove)
				_entities.Remove(key);

			// adding new
			foreach(var item in entitiesToUpdate.Union(entitiesToAdd))
			{
				var key = _getter(item);
				_entities[key] = item;
			}
		}


		public static IEnumerable<PropertyInfo> GetPropertiesHavingAttribute(Type objectType, Type attributeType)
		{
			return
				objectType
					.GetProperties()
					.Where(p => p.GetCustomAttributes(attributeType, false).Any());
		}

		private static (Func<T, string> getter, Action<T,string> setter) GetIdentiferGetterAndSetter()
		{
			var keyProperty = GetIdentifierProperty(typeof(T));

			if (keyProperty == null)
				throw new InvalidOperationException($"Can't find property with [Key] attribute in type {typeof(T)}");

			return ((Func<T, string>)Delegate.CreateDelegate(typeof(Func<T, string>), keyProperty.GetMethod),
					(Action<T, string>)Delegate.CreateDelegate(typeof(Action<T, string>), keyProperty.SetMethod));
		}

		private static PropertyInfo GetIdentifierProperty(Type type)
		{
			var identifierProperty = GetPropertiesHavingAttribute(type, typeof(IdentifierAttribute)).SingleOrDefault();

			if (identifierProperty != null)
				return identifierProperty;

			// if no identifier attribute then should be exactly one key attribute
			return GetPropertiesHavingAttribute(type, typeof(KeyAttribute)).SingleOrDefault();
		}
	}
}
