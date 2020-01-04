using System;
using System.Collections.Generic;
using System.Linq;
using UQFramework.DAO;

namespace UQFramework.XTest.Dummies
{
    internal class DummyDao : IDataSourceBulkReader<Dummy>, 
        IDataSourceBulkWriter<Dummy>,
        INeedDataSourceProperties,
        IDataSourceCacheProvider<Dummy>,
        IDataSourceEnumeratorEx<Dummy>
    {
        private MethodCallsCounter _methodCallsCounter;
        private IDictionary<string, Dummy> _internalDict;
        private const string _numberOfItemsPropertyName = "numberOfItems";
        private const string _methodCallsCounterPropertyName = "methodCallsCounter";

        public void SetProperties(IReadOnlyDictionary<string, object> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            if (!properties.TryGetValue(_numberOfItemsPropertyName, out var numberOfItems))
                throw new InvalidOperationException($"Cannot find required parameter '{_numberOfItemsPropertyName}'");

            if(!(numberOfItems is int))
                throw new InvalidOperationException($"Unexpected value of parameter '{_numberOfItemsPropertyName}'");

            var count = (int)numberOfItems;
            _internalDict = new Dictionary<string, Dummy>(count);

            for(var i = 0; i < count; i++)
            {
                var key = i.ToString();
                _internalDict[key] = new Dummy
                {
                    Id = key,
                    Name = $"Item {i}",
                    Number = i
                };
            }

            if (!properties.TryGetValue(_methodCallsCounterPropertyName, out var methodCallsCounter))
                throw new InvalidOperationException($"Cannot find required parameter '{_methodCallsCounterPropertyName}'");

            _methodCallsCounter = (MethodCallsCounter)methodCallsCounter;
        }

        public IEnumerable<Dummy> GetEntities(IEnumerable<string> identifiers)
        {
            _methodCallsCounter.AddMethodCall();
            return GetEntitiesByIds(identifiers).Select(Clone);
        }

        public void UpdateDataSource(IEnumerable<Dummy> entitiesToAdd, IEnumerable<Dummy> entitiesToUpdate, IEnumerable<Dummy> entitesToDelete)
        {
            _methodCallsCounter.AddMethodCall();
            if (_internalDict == null)
                throw new InvalidOperationException("Uninitialized");

            // delete
            foreach(var item in entitesToDelete)
            {
                var key = item.Id;

                if (!_internalDict.ContainsKey(key))
                    continue;

                _internalDict.Remove(key);
            }

            // update & add
            foreach(var item in entitiesToAdd.Union(entitiesToUpdate))
                _internalDict[item.Id] = Clone(item);
        }

        public IQueryable<Dummy> GetEntitiesWithCachedPropertiesOnly()
        {
            _methodCallsCounter.AddMethodCall();
            return GetEntitiesWithCachedPropertiesOnly(_internalDict.Values);
        }

        public IQueryable<Dummy> GetEntitiesWithCachedPropertiesOnly(IEnumerable<string> identifiers)
        {
            _methodCallsCounter.AddMethodCall();
            return GetEntitiesWithCachedPropertiesOnly(GetEntitiesByIds(identifiers));
        }

        public IEnumerable<Dummy> GetAllEntities()
        {
            _methodCallsCounter.AddMethodCall();
            return _internalDict.Values.Select(Clone);
        }

        public int Count()
        {
            _methodCallsCounter.AddMethodCall();
            return _internalDict.Count;
        }

        public IEnumerable<string> GetAllEntitiesIdentifiers()
        {
            _methodCallsCounter.AddMethodCall();
            return _internalDict.Keys;
        }

        private IEnumerable<Dummy> GetEntitiesByIds(IEnumerable<string> identifiers)
        {
            if (_internalDict == null)
                throw new InvalidOperationException("Uninitialized");

            foreach (var id in identifiers)
            {
                if (!_internalDict.ContainsKey(id))
                    continue;

                yield return _internalDict[id];
            }
        }

        private static IQueryable<Dummy> GetEntitiesWithCachedPropertiesOnly(IEnumerable<Dummy> source)
        {
            return source.Select(v => new Dummy
            {
                Id = v.Id,
                Name = v.Name
            }).AsQueryable();
        }

        private static Dummy Clone(Dummy item)
        {
            return new Dummy
            {
                Id = item.Id,
                Name = item.Name,
                Number = item.Number
            };
        }
    }
}
