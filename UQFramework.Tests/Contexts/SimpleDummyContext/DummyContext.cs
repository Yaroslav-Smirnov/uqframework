using System.Collections.Generic;
using UQFramework.Attributes;

namespace UQFramework.Tests
{
    internal class DummyContext : UQContext
    {
        public DummyContext(string folder, DaoMethodCallsCounter methodCallerCounter = null) : base("db1", new Dictionary<string, object>
        {
            ["folder"] = folder,
            ["counter"] = methodCallerCounter
        })
        {

        }

        [DataAccessObject(typeof(DummyEntityDAO))]
        public IUQCollection<DummyEntity> DummyEntities { get; private set; }

        [DataAccessObject(typeof(DummyEntityDAO2))]
        public IUQCollection<DummyEntity> DummyEntitiesWithCache { get; private set; }
    }
}
