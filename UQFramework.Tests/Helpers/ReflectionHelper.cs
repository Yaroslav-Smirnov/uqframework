using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace UQFramework.Tests.Helpers
{
    internal static class ReflectionHelper
    {
        internal static TestCacheDataProvider<T> WinkleCacheDataProviderOut<T>(IUQCollection<T> collection) where T : new()
        {
            // using reflection to get reference to the cacheDataProvider to assert method calls etc.
            var field = typeof(UQCollection<T>).GetField("_cachedDataProvider", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field);
            return (TestCacheDataProvider<T>)field.GetValue(collection);
        }
    }
}
