using System.Configuration;

namespace UQFramework.Configuration
{
    public class UQConfiguration : ConfigurationSection, IUQConfiguration
    {
        private const string ConfigurationSectionName = "uqframework";
        private const string HorizontalCacheConfigurationPropertyName = "h-cache";

        private static IUQConfiguration _instance;
        private UQConfiguration()
        {
        }

        public static IUQConfiguration Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                if (ConfigurationManager.GetSection(ConfigurationSectionName) is UQConfiguration config)
                    return _instance = config;

                return _instance = new UQConfiguration
                {
                    HorizontalCacheConfigurationSection = new HorizontalCacheConfiguration()
                };
            }
        }

        [ConfigurationProperty(HorizontalCacheConfigurationPropertyName)]
        internal HorizontalCacheConfiguration HorizontalCacheConfigurationSection
        {
            get => (HorizontalCacheConfiguration)base[HorizontalCacheConfigurationPropertyName];
            set => base[HorizontalCacheConfigurationPropertyName] = value;
        }

        public IHorizontalCacheConfiguration HorizontalCacheConfiguration => HorizontalCacheConfigurationSection;

    }
}
