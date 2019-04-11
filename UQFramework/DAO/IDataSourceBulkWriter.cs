using System.Collections.Generic;

namespace UQFramework.DAO
{
    public interface IDataSourceBulkWriter<T>
    {
        void UpdateDataSource(IEnumerable<T> entitiesToAdd, IEnumerable<T> entitiesToUpdate, IEnumerable<T> entitesToDelete);
    }
}
