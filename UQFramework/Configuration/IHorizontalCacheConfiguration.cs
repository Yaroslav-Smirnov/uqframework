using System;
using System.Collections.Generic;

namespace UQFramework.Configuration
{
    public interface IHorizontalCacheConfiguration
    {
        bool IsEnabled { get; }
        Type ProviderType { get; }
        string GetParameter(string key);
        IDictionary<string, string> GetAllParameters();
    }
}