using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UQFramework.DAO
{
	public interface IDataSourceCacheProvider<T>
	{
		// Returns entities with only cached pro
		IQueryable<T> GetEntitiesWithCachedPropertiesOnly();

		IQueryable<T> GetEntitiesWithCachedPropertiesOnly(IEnumerable<string> identifiers);
	}
}
