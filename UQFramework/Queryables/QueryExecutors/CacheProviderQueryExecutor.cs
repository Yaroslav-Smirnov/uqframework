using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UQFramework.DAO;
using UQFramework.Helpers;
using UQFramework.Queryables.ExpressionAnalysis;
using UQFramework.Queryables.ExpressionHelpers;
using UQFramework.Queryables.QueryExecutors.ResultsMergers;

namespace UQFramework.Queryables.QueryExecutors
{
    internal class CacheProviderQueryExecutor<T> : IQueryExecutor
    {
        private readonly ISavableData<T> _savable;
        private readonly IDataSourceCacheProvider<T> _dataSourceCacheProvider;
        private readonly ExpressionInfo<T> _expressionInfo;
        private readonly PropertyInfo _keyPropertyInfo;

        internal CacheProviderQueryExecutor(ISavableData<T> savable, PropertyInfo keyPropertyInfo, IDataSourceCacheProvider<T> dataSourceCacheProvider, ExpressionInfo<T> expressionInfo)
        {
            _savable = savable;
            _keyPropertyInfo = keyPropertyInfo;
            _dataSourceCacheProvider = dataSourceCacheProvider;
            _expressionInfo = expressionInfo;
        }

        public object Execute(Expression expression, bool isEnumerable)
        {
            var identifiers = GetIdentifiersToRequest();

            var queryable = identifiers != null
                ? _dataSourceCacheProvider.GetEntitiesWithCachedPropertiesOnly(identifiers)
                : _dataSourceCacheProvider.GetEntitiesWithCachedPropertiesOnly();

            // exclude pending identifiers
            var pendingItemsIds = _savable.GetAllPendingChangesIdentifiers();

            if (pendingItemsIds != null && pendingItemsIds.Any())
            {
                var exp = GeneralHelper.BuildNotContainsExpression<T>(_keyPropertyInfo, pendingItemsIds);
                queryable = queryable.Where(exp);
            }

            // get result from DAO
            var resultFromDao = ModifyExpressionAndReturnResult(expression, isEnumerable, queryable);

            if (pendingItemsIds == null || !pendingItemsIds.Any())
                return resultFromDao;

            // get result from 
            var queryPendingChanges = _savable.PendingAdd.Union(_savable.PendingUpdate).AsQueryable();
            var resultFromPendingChanges = ModifyExpressionAndReturnResult(expression, isEnumerable, queryPendingChanges);

            var resultsMerger = GetResultsMerger();
            return resultsMerger.Merge(resultFromDao, resultFromPendingChanges);
        }

        private IResultsMerger GetResultsMerger()
        {
            if (_expressionInfo.IsEnumerableResult)
            {
                var type = _expressionInfo.OriginalExpression.Type.GetGenericArguments()[0];
                // !!! OrderBy (facepalm)
                return (IResultsMerger)Activator.CreateInstance(typeof(EnumerableResultsMerger<>).MakeGenericType(type));
            }

            if (!(_expressionInfo.OriginalExpression is MethodCallExpression outmostMethodCall))
                throw new InvalidOperationException("Expression is not a method call (wierd)");

            if(outmostMethodCall.Method.DeclaringType != typeof(Enumerable) &&
                outmostMethodCall.Method.DeclaringType != typeof(Queryable))
                    throw new NotSupportedException();

            switch (outmostMethodCall.Method.Name)
            {
                case nameof(Enumerable.Any):
                    return new AnyMethodResultsMerger();
                case nameof(Enumerable.Count):
                    return new CountMethodResultsMerger();
                case nameof(Enumerable.FirstOrDefault):
                    return new NullableResultsMerger();
                default:
                    throw new NotSupportedException();
            }
        }

        private object ModifyExpressionAndReturnResult(Expression expression, bool isEnumerable, IQueryable<T> queryable)
        {
            var treeModifier = new ExpressionTreeCollectionInserter<T>(queryable);
            var newExpressionTree = treeModifier.Visit(expression);

            if (isEnumerable)
                return queryable.Provider.CreateQuery(newExpressionTree);
            else
                return queryable.Provider.Execute(newExpressionTree);
        }

        private IEnumerable<string> GetIdentifiersToRequest()
        {
            if (!_expressionInfo.FilterInfo.IdentifiersRangeFound)
                return null;

            return _expressionInfo.FilterInfo.Identifiers.Except(_savable.GetAllPendingChangesIdentifiers()).ToList();
        }
    }
}
