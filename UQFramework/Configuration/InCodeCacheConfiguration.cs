using System;
using System.Collections.Generic;

namespace UQFramework.Configuration
{
	internal class InCodeCacheConfiguration : IHorizontalCacheConfigurationInternal
	{
		private readonly IHorizontalCacheConfiguration _cacheConfiguration;
		internal InCodeCacheConfiguration(IHorizontalCacheConfiguration cacheConfiguration)
		{
			_cacheConfiguration = cacheConfiguration;
		}

		public Type CacheDataProviderType => null;

		public bool IsEnabled => _cacheConfiguration.IsEnabled;

		public Type ProviderType => _cacheConfiguration.ProviderType;

		public IDictionary<string, string> GetAllParameters()
		{
			return _cacheConfiguration.GetAllParameters();
		}

		public string GetParameter(string key)
		{
			return _cacheConfiguration.GetParameter(key);
		}
	}
}
