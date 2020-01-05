using System;
using UQFramework.Attributes;

namespace UQFramework.Tests.Tests.CacheProviders
{
    class DummyEnityWithCachedProperties
	{
		[Identifier]
		public string Id { get; set; }

		[Cached]
		public string Name { get; set; }

		[Cached]
		public DateTime TimeStamp { get; set; }
	}
}
