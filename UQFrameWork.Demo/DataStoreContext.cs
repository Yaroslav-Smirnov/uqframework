using System.Collections.Generic;
using UQFramework;
using UQFramework.Attributes;
using UQFrameWork.Demo.Dao;

namespace UQFrameWork.Demo
{
    internal class DataStoreContext : UQContext
    {
        public DataStoreContext() : base("db1", new Dictionary<string, object>
        {
            ["folder"] = @"C:\UQFramework.Demo\Files"
        })
        {

        }

        [DataAccessObject(typeof(DaoFile))]
        public IUQCollection<Entity> Entities { get; set; }
    }
}
