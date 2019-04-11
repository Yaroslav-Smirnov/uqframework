using System;
using System.Collections.Generic;

namespace UQFramework.Cache
{
    public interface ICachedDataProvider<TEntity>
    {
        // returns the cache directly
        IEnumerable<TEntity> GetCachedEntities();

        // returns the cache directly
        IEnumerable<TEntity> GetCachedEntities(IEnumerable<string> identifiers);

        // returns cloned entities
        IEnumerable<TEntity> GetEntitiesBasedOnCache();

        // returns cloned entities
        IEnumerable<TEntity> GetEntitiesBasedOnCache(IEnumerable<string> identifiers);

        // returns cloned entities
        IEnumerable<TEntity> GetEntitiesBasedOnCache(Func<TEntity, bool> filter);

        // returns cloned entities
        IEnumerable<string> GetIdentifiersFromCache(IEnumerable<string> identifiers);

        // returns cloned entities
        IEnumerable<string> GetIdentifiersFromCache();

        IEnumerable<string> GetIdentifiersFromCache(Func<TEntity, bool> predicate);

        IEnumerable<string> GetCachedProperties();
    }
}
