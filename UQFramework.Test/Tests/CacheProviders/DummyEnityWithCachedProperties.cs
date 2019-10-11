using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UQFramework.Attributes;

namespace UQFramework.Test.Tests.CacheProviders
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
