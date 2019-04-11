using System;

namespace UQFramework.Attributes
{
    public class DataAccessObjectAttribute : Attribute
    {
        public DataAccessObjectAttribute(Type dataAccessObjectType, bool disableCache = false)
        {
            DataAccessObjectType = dataAccessObjectType;
            DisableCache = disableCache;
        }

        internal Type DataAccessObjectType { get; }

        internal bool DisableCache { get; }
    }
}
