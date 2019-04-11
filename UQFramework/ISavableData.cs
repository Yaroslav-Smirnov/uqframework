using System;
using System.Collections.Generic;

namespace UQFramework
{
    internal interface ISavableData<T> : ISavableDataEx
    {
        /// <summary>
        /// Combines an external range with pending changes
        /// </summary>
        /// <param name="data">The range to combine the pending changes with</param>
        /// <returns>The combined range</returns>
        IEnumerable<T> CombineWithPendingChanges(IEnumerable<T> data, Func<T, bool> filter);


        /// <summary>
        /// Combines and external range with pending changes
        /// </summary>
        /// <param name="data">The range to combine the pending changes with</param>
        /// <returns>The combined range</returns>
        IEnumerable<string> CombineIdentifiersRangeWithPendingChanges(IEnumerable<string> identifiers, Func<T, bool> filter);

        /// <summary>
        /// Returns all the pending changes
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> GetAllPendingChanges();

        IEnumerable<string> GetAllPendingChangesIdentifiers();

        /// <summary>
        /// Returns items pending deletion
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> PendingDelete { get; }

        IEnumerable<T> PendingUpdate { get; }

        IEnumerable<T> PendingAdd { get; }
    }
}
