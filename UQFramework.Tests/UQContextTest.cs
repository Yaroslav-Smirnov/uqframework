using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UQFramework.Configuration;

namespace UQFramework.Tests
{
    [TestClass]
    public class UQContextTest
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
        public void TestInitializeCollectionUsingReflection()
        {
            // Arrange - Act
            var context = new DummyContext(_folder);

            // Assert
            Assert.IsNotNull(context.DummyEntities);

            var data = context.DummyEntities.ToList();

            Assert.AreEqual(2, data.Count);

            var data2 = context.DummyEntitiesWithCache.ToList();

            Assert.AreEqual(1000, data2.Count);
        }

        [TestMethod]
        public void TestKeepCacheUntouched()
        {
            // Arrange
            var context = new DummyContext(_folder);

            var query = context.DummyEntitiesWithCache.Select(e => new { e.Key, e.Name, e.ListData }).Where(x => x.Key == "718");

            // Act
            var item = query.FirstOrDefault();

            Assert.IsNotNull(item);

            (item.ListData as List<string>).Add("4");

            var item2 = query.FirstOrDefault();

            // Assert
            Assert.IsNotNull(item2);

            Assert.AreEqual(3, item2.ListData.Count()); // the cached data should not be afffected
        }

        [TestMethod]
        public void TestContains()
        {
            // Arrange
            var context = new DummyContext(_folder);

            // Act
            var query = context.DummyEntitiesWithCache.Where(x => new[] { "713", "100", "5", "359", "991" }.Contains(x.Key));

            var data = query.Select(x => new { x.Key, x.Name }).ToList();

            // Assert
            Assert.AreEqual(5, data.Count);

        }
    }
}
