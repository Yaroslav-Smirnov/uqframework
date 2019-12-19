using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace UQFramework.Cache.Providers
{
	internal static class CacheGlobals
	{
		[ThreadStatic]
		public static DbTransaction Transaction;
	}
}
