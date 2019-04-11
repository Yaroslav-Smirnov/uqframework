using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Linq.Expressions;
using UQFramework.Test.Helpers;

namespace UQFramework.Test.LinqTests
{
    [TestClass]
    public class WhereTest : LinqTestBase
    {
        [TestMethod]
        public void TestCorrectlyProcessBadWhereWithNonCachedProperties()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            var identifiers = (new[] { "1", "2", "17" }).AsEnumerable();

            // Act
            var data = context.DummyEntitiesWithCache
                            .Where(x => identifiers.Contains(x.Key) && x.NonCachedField == "NonCachedData17")
                            .Select(x => x.Key)
                            .ToArray();

            // Assert
            CollectionAssert.AreEquivalent(new[] { "17" }, data); //must be equivalent if is gotten not from cache
            Assert.AreEqual(1000, methodCounter.EntityCallsCount); //ensure calls data store to get data
        }

        [TestMethod]
        public void TestCorrectlyProcessGoodLinq()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            var identifiers = (new[] { "1", "2", "17" }).AsEnumerable();

            // Act
            var data = context.DummyEntitiesWithCache
                            .Where(x => identifiers.Contains(x.Key))
                            .Where(x => x.NonCachedField == "NonCachedData17")
                            .Select(x => x.Key)
                            .ToArray();

            // Assert
            CollectionAssert.AreEquivalent(new[] { "17" }, data); //must be equivalent if is gotten not from cache
            Assert.AreEqual(3, methodCounter.EntityCallsCount); //ensure calls data store to get data
        }

        [TestMethod]
        public void TestCanGetFromCache()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            var identifiers = (new[] { "196", "235", "11" }).AsEnumerable();

            // Act
            var data = context.DummyEntitiesWithCache
                            .Where(x => x.SomeData.Contains("A") && x.Created > new DateTime(2018, 9, 19) || identifiers.Contains(x.Key))
                            .Select(x => x.Key)
                            .ToArray();

            // Assert        
            Assert.IsTrue(data.Count() > 3);
            CollectionAssert.Contains(data, "196");
            CollectionAssert.Contains(data, "235");
            CollectionAssert.Contains(data, "11");
            Assert.AreEqual(0, methodCounter.EntityCallsCount); //ensure gets everything from cache
        }

        [TestMethod]
        public void TestEffectivelyProcessesContains()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            var identifiers = Enumerable.Range(0, 20000).Select(i => i.ToString());

            // Act
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            var data = context.DummyEntitiesWithCache
                            .Where(x => identifiers.Contains(x.Key))
                            .Select(x => x.Key)
                            .ToList();

            stopWatch.Stop();

            // Assert
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount); // only identifiers are used
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30); // proves that Where is replaced
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestWhereWithFilterByString()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            var data = context.DummyEntitiesWithCache
                 .Where(x => (x.Name ?? string.Empty).IndexOf("11", StringComparison.InvariantCultureIgnoreCase) >= 0)
                 .Select(x => new { Id = x.Key, x.Name })
                 .ToList();

            stopWatch.Stop();

            // Assert
            var cacheProvider = ReflectionHelper.WinkleCacheDataProviderOut(context.DummyEntitiesWithCache);
            Assert.AreEqual(0, cacheProvider.CreateEntityFromCachedEntryCount); // only identifiers are used
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30); // proves that Where is replaced
            Assert.AreEqual(0, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestDontUseCacheIfReturnsEntity()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.Where(x => x.Key == "324").FirstOrDefault();

            // Assert
            Assert.AreEqual("NonCachedData324", result.NonCachedField);
            Assert.AreEqual(1, methodCounter.EntityCallsCount);

        }

        [TestMethod]
        public void TestDontUseCacheIfReturnsListOfEntites()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.ToList();

            // Assert
            var nonCachedData = result.Select(x => x.NonCachedField).Where(x => x.Contains("NonCachedData")).ToList();
            Assert.AreEqual(1000, nonCachedData.Count);
            Assert.AreEqual(1000, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        public void TestWhereWithNotEqual()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act 
            var result = context.DummyEntitiesWithCache.Where(x => x.Key != "2" && x.Key !="32").FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestWhereWithFuncDefinedInAnotherMethod()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act 
            var result = context.DummyEntitiesWithCache.Where(x => Predicate()(x)).FirstOrDefault();
            // NOTE: Just Where(Predicate()) will not work

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, methodCounter.EntityCallsCount);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestWhereWithInlinePredicate()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            Func<DummyEntity, int> func = e => int.Parse(e.Key);

            var result = context.DummyEntitiesWithCache.Where(x => func(x) == 4).ToList();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("4", result[0].Key);
            Assert.AreEqual(1, methodCounter.EntityCallsCount); // 1 call for it process filter on cache first
        }

        [TestMethod]
        public void TestWhereWithPredicateDefinedInAnotherMethod()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act 
            var result = context.DummyEntitiesWithCache.Where(x => Predicate(x)).FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(methodCounter.EntityCallsCount > 100); // proves that cache is not used
        }

        [TestMethod]
        public void TestWhereWithExpressionPredicateDefinedInAnotherMethod()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act 
            var result = context.DummyEntitiesWithCache.Where(ExprPredicate()).FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, methodCounter.EntityCallsCount); // proves that cache is not used
        }

        private static Expression<Func<DummyEntity, bool>> ExprPredicate()
        {
            return e => e.Key != "2" && e.Key != "13";
        }

        private static Func<DummyEntity,bool> Predicate()
        {
            return e => e.Key != "2" && e.Key != "13";
        }

        private static bool Predicate(DummyEntity e)
        {
            return e.Key != "2" && e.Key != "13";
        }
    }
}
