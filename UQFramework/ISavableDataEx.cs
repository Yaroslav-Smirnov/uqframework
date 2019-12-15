using System;
using System.Collections.Generic;

namespace UQFramework
{
    interface ISavableDataEx
    {
        //void SaveChanges();

		void Delete();

		void CreateAndUpdate();

        void UpdateCacheWithPendingChanges();

		IEnumerable<(Type type, string id, object entity)> PendingAdd { get; }

		IEnumerable<(Type type, string id, object entity)> PendingUpdate { get; }

		IEnumerable<(Type type, string id, object entity)> PendingDelete { get; }
	}
}
