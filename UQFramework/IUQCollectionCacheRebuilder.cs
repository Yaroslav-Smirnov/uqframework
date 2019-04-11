using System.Collections.Generic;

namespace UQFramework
{
    internal interface IUQCollectionCacheRebuilder
    {
        void NotifyCacheExpired();

        void NotifyCacheItemsExpired(IEnumerable<string> identifiers);
    }
}
