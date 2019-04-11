using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UQFramework.Configuration;

namespace UQFramework.Test.LinqTests
{
    public abstract class LinqTestBase
    {
        protected string _folder;

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
            config.HorizontalCacheConfigurationSection.SetCacheDataProviderType(typeof(TestCacheDataProvider<>));

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
    }
}
