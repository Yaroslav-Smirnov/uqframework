using System.Collections.Generic;
using UQFramework.Attributes;
using UQFramework.Test.Helpers;

namespace UQFramework.Test
{
	public class ContextWithRelations : UQContext
	{
		public ContextWithRelations(MethodCallsRecorder methodCallsRecorder) : base("db1", new Dictionary<string, object>
												{
													["methodCallsRecorder"] = methodCallsRecorder
												})
		{}

		[DataAccessObject(typeof(DummyDataAccessObject<DummyEntity1>))]
		public IUQCollection<DummyEntity1> DummyEntities1 { get; private set; }

		[DataAccessObject(typeof(DummyDataAccessObject<DummyEntity2>))]
		public IUQCollection<DummyEntity2> DummyEntities2 { get; private set; }

		[DataAccessObject(typeof(DummyDataAccessObject<DummyEntity3>))]
		public IUQCollection<DummyEntity3> DummyEntities3 { get; private set; }

		[DataAccessObject(typeof(DummyDataAccessObject<DummyEntity4>))]
		public IUQCollection<DummyEntity4> DummyEntities4 { get; private set; }

	}
}
