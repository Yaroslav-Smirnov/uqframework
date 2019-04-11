using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UQFramework.Cache;
using UQFramework.DAO;
using UQFramework.Queryables.ExpressionAnalysis;

namespace UQFramework.Queryables.QueryExecutors
{
    internal class DefaultWithCacheQueryExecutor<T> : QueryExecutorBase<T>
    {
        // default executor with cache support
        // it means that the whole query can be executed on cache, but the result need to be obtained 
        // from datastore

        private readonly ISavableData<T> _savable;
        private readonly object _dataAccessObject;
        private readonly ICachedDataProvider<T> _cachedDataProvider;
        private readonly Func<T, string> _keyGetter;

        internal DefaultWithCacheQueryExecutor(ISavableData<T> savable, object dataAccessObject, ICachedDataProvider<T> cachedDataProvider, Func<T, string> keyGetter, ExpressionInfo<T> expressionInfo) : base(expressionInfo)
        {
            _savable = savable;
            _dataAccessObject = dataAccessObject;
            _cachedDataProvider = cachedDataProvider;
            _keyGetter = keyGetter;
        }

        protected override IEnumerable<string> GetIdentifiersToRequest()
        {
            // here we need to get entities from cache, execute the expression on them,
            // get the result and grab only identifiers from the result;

            // read all entites from cache
            var entites = _cachedDataProvider.GetCachedEntities();
            // merge with the all the pending changes
            var mergedEntities = _savable.CombineWithPendingChanges(entites, null);
            var queryableEntities = mergedEntities.AsQueryable();
            // execute expression on the cached entities
            var newExpressionTree = ModifyExpressionAndReturnResult(_expressionInfo.OriginalExpression, queryableEntities);

            object result;
            if (_expressionInfo.IsEnumerableResult)
                result = queryableEntities.Provider.CreateQuery(newExpressionTree);
            else
                result = queryableEntities.Provider.Execute(newExpressionTree);

            if (result == null)
                return Enumerable.Empty<string>();

            if (result is T item)
                return new[] { _keyGetter(item) };

            if (result is IEnumerable<T> items)
                return items.Select(_keyGetter);

            throw new NotSupportedException($"Expression is not supported");
        }

        protected override IEnumerable GetEntities(IEnumerable<string> identifiers)
        {
            var identifiersToRequest = identifiers.Except(_savable.GetAllPendingChangesIdentifiers());
            if (_expressionInfo.InnerMostExpression.Method.Name == nameof(Queryable.FirstOrDefault) ||
                _expressionInfo.InnerMostExpression.Method.Name == nameof(Queryable.LastOrDefault))
            // other methods like that
            {
                // do not care about order, so FirstOrDefault and LastOrDefault are the same
                identifiersToRequest = identifiersToRequest.Take(1);
            }
            var entities = identifiersToRequest.Any() ? GetEntitiesFromDao(identifiersToRequest) : Enumerable.Empty<T>();
            return _savable.CombineWithPendingChanges(entities, _predicate);
        }

        protected override Expression ModifyExpression(Expression expression, IQueryable queryableEntities)
        {
            return ModifyExpressionAndReturnResult(expression, queryableEntities, _expressionInfo.InnerMostExpression, _expressionInfo.InnerMostExpression.Method);
        }

        private IEnumerable<T> GetEntitiesFromDao(IEnumerable<string> identifiers)
        {
            if (_dataAccessObject is IDataSourceBulkReader<T> bulkReader)
                return bulkReader.GetEntities(identifiers);

            if (_dataAccessObject is IDataSourceReader<T> justReader)
                return identifiers.AsParallel().Select(justReader.GetEntity).Where(x => x != null);

            throw new InvalidOperationException($"DataAccessObject {_dataAccessObject.GetType()} for type {typeof(T).FullName} does not implement neither {nameof(IDataSourceBulkReader<T>)} nor {nameof(IDataSourceReader<T>)}");
        }
    }
}
