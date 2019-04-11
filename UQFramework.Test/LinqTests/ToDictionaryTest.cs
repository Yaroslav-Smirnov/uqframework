using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UQFramework.Test.Helpers;

namespace UQFramework.Test.LinqTests
{
    [TestClass]
    public class ToDictionaryTest : LinqTestBase
    {
        [TestMethod]
        public void TestToDictionary()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.ToDictionary(x => x.Key, x => x.Name).FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(1000, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestToDictionaryWithTake()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.Take(4).ToDictionary(x => x.Key, x => x.Name);

            // Assert
            Assert.IsNotNull(result);
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(4, methodCounter.EntityCallsCount);

        }
    }
}
