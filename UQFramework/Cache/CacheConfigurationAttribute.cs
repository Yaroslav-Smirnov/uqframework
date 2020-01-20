using System;
using UQFramework.Configuration;

namespace UQFramework.Cache
{
	[AttributeUsage(AttributeTargets.Class)]
	public class CacheConfigurationAttribute : Attribute
	{
		private readonly Type _configurationType;
		public CacheConfigurationAttribute(Type configurationType)
		{
			if (configurationType == null)
				throw new ArgumentNullException(nameof(configurationType));

			if (!typeof(IHorizontalCacheConfiguration).IsAssignableFrom(configurationType))
				throw new InvalidOperationException($"Configuration class must implement {nameof(IHorizontalCacheConfiguration)}");

			_configurationType = configurationType;
		}

		internal IHorizontalCacheConfiguration Configuration => (IHorizontalCacheConfiguration)Activator.CreateInstance(_configurationType);
	}
}
