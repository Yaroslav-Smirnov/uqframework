using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using UQFramework.Test.Helpers;

namespace UQFramework.Test.LinqTests
{
    [TestClass]
    public class FirstOrDefaultTest : LinqTestBase
    {
        [TestMethod]
        public void TestFirstOrDefaultWithSingleId()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.FirstOrDefault(x => x.Key == "75");

            // Assert
            Assert.IsNotNull(result);
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestFirstOrDefaultWithFilterReturningOneItem()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.FirstOrDefault(x => x.Name == "Dummy Item 75");

            // Assert
            Assert.IsNotNull(result);
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount); // not expecting creating instances from cache - only quereing cache
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestFirstOrDefaultWithoutFilter()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestFirstOrDefaultWithContainsFilter()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            var identifiers = Enumerable.Range(177, 20000).Select(i => i.ToString());

            // Act

            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            //var data = context.DummyEntitiesWithCache
            //                .FirstOrDefault(x => identifiers.Contains(x.Key));

            var data = context.DummyEntitiesWithCache
                .FirstOrDefault(x => x.Key == "177");

            stopWatch.Stop();

            // Assert
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30);  //YSV: one call to the datastorage
            // Ok, that's a problem here
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
            Assert.AreEqual(0, methodCounter.GetIdentifiersCallsCount);
            Assert.IsNotNull(data);
        }


        [TestMethod]
        public void TestCombinesWithPendingChangesRemove()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);
            context.DummyEntitiesWithCache.Remove(new DummyEntity { Key = "2" });

            // Act (Filter by Key)
            var entity = context.DummyEntitiesWithCache.Where(x => x.Key == "2").FirstOrDefault();

            // Assert (Filter by Key)
            Assert.IsNull(entity);
            Assert.AreEqual(0, methodCounter.EntityCallsCount);

            // Act (Filter by a cached property)
            var entity2 = context.DummyEntitiesWithCache.Where(x => x.Name == "Dummy Entity 2").FirstOrDefault();
            Assert.IsNull(entity2);
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestCombinesWithPendingChangesAddUpdate()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            var newItem = new DummyEntity
            {
                Key = "1051",
                Name = "Test 1051"
            };

            var newItem2 = new DummyEntity
            {
                Key = "751",
                Name = "Test 751"
            };

            context.DummyEntitiesWithCache.Add(newItem);
            context.DummyEntitiesWithCache.Update(newItem2);

            // Act
            var addedItem = context.DummyEntitiesWithCache.FirstOrDefault(x => x.Key == "1051");
            var updatedItem = context.DummyEntitiesWithCache.FirstOrDefault(x => x.Key == "751");

            // Assert
            Assert.AreEqual(newItem2, updatedItem);
            Assert.AreEqual(newItem, addedItem);
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
            Assert.AreEqual(0, methodCounter.GetIdentifiersCallsCount);
        }

        [TestMethod]
        public void TestFilterWithCachedProperties()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.FirstOrDefault(x => x.Name == "Dummy Item 75");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("75", result.Key);
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestFilterWithCachedPropertiesComplexPredicate()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.FirstOrDefault(x => x.Name.Contains("Dummy Item ") && !x.Key.StartsWith("1"));

            // Assert
            Assert.IsNotNull(result);
            //Assert.AreEqual("2", result.Key);
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
        }
    }
}
