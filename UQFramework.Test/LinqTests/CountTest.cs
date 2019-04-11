using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using UQFramework.Test.Helpers;

namespace UQFramework.Test.LinqTests
{
    [TestClass]
    public class CountTest : LinqTestBase
    {
        [TestMethod]
        public void TestCountWithContainsFilter()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            var identifiers = Enumerable.Range(177, 20000).Select(i => i.ToString());

            // Act

            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            var data = context.DummyEntitiesWithCache
                            .Count(x => identifiers.Contains(x.Key));

            stopWatch.Stop();

            // Assert
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < 40);  //YSV: successive call might take only 20 ms. Not sure what happens when it call it first
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
            Assert.AreEqual(0, methodCounter.GetIdentifiersCallsCount);
            Assert.AreEqual(823, data);
        }

        [TestMethod]
        public void TestCountWithoutFilter()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            var data = context.DummyEntitiesWithCache.Count();

            stopWatch.Stop();

            // Assert
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < 20);  //effectively gets data from cache
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
            Assert.AreEqual(0, methodCounter.GetIdentifiersCallsCount);
            Assert.AreEqual(1000, data);
        }

        [TestMethod]
        public void TestCombinesWithPendingChanges()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act - Removing
            var entity = context.DummyEntitiesWithCache.Where(x => x.Key == "2").FirstOrDefault();
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
            context.DummyEntitiesWithCache.Remove(entity);

            // Assert
            Assert.AreEqual(999, context.DummyEntitiesWithCache.Count());
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
            Assert.AreEqual(0, methodCounter.GetIdentifiersCallsCount);

            // Act - Adding & Update
            var newItem = new DummyEntity
            {
                Key = "1051",
                Name = "Test 1051"
            };

            var newItem2 = new DummyEntity
            {
                Key = "1071",
                Name = "Test 1071"
            };

            var newItem3 = new DummyEntity
            {
                Key = "751",
                Name = "Test 751"
            };

            context.DummyEntitiesWithCache.Add(newItem);
            context.DummyEntitiesWithCache.Add(newItem2);
            context.DummyEntitiesWithCache.Update(newItem3);

            // Assert
            Assert.AreEqual(1001, context.DummyEntitiesWithCache.Count());
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
            Assert.AreEqual(0, methodCounter.GetIdentifiersCallsCount);

        }

        [TestMethod]
        public void TestFilterCombinesWithPendingChanges()
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
                Key = "1071",
                Name = "Test 1071"
            };

            var newItem3 = new DummyEntity
            {
                Key = "751",
                Name = "Test 751"
            };

            context.DummyEntitiesWithCache.Add(newItem);
            context.DummyEntitiesWithCache.Add(newItem2);
            context.DummyEntitiesWithCache.Update(newItem3);

            // Act
            var result1 = context.DummyEntitiesWithCache.Count(x => new[] { "2" }.Contains(x.Key));

            // Assert
            Assert.AreEqual(1, result1);
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
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            var result = context.DummyEntitiesWithCache.Count(x => x.Name.Contains("1"));

            stopWatch.Stop();

            // Assert
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30);  //effectively gets data from cache
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.IsTrue(result > 0);
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
            Assert.AreEqual(0, methodCounter.GetIdentifiersCallsCount);
        }
    }
}
