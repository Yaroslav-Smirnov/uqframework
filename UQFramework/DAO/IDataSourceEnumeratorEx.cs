using System.Collections.Generic;

namespace UQFramework.DAO
{
    public interface IDataSourceEnumeratorEx<T> : IDataSourceEnumerator<T>
    {
        IEnumerable<T> GetAllEntities();
    }
}
