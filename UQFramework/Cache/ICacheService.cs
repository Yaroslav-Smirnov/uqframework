using System;
using System.Collections.Generic;

namespace UQFramework.Cache
{
    internal interface ICacheService<T>
    {
        IEnumerable<T> GetAllCache();

        IEnumerable<T> GetCache(Func<T, bool> filter);

        IEnumerable<T> GetCachedEntitiesById(IEnumerable<string> identifiers);

        IEnumerable<string> GetExistingIdentifiersFromRange(IEnumerable<string> identifiers);

        IEnumerable<string> GetExistingIdentifiers(Func<T, bool> predicate);

        IEnumerable<string> GetIdentifiersOfAllCachedItems();
    }
}