using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UQFramework.Tests.Helpers;

namespace UQFramework.Tests.LinqTests
{
    [TestClass]
    public class LastOrDefaultTest : LinqTestBase
    {
        // TBD - add other tests;
        [TestMethod]
        public void TestLastOrDefaultWithSingleId()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.LastOrDefault(x => x.Key == "75");

            // Assert
            Assert.IsNotNull(result);
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
        }
    }
}
