using System.Collections.Generic;

namespace UQFramework.Cache
{
    /// <summary>
    /// Interface for any persistent cache
    /// </summary>
    /// <typeparam name="T">Type of the cached items</typeparam>
    internal interface IPersistentCacheProvider<T>
    {
        string UniqueCacheKey { get; }

        /// <summary>
        /// Returns all the items currently in cache
        /// </summary>
        /// <returns></returns>
        IDictionary<string, T> GetAllCachedItems();

        /// <summary>
        /// Rebuilds itmes from a set of identifiers
        /// </summary>
        /// <param name="identifiers">The enumeration of identifiers</param>
        /// <returns>IDictionary of updated items</returns>
        IDictionary<string, T> RebuildItems(IEnumerable<string> identifiers);

        /// <summary>
        /// Forcibly rebuilds the whole persistent cache;
        /// </summary>
        void FullRebuild();
    }
}
