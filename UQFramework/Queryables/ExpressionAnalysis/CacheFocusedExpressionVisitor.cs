using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace UQFramework.Queryables.ExpressionAnalysis
{
    internal class CacheFocusedExpressionVisitor<T> : ExpressionVisitor
    {
        private readonly Type _entitiesType;
        private readonly PropertyInfo _keyProperty;

        public CacheFocusedExpressionVisitor(PropertyInfo keyProperty)
        {
            _keyProperty = keyProperty ?? throw new ArgumentNullException(nameof(keyProperty));
            _entitiesType = typeof(T);
        }

        public bool UsesOnlyCachedProperties { get; private set; } = true;

        public bool UsesOnlyKeyProperties { get; private set; } = true;

        public bool UsesCachedEnumerables { get; private set; } = false;

        protected override Expression VisitNew(NewExpression node)
        {
            var arguments = node.Arguments;

            foreach (var arg in arguments)
            {
                if (arg.Type == _entitiesType || typeof(IEnumerable<T>).IsAssignableFrom(arg.Type))
                {
                    // we use the whole entity as a parameter which means we can't use only 
                    UsesOnlyCachedProperties = false;
                    UsesOnlyKeyProperties = false;
                }
            }
            return base.VisitNew(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            CheckCangetFromCache(node.Member);

            return base.VisitMember(node);
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            throw new NotSupportedException($"Invocations are not supported, please re-write your linq");
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            CheckMethodCall(node);
            return base.VisitMethodCall(node);
        }

        private void CheckMethodCall(MethodCallExpression node)
        {
            var usesEntityAsParameter = node.Arguments.Any(arg =>
            {
                if (!(arg is ParameterExpression param))
                    return false;

                if (param.Type == _entitiesType)
                    return true;

                return false;
            });

            if (!usesEntityAsParameter)
                return;

            UsesOnlyKeyProperties = false;
            UsesOnlyCachedProperties = false;
        }

        private void CheckCangetFromCache(MemberInfo member)
        {
            if (member.DeclaringType != _entitiesType)
                return;

            if (member == _keyProperty)
                return;

            UsesOnlyKeyProperties = false;

            if (member.GetCustomAttribute(typeof(Attributes.CachedAttribute)) == null)
            {
                UsesOnlyCachedProperties = false;
                return;
            }

            // cached property. check it for IEnumerables
            var memberType = GetUnderlyingType(member);

            if (typeof(IEnumerable).IsAssignableFrom(memberType) && memberType != typeof(string))
                UsesCachedEnumerables = true;
        }

        private static Type GetUnderlyingType(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                default:
                    throw new NotSupportedException($"Not supported member type {member.MemberType}");
            }
        }
    }
}
