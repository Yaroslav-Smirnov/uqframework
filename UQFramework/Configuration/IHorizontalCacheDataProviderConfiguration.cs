using System;

namespace UQFramework.Configuration
{
    internal interface IHorizontalCacheConfigurationInternal : IHorizontalCacheConfiguration
    {
        Type CacheDataProviderType { get; }
    }
}
