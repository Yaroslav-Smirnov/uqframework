using UQFramework.Attributes;

namespace UQFramework.Test
{
	public class DummyEntity4
	{
		[Identifier]
		public string Key { get; set; }

		public string Name { get; set; }

		[EntityIdentifier(typeof(DummyEntity3))]
		public string Entity3Id { get; set; }
	}
}
