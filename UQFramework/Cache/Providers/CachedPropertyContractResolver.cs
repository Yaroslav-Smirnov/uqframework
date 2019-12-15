using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UQFramework.Cache.Providers
{
	public class CachedPropertyContractResolver<T> : DefaultContractResolver
	{
		private readonly IEnumerable<PropertyInfo> _cachedProperties;
		private IList<JsonProperty> _serializedProperties;
		internal CachedPropertyContractResolver(IEnumerable<PropertyInfo> cachedProperties)
		{
			_cachedProperties = cachedProperties;
		}
		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			if (type != typeof(T)) // just in case we have a hierarchy here
				return base.CreateProperties(type, memberSerialization);

			if (_serializedProperties != null)
				return _serializedProperties;

			var properties = base.CreateProperties(type, memberSerialization);

			return _serializedProperties = properties.Where(jp => _cachedProperties.Any(p => p.Name == jp.PropertyName)).ToList();
		}
	}
}
