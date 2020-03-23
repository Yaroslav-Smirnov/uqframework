using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using UQFramework.Configuration;

namespace UQFramework.Tests
{
    [TestClass]
    public class UQCollectionTest
    {
        private string _folder;

        [TestInitialize]
        public void TestSetup()
        {
            _folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_folder);

            var cacheFolder = Path.Combine(_folder, "Cache");
            Directory.CreateDirectory(cacheFolder);

            // override configuration
            var config = UQConfiguration.Instance as UQConfiguration;
            config.HorizontalCacheConfigurationSection.SetParameter("folder", cacheFolder);
            config.HorizontalCacheConfigurationSection.ProviderType = typeof(TestTextFileCacheProvider<>);
            config.HorizontalCacheConfigurationSection.IsEnabled = true;


            // create 1000 files for testing
            var dao = new DummyEntityDAO2();

            dao.SetProperties(new Dictionary<string, object>
            {
                ["folder"] = _folder
            });

            dao.GenerateFiles(1000);

            // forces the cache initialization
            var context = new DummyContext(_folder);
            context.DummyEntitiesWithCache.Select(x => x.Key).ToList();

        }

        [TestCleanup]
        public void TestTeardown()
        {
            Directory.Delete(_folder, true);
        }

        [TestMethod]
        public void TestCreateUQCollection()
        {
            var collection = new UQCollection<DummyEntity>();

            Assert.IsNotNull(collection);
        }


        [TestMethod]
        public void TestSavesUpdatedEntitiesAndReturnsThemInQueryCached()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            var itemToAdd = new DummyEntity
            {
                Key = "4000",
                Name = "Item 4000"
            };

            context.DummyEntitiesWithCache.Add(itemToAdd);

            // Ensure item do not exist

            var result = context.DummyEntitiesWithCache.Where(x => x.Key == "4000").Select(x => x.Name).FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Item 4000", result);
            //Assert.AreEqual(1000, methodCounter.EntityCallsCount); // 1000 calls for cannot get id from invocations
        }

        [TestMethod]
        public void TestSavesUpdatedEntitiesAndReturnsThemInQueryNonCached()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            var itemToAdd = new DummyEntity
            {
                Key = "4000",
                Name = "Item 4000"
            };

            context.DummyEntitiesWithCache.Add(itemToAdd);

            // Ensure item do not exist

            var result = context.DummyEntitiesWithCache.Where(x => x.Key == "4000").FirstOrDefault();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Item 4000", result.Name);
            //Assert.AreEqual(1000, methodCounter.EntityCallsCount); // 1000 calls for cannot get id from invocations
        }

        [TestMethod]
        public void TestQuery()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.Query<bool>("GetTest");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestQueryNoSuchMethod()
        {
            // Arrange
            var methodCounter = new DaoMethodCallsCounter();
            var context = new DummyContext(_folder, methodCounter);

            // Act
            var result = context.DummyEntitiesWithCache.Query<bool>("NoSuchMethod");

            // Assert
            Assert.IsTrue(result);
        }
    }

}
