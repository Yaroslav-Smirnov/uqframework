using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using UQFramework.Tests.Helpers;

namespace UQFramework.Tests.LinqTests
{
    [TestClass]
    public class AnyTest : LinqTestBase
    {
        [TestMethod]
        public void TestAnyWithContainsFilter()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act (exists)
            var identifiers = Enumerable.Range(177, 20000).Select(i => i.ToString());
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            var result = context.DummyEntitiesWithCache
                            .Any(x => identifiers.Contains(x.Key));

            stopWatch.Stop();

            // Assert
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30);
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount); // only identifiers are used
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
            Assert.AreEqual(0, methodCounter.GetIdentifiersCallsCount);
            Assert.IsTrue(result);

            // Act (do not exists)
            var identifiers2 = Enumerable.Range(1770, 20000).Select(i => i.ToString());
            stopWatch.Restart();

            var result2 = context.DummyEntitiesWithCache
                            .Any(x => identifiers2.Contains(x.Key));

            stopWatch.Stop();

            // Assert
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30);  //Ok, it takes longer for it need to check the range

            Assert.AreEqual(0, methodCounter.EntityCallsCount);
            Assert.AreEqual(0, methodCounter.GetIdentifiersCallsCount);
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public void TestAnyWithoutFilter()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Restart();

            var result = context.DummyEntitiesWithCache.Any();

            stopWatch.Stop();

            // Assert
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30);  //effectively gets data from cache
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
            Assert.AreEqual(0, methodCounter.GetIdentifiersCallsCount);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestCombinesWithPendingChanges()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act - Removing
            for (var i = 0; i < 1000; i++)
            {
                context.DummyEntitiesWithCache.Remove(new DummyEntity
                {
                    Key = i.ToString()
                });
            }

            // Assert
            Assert.IsFalse(context.DummyEntitiesWithCache.Any());
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
            Assert.AreEqual(0, methodCounter.GetIdentifiersCallsCount);

            // Act - Adding
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

            context.DummyEntitiesWithCache.Add(newItem);
            context.DummyEntitiesWithCache.Add(newItem2);

            // Assert
            Assert.IsTrue(context.DummyEntitiesWithCache.Any());
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

            var result = context.DummyEntitiesWithCache.Any(x => x.Name.Contains("100"));

            stopWatch.Stop();

            // Assert
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);

            // That's vary from 1 to 129, but we ultimately want it only once
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30);  //effectively gets data from cache
            Assert.IsTrue(result);
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
            Assert.AreEqual(0, methodCounter.GetIdentifiersCallsCount);
        }
    }
}
