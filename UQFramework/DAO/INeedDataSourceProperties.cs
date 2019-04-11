using System.Collections.Generic;

namespace UQFramework.DAO
{
    public interface INeedDataSourceProperties
    {
        void SetProperties(IReadOnlyDictionary<string, object> properties);
    }
}
