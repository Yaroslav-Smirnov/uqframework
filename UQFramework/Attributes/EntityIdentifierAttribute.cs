using System;

namespace UQFramework.Attributes
{
	// indicates that a property hold identifier of a parent entity (analogue of ForeignKey in relational databases)
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class EntityIdentifierAttribute : Attribute
	{		
		public EntityIdentifierAttribute(Type entityType)
		{
			EntityType = entityType;
		}

		internal Type EntityType {get;}
	}
}
