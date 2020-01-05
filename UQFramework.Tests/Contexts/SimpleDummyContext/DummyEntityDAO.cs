using System;
using System.Collections.Generic;
using System.Linq;
using UQFramework.DAO;

namespace UQFramework.Tests
{
    internal class DummyEntityDAO : IDataSourceReader<DummyEntity>, IDataSourceEnumerator<DummyEntity>
    {
        public int Count()
        {
            return 2;
        }

        public IEnumerable<string> GetAllEntitiesIdentifiers()
        {
            yield return "1";
            yield return "2";
        }

        public DummyEntity GetEntity(string identifier)
        {
            return new DummyEntity
            {
                Key = identifier,
                Name = $"Entity {identifier}"
            };
        }
    }

}
