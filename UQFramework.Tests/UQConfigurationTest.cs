using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using UQFramework.Cache;
using UQFramework.Cache.Providers;
using UQFramework.Configuration;

namespace UQFramework.Tests
{
    [TestClass]
    public class UQConfigurationTest
    {
        private static string _data;
        [TestInitialize]
        public void Setup()
        {
            // reset configuration
            var field = typeof(UQConfiguration).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(null, null);
            ConfigurationManager.RefreshSection("uqframework");
            // read current app.config
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            _data = File.ReadAllText(config.FilePath);
            Assert.IsTrue(_data.Contains("<uqframework>"));
        }

        [TestCleanup]
        public void TearDown()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            File.WriteAllText(config.FilePath, _data);
            // reset instance
            var instanceField = typeof(UQConfiguration).GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(instanceField);
            instanceField.SetValue(null, null);
            ConfigurationManager.RefreshSection("uqframework");
        }

        [TestMethod] // tests parameters in the test project app.config file
        public void TestConfiguration()
        {
            // Arrange - Act
            var config = UQConfiguration.Instance;

            // Assert
            Assert.IsTrue(config.HorizontalCacheConfiguration.IsEnabled);
            Assert.AreEqual(typeof(TextFileCacheProvider<>), config.HorizontalCacheConfiguration.ProviderType);

            var data = CacheInitializer.GetCachedDataProvider(typeof(DummyEntity), "db1", config.HorizontalCacheConfiguration as IHorizontalCacheConfigurationInternal, new DummyEntityDAO2());

            var memCache = typeof(CachedDataProviderBase<DummyEntity>).GetField("_memoryCacheService", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(data);

            var provider = typeof(MemoryCacheService<DummyEntity>).GetField("_cacheProvider", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(memCache);

            var folder = typeof(TextFileCacheProvider<DummyEntity>).GetField("_folder", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(provider);

            Assert.AreEqual(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\UQFramework", folder);
        }

        [TestMethod]
        public void TestEmptyConfig()
        {
            // Arrange
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.Sections.Remove("uqframework");
            config.Save();
            ConfigurationManager.RefreshSection("uqframework");
            Assert.IsNull(ConfigurationManager.GetSection("uqframework"));

            // Act
            var uqConfig = UQConfiguration.Instance;

            // Assert
            Assert.IsNotNull(uqConfig);
            Assert.IsNotNull(uqConfig.HorizontalCacheConfiguration);
            Assert.IsFalse(uqConfig.HorizontalCacheConfiguration.IsEnabled);           
        }
    }
}
