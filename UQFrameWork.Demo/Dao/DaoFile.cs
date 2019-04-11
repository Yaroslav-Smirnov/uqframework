using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UQFramework.Attributes;
using UQFramework.DAO;


namespace UQFrameWork.Demo.Dao
{
    [DaoVersion("1.1.0.0")]
    internal class DaoFile : IDataSourceReader<Entity>, IDataSourceEnumerator<Entity>, IDataSourceWriter<Entity>, INeedDataSourceProperties
    {
        private string _folder;

        public void SetProperties(IReadOnlyDictionary<string, object> properties)
        {
            _folder = (string)properties["folder"];
        }

        public Entity GetEntity(string identifier)
        {
            var file = Path.Combine(_folder, $"{identifier}.json");
            if (!File.Exists(file))
                return null;
            return JsonConvert.DeserializeObject<Entity>(File.ReadAllText(file));
        }

        public void AddEntity(Entity entity)
        {
            if (string.IsNullOrEmpty(entity.Identifier))
                entity.Identifier = (GetAllEntitiesIdentifiers().Max(int.Parse) + 1).ToString();

            var file = Path.Combine(_folder, $"{entity.Identifier}.json");
            File.WriteAllText(file, JsonConvert.SerializeObject(entity));
        }

        public void UpdateEntity(Entity entity)
        {
            var file = Path.Combine(_folder, $"{entity.Identifier}.json");
            File.WriteAllText(file, JsonConvert.SerializeObject(entity));
        }

        public void DeleteEntity(Entity entity)
        {
            var file = Path.Combine(_folder, $"{entity.Identifier}.json");
            File.Delete(file);
        }

        public IEnumerable<string> GetAllEntitiesIdentifiers()
        {
            return Directory.EnumerateFiles(_folder, "*.json").Select(Path.GetFileNameWithoutExtension);
        }

        public int Count()
        {
            return Directory.EnumerateFiles(_folder, "*.json").Count();
        }
    }
}
