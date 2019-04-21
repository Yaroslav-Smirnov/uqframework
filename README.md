# UQframework
UQFramework .Net Standard Library which allows linq to any data store similiar to Linq2SQL and Entity Framework.

The library has been developed within [LEAPDev](https://leapdev.io/) and is used on various projects to access NoSql storages such as files, S3, Rest API etc.

# Getting Started

## Creating Model
Any model with string identifier can be used in UQFramework. The identifier property must be marked with [Key] or [Identifier] attribute.
```C#
    public class Entity
    {
        [Key]
        public string Identifier { get; set; }

        public string Name { get; set; }

        public string Property1 { get; set; }
    }
```

## Creating Data Access Components (DAO)

There are following interfaces available for implementation in Data Access Components:
* *IDataSourceReader<>* - supports getting entity by identifier;
* *IDataBulkReader<>* - supports getting a set of entities by a set of identifiers in one go;
* *IDataSourceWriter<>* - supports CRUD operations;
* *IDataSourceBulkWriter<>* - supports updating, creation and deletion of entities in one go.
* *INeedDataSourceProperties* - allows passing parameters to datastore (such as connection strings)
* *IDataSourceEnumerator* - supports enumeration of the entities. Must be implemented if caching is used (see below).

if DAO is intended to be used for fetching data it must implement either IDataSourceReader<> or IDataBulkReader<>. 
if in addition to that data changes are required then it must implement either IDataSourceWriter<> or IDataSourceBulkWriter<>

INeedDataSourceProperties is used when DAO needs some configuration to access the data. In the example below DAO operates over files in a folder and gets the path to this folder from the parameters supplied through INeedDataSourceProperties interface.

**Example**
```C#
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
```

## Creating Context

To access entities in collections and being able to query them using LINQ a context class must be created, according to the following rules:
* It must iherit from *UQContext*
* For each entity a property of type IUQCollection<> must be created and it **must have a setter** (preferably a private one).
* The DAO matching the property should be specified in *DataAccessObject* attribute

**Example**
```C#
    internal class DataStoreContext : UQContext
    {
        public DataStoreContext() : base("db1", new Dictionary<string, object>
        {
            ["folder"] = @"C:\UQFramework.Demo\Files"
        })
        { }

        [DataAccessObject(typeof(DaoFile))]
        public IUQCollection<Entity> Entities { get; set; }
    }
```

In the example above the base constructor take *dataStoreId* and a dictionary of paramters which will be internally passed to all data access components implementing the INeedDataSourceProperties interface. 
*dataStoreId* - is logical name for the data storage which also allows switching between different datastores if the application supports many.

## Querying the Entities
After all the previous steps are done data can be filtered and projected using LINQ:

```C#
    var context = new DataStoreContext();
    // search by substring
    var subString = "aSubstring";
    var list = context.Entities
        .Where(x => x.Name.IndexOf(subString, StringComparison.InvariantCultureIgnoreCase) >= 0)
        .ToList();
```
See UQFramework.Demo project for more details.

## CRUD Operations
(TBD)

## Caching
Filtering operations (such as Where clause and so on) can take a lot of time in case of large data stores for each item needs to be loaded and checked for meeting the filter criteria. UQ Framework allows to improve performance of such operations significantly by caching of a subset of entity properties. 
Cached properties are kept in memory along with the item identifier so that when only cached properties are used in an expression UQ framework is able to execute filter over the cache. Then when identifiers of the items is obtained only they are loaded from the datastore so the resulting number of data requests is significantly reduced. Furthermore, if only cached properties are retrieved (lets say an aggregation operation is used or Select projects on another type) then UQFrameworks executes the whole query entirely in memory without any call to data access components. 

UQ keeps the cache in sync by updating on each CRUD operation (only added, changed or deleted items are updated).

To enable caching in UQ Framework the following needs to be done
* DAO must implement *IDataSourceEnumerator<>* or *IDataSourceEnumeratorEx<>* interface
* Cache storage must be configured in App.config
* Each cached property must be decorated with *Cached* attribute. 

### Cache configuration
(TBD)
