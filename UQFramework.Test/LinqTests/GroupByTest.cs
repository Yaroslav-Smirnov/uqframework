using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using UQFramework.Test.Helpers;

namespace UQFramework.Test.LinqTests
{
    [TestClass]
    public class GroupByTest : LinqTestBase
    {
        [TestMethod]
        public void TestSimpleGroupBy()
        {

            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.GroupBy(x => int.Parse(x.Key) % 10)
                            .Select(g => 
                                new
                                {
                                    g.Key,
                                    Count = g.Count()
                                })
                            .ToList();

            // Assert
            Assert.IsNotNull(result);
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestGroupByWithToList()
        {

            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.GroupBy(x => int.Parse(x.Key) % 10)
                            .Select(g =>
                                new
                                {
                                    g.Key,
                                    Data = g.ToList()
                                })
                            .ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(10, result.Count);

            foreach (var x in result)
            {
                Assert.AreEqual(100, x.Data.Count);
                foreach(var d in x.Data)
                {
                    Assert.IsNotNull(d.NonCachedField);
                }
            }

            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(1000, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestGroupByWithComplexProjection()
        {

            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.GroupBy(x => int.Parse(x.Key) % 10)
                            .Select(g =>
                                new
                                {
                                    g.Key,
                                    Data = g.Select(x => new
                                    {
                                        x.Key,
                                        x.Name,
                                        x.SomeData
                                    })
                                    .ToList()
                                })
                            .ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(10, result.Count);

            foreach (var x in result)
            {
                Assert.AreEqual(100, x.Data.Count);
                foreach (var d in x.Data)
                {
                    Assert.IsNotNull(d.SomeData);
                }
            }

            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
        }
    }
}
