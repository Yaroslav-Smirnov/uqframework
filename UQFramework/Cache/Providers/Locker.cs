using System;
using System.Collections.Generic;
using System.Text;

namespace UQFramework.Cache.Providers
{
	// locker to use with lock keywords
	internal static class Locker
	{
		public readonly static object Lock = new object();
	}
}
