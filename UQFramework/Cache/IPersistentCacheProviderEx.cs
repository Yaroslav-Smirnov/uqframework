using System;
using System.Collections.Generic;
using System.Text;

namespace UQFramework.Cache
{
    internal interface IPersistentCacheProviderEx<T>
    {
        (IEnumerable<string> replacedItemsIdentifiers, IDictionary<string, T> newItems, long lastUpdate) GetDelta(long? updateNumber);
    }
}
