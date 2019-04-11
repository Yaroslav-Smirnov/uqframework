using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UQFramework.Cache
{
    internal interface ICachedDataProviderEx
    {
        void UpdateCacheService(IEnumerable<string> identifiers);

        void UpdateCacheServiceAll();
    }
}
