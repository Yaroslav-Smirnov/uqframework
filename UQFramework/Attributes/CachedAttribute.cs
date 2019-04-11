using System;

namespace UQFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CachedAttribute : Attribute
    {
        public CachedAttribute()
        {
        }
    }
}
