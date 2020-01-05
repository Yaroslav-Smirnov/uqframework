using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using UQFramework.Attributes;
using UQFramework.Cache.Providers;
using UQFramework.Helpers;

namespace UQFramework.Tests
{
    [TestClass]
    public class TextFileCacheProviderTest
    {
        [ClassInitialize]
        public static void Setup(TestContext testContext)
        {
            Directory.CreateDirectory(@"C:\Tests");
            Directory.CreateDirectory(@"C:\Tests\DataSource");

            var dao = new DummyEntityDAO2();

            dao.SetProperties(new Dictionary<string, object>
            {
                ["folder"] = @"C:\Tests\DataSource"
            });

            dao.GenerateFiles(100000);
        }

        [TestMethod]
        [Ignore] // takes time
        public void TestFullRebuild()
        {
            var folder = @"C:\Tests\";
            var dao = new DummyEntityDAO2();

            dao.SetProperties(new Dictionary<string, object>
            {
                ["folder"] = Path.Combine(folder, "DataSource")
            });

            var cacheProvider = new TextFileCacheProvider<DummyEntity>("DB1", new Dictionary<string, string> { ["folder"] = folder } , dao, GeneralHelper.GetPropertiesHavingAttribute(typeof(DummyEntity), typeof(CachedAttribute)));

            var stopwatch = new System.Diagnostics.Stopwatch();

            stopwatch.Start();

            cacheProvider.FullRebuild();

            stopwatch.Stop();

            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100000);

            Assert.IsTrue(File.Exists(Path.Combine(folder, "TN.DB1.UQFramework.Test.DummyEntity.UQFramework.Test.DummyEntityDAO2.txt")));

            var cache = cacheProvider.GetAllCachedItems();

            Assert.AreEqual(100000, cache.Count);

            var itemId = "32017"; // just random id to check that cached is properly

            var cachedData = cache[itemId].SomeData;

            var realItem = dao.GetEntity(itemId);

            Assert.AreEqual(realItem.SomeData, cachedData);
            
        }
    }
}
