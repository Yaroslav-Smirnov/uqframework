using System.Collections.Generic;

namespace UQFramework.DAO
{
    public interface IDataSourceBulkReader<T>
    {
        IEnumerable<T> GetEntities(IEnumerable<string> identifiers);
    }
}
