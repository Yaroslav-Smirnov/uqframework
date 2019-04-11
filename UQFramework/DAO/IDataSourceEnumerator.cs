using System.Collections.Generic;

namespace UQFramework.DAO
{
    public interface IDataSourceEnumerator<T>
    {
        int Count();
        IEnumerable<string> GetAllEntitiesIdentifiers();
    }
}
