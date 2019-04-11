using System;

namespace UQFramework.Cache.Providers
{
    internal class CacheHeader
    {
        public Version DaoVersion { get; set; }

        public Version CacheFormatVersion { get; set; }

        public string DataAccessObjectType { get; set; }

        public string CachedEntityType { get; set; }

        public string CachedPropertiesString { get; set; }
    }
}
