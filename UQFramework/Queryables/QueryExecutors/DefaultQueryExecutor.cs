using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UQFramework.DAO;
using UQFramework.Queryables.ExpressionAnalysis;

namespace UQFramework.Queryables.QueryExecutors
{
    internal class DefaultQueryExecutor<T> : QueryExecutorBase<T>
    {
        // default provider, no cache.
        private readonly ISavableData<T> _savable;
        private readonly object _dataAccessObject;

        internal DefaultQueryExecutor(ISavableData<T> savable, object dataAccessObject, ExpressionInfo<T> expressionInfo) : base(expressionInfo)
        {
            _savable = savable;
            _dataAccessObject = dataAccessObject;
        }

        protected override IEnumerable<string> GetIdentifiersToRequest()
        {
            if (!_expressionInfo.FilterInfo.IdentifiersRangeFound)
                return null;

            return _expressionInfo.FilterInfo.Identifiers.Except(_savable.GetAllPendingChangesIdentifiers());
        }

        protected override IEnumerable GetEntities(IEnumerable<string> identifiers)
        {
            IEnumerable<T> entities;
            if (identifiers == null)
            {
                entities = GetAllEntitiesFromDao();
            }
            else if (!identifiers.Any())
            {
                entities = Enumerable.Empty<T>();
            }
            else
            {
                var identifiersToRequest = identifiers.Except(_savable.GetAllPendingChangesIdentifiers());
                entities = identifiersToRequest.Any() ? GetEntitiesFromDao(identifiersToRequest) : Enumerable.Empty<T>();
            }

            return _savable.CombineWithPendingChanges(entities, _predicate);
        }

        protected override Expression ModifyExpression(Expression expressionToModify, IQueryable queryableEntities)
        {
            if (_expressionInfo.FilterInfo.IsContainsExpression) // can replace (only identifiers in the contains?)
                return ModifyExpressionAndReturnResult(expressionToModify, queryableEntities, _expressionInfo.InnerMostExpression, _expressionInfo.InnerMostExpression.Method);

            return ModifyExpressionAndReturnResult(expressionToModify, queryableEntities); // no replace

        }

        private IEnumerable<T> GetEntitiesFromDao(IEnumerable<string> identifiers)
        {
            if (_dataAccessObject is IDataSourceBulkReader<T> bulkReader)
                return bulkReader.GetEntities(identifiers);

            if (_dataAccessObject is IDataSourceReader<T> justReader)
                return identifiers.AsParallel().Select(justReader.GetEntity).Where(x => x != null);

            throw new InvalidOperationException($"DataAccessObject {_dataAccessObject.GetType()} for type {typeof(T).FullName} does not implement neither {nameof(IDataSourceBulkReader<T>)} nor {nameof(IDataSourceReader<T>)}");
        }

        private IEnumerable<T> GetAllEntitiesFromDao()
        {
            if (_dataAccessObject is IDataSourceEnumeratorEx<T> dataSourceReaderAll)
                return dataSourceReaderAll.GetAllEntities();

            if (_dataAccessObject is IDataSourceEnumerator<T> dataSourceEnumerator)
            {
                var identifiers = dataSourceEnumerator.GetAllEntitiesIdentifiers();
                return GetEntitiesFromDao(identifiers);
            }

            throw new InvalidOperationException($"DataAccessObject {_dataAccessObject.GetType()} for type {typeof(T).FullName} does not implement {nameof(IDataSourceEnumerator<T>)}");
        }
    }
}
