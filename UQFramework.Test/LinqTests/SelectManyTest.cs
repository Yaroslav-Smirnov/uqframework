using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using UQFramework.Test.Helpers;

namespace UQFramework.Test.LinqTests
{
    [TestClass]
    public class SelectManyTest : LinqTestBase
    {
        [TestMethod]
        public void TestSelectManySimple()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache
                            .SelectMany(x => x.ListData.Select(i =>
                            new
                            {
                                x.Key,
                                Number = i
                            }).ToList())
                            .Where(x => x.Key.Contains(x.Number))
                            .ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(813, result.Count);

            foreach (var x in result)
            {
                Assert.IsTrue(x.Key.Contains(x.Number));
            }

            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(1000, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestSelectManyCombinesWithPendingChangesAddOrUpdate()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);
            context.DummyEntitiesWithCache.Add(new DummyEntity
            {
                Key = "1001",
                ListData = new[]
                {
                    "1", "2", "10"
                }
            });

            context.DummyEntitiesWithCache.Update(new DummyEntity
            {
                Key = "0",
                ListData = new[]
                {
                    "0", "1", "2"
                }
            });

            // Act
            var result = context.DummyEntitiesWithCache
                            .SelectMany(x => x.ListData.Select(i =>
                            new
                            {
                                x.Key,
                                Number = i
                            }).ToList())
                            .Where(x => x.Key.Contains(x.Number))
                            .ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(816, result.Count);

            foreach (var x in result)
            {
                Assert.IsTrue(x.Key.Contains(x.Number));
            }

            Assert.IsNotNull(result.FirstOrDefault(x => x.Key == "1001" && x.Number == "1"));
            Assert.IsNotNull(result.FirstOrDefault(x => x.Key == "1001" && x.Number == "10"));
            Assert.IsNotNull(result.FirstOrDefault(x => x.Key == "0" && x.Number == "0"));

            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(1000, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestSelectManyCombinesWithPendingChangesRemove()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);
            var itemToRemove = context.DummyEntitiesWithCache.FirstOrDefault(x => x.Key == "121");
            Assert.AreEqual(1, methodCounter.EntityCallsCount); // one dao call to retrieve an entity

            Assert.IsNotNull(itemToRemove);
            context.DummyEntitiesWithCache.Remove(itemToRemove);

            // Act
            var result = context.DummyEntitiesWithCache
                            .SelectMany(x => x.ListData.Select(i =>
                            new
                            {
                                x.Key,
                                Number = i
                            }).ToList())
                            .Where(x => x.Key.Contains(x.Number))
                            .ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(811, result.Count);

            foreach (var x in result)
            {
                Assert.IsTrue(x.Key.Contains(x.Number));
            }

            Assert.IsNull(result.FirstOrDefault(x => x.Key == "121"));

            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(1000, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(1, methodCounter.EntityCallsCount); //
        }


    }
}
