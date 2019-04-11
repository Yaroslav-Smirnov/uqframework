using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UQFramework.Cache;
using UQFramework.Queryables.ExpressionAnalysis;

namespace UQFramework.Queryables.QueryExecutors
{
    // used if the whole expression can be processed based on identifiers only
    internal class IdentifiersBasedQueryExecutor<T> : QueryExecutorBase<T>
    {
        private readonly ISavableData<T> _savable;
        private readonly ICachedDataProvider<T> _cachedDataProvider;
        internal IdentifiersBasedQueryExecutor(ISavableData<T> savable, ICachedDataProvider<T> cachedDataProvider, ExpressionInfo<T> expressionInfo)
            : base(expressionInfo)
        {
            _savable = savable;
            _cachedDataProvider = cachedDataProvider;
        }

        protected override IEnumerable<string> GetIdentifiersToRequest()
        {
            var filterInfo = _expressionInfo.FilterInfo;

            if (filterInfo.IdentifiersRangeFound && filterInfo.IsContainsExpression)
                return _cachedDataProvider.GetIdentifiersFromCache(filterInfo.Identifiers);

            return _cachedDataProvider.GetIdentifiersFromCache(_predicate);
        }

        protected override IEnumerable GetEntities(IEnumerable<string> identifiers)
        {
            var method = _expressionInfo.InnerMostExpression.Method;

            if (!IsNonReplacableMethod(method))
                return _savable.CombineIdentifiersRangeWithPendingChanges(identifiers, _predicate);

            // non - replacable methods, we need entities
            var identifiersToRequest = identifiers.Except(_savable.GetAllPendingChangesIdentifiers());
            // assume we can just get if from the cache directly for no other properties except key are used.
            var entities = identifiersToRequest.Any() ? _cachedDataProvider.GetCachedEntities(identifiersToRequest) : Enumerable.Empty<T>();

            //var entities = identifiersToRequest.Any() ? _cachedDataProvider.GetEntitiesBasedOnCache(identifiersToRequest) : Enumerable.Empty<T>();

            return _savable.CombineWithPendingChanges(entities, _predicate);
        }

        protected override Expression ModifyExpression(Expression expressionToModify, IQueryable queryableEntities)
        {
            var method = _expressionInfo.InnerMostExpression.Method;

            if(method.Name == nameof(Queryable.Where) && _expressionInfo.FilterInfo.IsContainsExpression)
                return ModifyExpressionAndReturnResult(expressionToModify, queryableEntities, _expressionInfo.InnerMostExpression, null);

            var replacingMethod = ModifyMethodToUseIdentifiers(method);

            if (replacingMethod == null)
                return ModifyExpressionAndReturnResult(expressionToModify, queryableEntities);

            return ModifyExpressionAndReturnResult(expressionToModify, queryableEntities, _expressionInfo.InnerMostExpression, replacingMethod);
        }

        private static bool IsNonReplacableMethod(MethodInfo method)
        {
            return method.Name == nameof(Queryable.Select) ||
                    method.Name == nameof(Queryable.Where) ||
                    method.Name == nameof(Queryable.GroupBy);
        }

        private static MethodInfo ModifyMethodToUseIdentifiers(MethodInfo originalMethod)
        {
            if (IsNonReplacableMethod(originalMethod))
                return null;

            var replacementMethod = GetReplacementMethod(originalMethod);

            if (!replacementMethod.IsGenericMethodDefinition)
                throw new InvalidOperationException("Method must be generic");


            return replacementMethod.MakeGenericMethod(typeof(string));
        }

        private static MethodInfo GetReplacementMethod(MethodInfo originalMethod)
        {
            var paramsCount = 1;

            var methodInfo = typeof(Enumerable).GetMethods()
                                .FirstOrDefault(m => m.Name == originalMethod.Name && m.GetParameters().Count() == paramsCount);

            if (methodInfo == null)
                throw new NotSupportedException($"Method {originalMethod.Name} is not supported");

            return methodInfo;
        }
    }
}
