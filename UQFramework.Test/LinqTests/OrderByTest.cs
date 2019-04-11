using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UQFramework.Test.Helpers;

namespace UQFramework.Test.LinqTests
{
    [TestClass]
    public class OrderByTest : LinqTestBase
    {
        [TestMethod]
        public void TestOrderByWithSimpleKey()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.OrderBy(x => x.Key).FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount);
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestOrderByReturningAnonymous_NotNice()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache
                        .OrderBy(x => x.Key)
                        .Select(x => new
                        {
                            x.Key,
                            x.Name
                        })
                        .FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount); //ok that's not nice
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestOrderByWithTake()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache
                        .OrderBy(x => x.Key)
                        .Select(x => new
                        {
                            x.Key,
                            x.Name
                        })
                        .Take(3)
                        .ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("0", result[0].Key);
            Assert.AreEqual("Dummy Item 0", result[0].Name);

            Assert.AreEqual("1", result[1].Key);
            Assert.AreEqual("Dummy Item 1", result[1].Name);

            Assert.AreEqual("10", result[2].Key);
            Assert.AreEqual("Dummy Item 10", result[2].Name);

            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount); //ok that's not nice
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestOrderByReturningAnonymousWithDependencyOfEntity_NotNice()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache
                        .OrderBy(x => x.Key)
                        .Select(x => new
                        {
                            x.Key,
                            Value = x
                        })
                        .Take(3)
                        .ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);

            Assert.AreEqual("0", result[0].Key);
            Assert.AreEqual("0", result[0].Value.Key);
            Assert.AreEqual("Dummy Item 0", result[0].Value.Name);
            Assert.AreEqual("NonCachedData0", result[0].Value.NonCachedField);

            Assert.AreEqual("1", result[1].Key);
            Assert.AreEqual("1", result[1].Value.Key);
            Assert.AreEqual("Dummy Item 1", result[1].Value.Name);
            Assert.AreEqual("NonCachedData1", result[1].Value.NonCachedField);

            Assert.AreEqual("10", result[2].Key);
            Assert.AreEqual("10", result[2].Value.Key);
            Assert.AreEqual("Dummy Item 10", result[2].Value.Name);
            Assert.AreEqual("NonCachedData10", result[2].Value.NonCachedField);

            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount); //ok that's not nice
            Assert.AreEqual(1000, methodCounter.EntityCallsCount);
        }
    }
}
