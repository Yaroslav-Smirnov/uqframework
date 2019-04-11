using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UQFramework.DAO;

namespace UQFramework.Test
{
    // json reader for tests
    internal class DummyEntityDAO2 : IDataSourceReader<DummyEntity>, IDataSourceEnumerator<DummyEntity>
        , INeedDataSourceProperties
    {
        private string _folder;
        private DaoMethodCallsCounter _methodCallerCounter;

        public void SetProperties(IReadOnlyDictionary<string, object> properties)
        {
            _folder = (string)properties["folder"];
            properties.TryGetValue("counter", out var methodCounter);
            _methodCallerCounter = (DaoMethodCallsCounter)methodCounter;
        }

        #region Generate Files

        public void GenerateFiles(int number, bool overwrite = false)
        {
            var random = new Random();

            var data = Enumerable.Range(0, number).Select(i => RandomString(random, 20)).ToArray();

            Parallel.For(0, number, i =>
                {
                    var file = GetFileName(i.ToString());

                    if (File.Exists(file) && !overwrite)
                        return;

                    var item = new DummyEntity
                    {
                        Key = i.ToString(),
                        Name = $"Dummy Item {i}",
                        SomeData = data[i],
                        Created = DateTime.Now.AddSeconds(i),
                        ListData = new List<string> { "1", "2", "3" },
                        NonCachedField = $"NonCachedData{i}"
                    };

                    File.WriteAllText(file, JsonConvert.SerializeObject(item));
                });
        }

        private static string RandomString(Random random, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_&@";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #endregion

        public DummyEntity GetEntity(string identifier)
        {
            if (_methodCallerCounter != null)
                _methodCallerCounter.GetEntityCalled();

            var file = GetFileName(identifier);

            if (!File.Exists(file))
                return null;

            return JsonConvert.DeserializeObject<DummyEntity>(File.ReadAllText(file));
        }

        private string GetFileName(string identifier) => Path.Combine(_folder, $"{identifier}.json");

        public IEnumerable<string> GetAllEntitiesIdentifiers()
        {
            if (_methodCallerCounter != null)
                _methodCallerCounter.GetIdentifiersCalled();
            return Directory.EnumerateFiles(_folder, "*.json").Select(Path.GetFileNameWithoutExtension);
        }

        public int Count()
        {
            return Directory.EnumerateFiles(_folder, "*.json").Count();
        }
    }
}
