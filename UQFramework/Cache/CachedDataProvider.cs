using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UQFramework.Cache
{
    internal class CachedDataProvider<T> : CachedDataProviderBase<T> where T : new()
    {
        private readonly IEnumerable<PropertyInfo> _cachedProperties;
        private readonly PropertyInfo _keyProperty;

        private readonly IEnumerable<IPropertySetter> _setters;

        public CachedDataProvider(IEnumerable<PropertyInfo> cachedProperties, PropertyInfo keyProperty, PersistentCacheProviderBase<T> persistentCacheProvider)
            : base(persistentCacheProvider)
        {
            _keyProperty = keyProperty;
            _cachedProperties = cachedProperties;

            _setters = cachedProperties.Select(p =>
            {
                Type type;
                if (typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string))
                {
                    type = typeof(CollectionPropertySetter<>).MakeGenericType(typeof(T), p.PropertyType.GenericTypeArguments[0]);
                }
                else
                {
                    type = typeof(PropertySetter<>).MakeGenericType(typeof(T), p.PropertyType);
                }

                var setter = (IPropertySetter)Activator.CreateInstance(type);

                setter.Initialize(p);

                return setter;

            }).ToArray();
        }

        public override IEnumerable<string> GetCachedProperties() => _cachedProperties.Select(p => p.Name);

        protected override T CreateEntityFromCachedEntry(T cachedEntry)
        {
            // YSV: we can't just return entry for it gives a live reference to the cache and might be then accidently changed.
            // So we are creating a new entity, even though it takes time.

            var newEntity = new T();

            foreach (var setter in _setters)
            {
                setter.SetValue(newEntity, cachedEntry);
            }

            return newEntity;
        }

        private interface IPropertySetter
        {
            void SetValue(T target, T source);

            void Initialize(PropertyInfo propInfo);
        }

        private class PropertySetter<TProp> : IPropertySetter
        {
            private Action<T, TProp> _setProp;
            private Func<T, TProp> _getProp;

            public void SetValue(T target, T source)
            {
                _setProp(target, _getProp(source));
            }

            public void Initialize(PropertyInfo propInfo)
            {
                if (propInfo.SetMethod == null || propInfo.GetMethod == null)
                    throw new InvalidOperationException($"Property {propInfo.Name} cannot be cached: setter or getter is absent");

                _setProp = (Action<T, TProp>)Delegate.CreateDelegate(typeof(Action<T, TProp>), propInfo.SetMethod);
                _getProp = (Func<T, TProp>)Delegate.CreateDelegate(typeof(Func<T, TProp>), propInfo.GetMethod);
            }
        }

        private class CollectionPropertySetter<TItemType> : IPropertySetter
        {
            private Action<T, IEnumerable<TItemType>> _setProp;
            private Func<T, IEnumerable<TItemType>> _getProp;

            public void Initialize(PropertyInfo propInfo)
            {
                _setProp = (Action<T, IEnumerable<TItemType>>)Delegate.CreateDelegate(typeof(Action<T, IEnumerable<TItemType>>), propInfo.SetMethod);
                _getProp = (Func<T, IEnumerable<TItemType>>)Delegate.CreateDelegate(typeof(Func<T, IEnumerable<TItemType>>), propInfo.GetMethod);
            }

            public void SetValue(T target, T source)
            {
                var value = _getProp(source);
                if (value == null)
                    return;
                _setProp(target, new List<TItemType>(value));
            }
        }
    }
}
