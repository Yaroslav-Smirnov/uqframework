using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using UQFramework.Tests.Helpers;

namespace UQFramework.Tests.LinqTests
{
    [TestClass]
    public class AdditionalLinqTest : LinqTestBase
    {
        [TestMethod]
        public void TestToList()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act 
            context.DummyEntitiesWithCache.Add(new DummyEntity { });
            var result = context.DummyEntitiesWithCache.ToList();

            // Assert
            Assert.AreEqual(1001, result.Count);
        }

        [TestMethod]
        public void TestSelectKey()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.Select(x => x.Key).FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(!string.IsNullOrEmpty(result));

            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestSelectKeyAnonimousType()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.Select(x => new
            {
                x.Key
            }).FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(!string.IsNullOrEmpty(result.Key));

            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
        }

    }
}
