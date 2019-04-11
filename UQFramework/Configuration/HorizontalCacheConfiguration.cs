using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;

namespace UQFramework.Configuration
{
    internal class HorizontalCacheConfiguration : ConfigurationElement, IHorizontalCacheConfigurationInternal
    {
        private const string IsEnabledName = "enabled";
        private const string ProviderTypeName = "type";
        private const string ParametersName = "parameters";

        public override bool IsReadOnly()
        {
            return false;
        }

        [ConfigurationProperty(IsEnabledName, DefaultValue = false)]
        public bool IsEnabled
        {
            get => (bool)base[IsEnabledName];
            set => base[IsEnabledName] = value;
        }

        [ConfigurationProperty(ProviderTypeName)]
        [TypeConverter(typeof(TypeNameConverter))]
        public Type ProviderType
        {
            get => (Type)base[ProviderTypeName];
            set => base[ProviderTypeName] = value;
        }

        [ConfigurationProperty("", IsDefaultCollection = true)]
        public NameValueConfigurationCollection Parameters
        {
            get => (NameValueConfigurationCollection)base[""];
            set => base[""] = value;
        }

        public string GetParameter(string key)
        {
            if (_dictionary.TryGetValue(key, out var value))
                return value;

            var parameters = Parameters;
            return parameters[key].Value;
        }

        //for test only
        private IDictionary<string, string> _dictionary = new Dictionary<string, string>();
        internal void SetParameter(string key, string value)
        {
            _dictionary[key] = value;
        }

        public IDictionary<string, string> GetAllParameters()
        {
            return Parameters.AllKeys.ToDictionary(k => k, k => GetParameter(k));
        }

        Type _cacheDataProviderType;
        Type IHorizontalCacheConfigurationInternal.CacheDataProviderType => _cacheDataProviderType;

        internal void SetCacheDataProviderType(Type cacheDataProvierType)
        {
            _cacheDataProviderType = cacheDataProvierType;
        }
    }
}
