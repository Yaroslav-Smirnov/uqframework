using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using UQFramework.Attributes;

namespace UQFramework.Helpers
{
    internal static class GeneralHelper
    {
        public static Func<T, string> GetIdentiferGetter<T>(out PropertyInfo keyProperty)
        {
            keyProperty = GetIdentifierProperty(typeof(T));

            if (keyProperty == null)
                throw new InvalidOperationException($"Can't find property with [Key] attribute in type {typeof(T)}");

			return GetStringPropertyGetter<T>(keyProperty);
        }

		public static Func<T,string> GetStringPropertyGetter<T>(PropertyInfo propertyInfo)
		{
			if (propertyInfo == null)
				throw new ArgumentNullException(nameof(propertyInfo));

			if (propertyInfo.GetMethod == null)
				throw new InvalidOperationException($"Property {propertyInfo.Name} in type {typeof(T)} does not have a public getter");

			return (Func<T, string>)Delegate.CreateDelegate(typeof(Func<T, string>), propertyInfo.GetMethod);
		}

        public static IEnumerable<PropertyInfo> GetPropertiesHavingAttribute(Type objectType, Type attributeType)
        {
            return
                objectType
                    .GetProperties()
                    .Where(p => p.GetCustomAttributes(attributeType, false).Any());
        }

        public static PropertyInfo GetIdentifierProperty(Type type)
        {
            var identifierProperty = GetPropertiesHavingAttribute(type, typeof(IdentifierAttribute)).SingleOrDefault();

            if (identifierProperty != null)
                return identifierProperty;

            // if no identifier attribute then should be exactly one key attribute
            return GetPropertiesHavingAttribute(type, typeof(KeyAttribute)).SingleOrDefault();
        }
    }
}
