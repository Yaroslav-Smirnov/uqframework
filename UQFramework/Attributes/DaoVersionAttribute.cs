using System;

namespace UQFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DaoVersionAttribute : Attribute
    {
        public DaoVersionAttribute(string version)
        {
            Version = Version.Parse(version);
        }

        internal Version Version { get; }
    }
}
