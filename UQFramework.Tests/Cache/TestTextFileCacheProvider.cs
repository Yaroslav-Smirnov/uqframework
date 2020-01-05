using System.Collections.Generic;
using System.Reflection;
using UQFramework.Cache.Providers;

namespace UQFramework.Tests
{
    internal class TestTextFileCacheProvider<T> : TextFileCacheProvider<T>
    {
        public TestTextFileCacheProvider(string dataStoreId, IDictionary<string, string> parameters, object dataSourceReader, IEnumerable<PropertyInfo> cachedProperties) 
            : base(dataStoreId, parameters, dataSourceReader, cachedProperties)
        {
        }
    }
}
