using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UQFramework.Queryables.ExpressionAnalysis;
using UQFramework.Queryables.ExpressionHelpers;

namespace UQFramework.Queryables.QueryExecutors
{
    internal abstract class QueryExecutorBase<T>
    {
        protected readonly ExpressionInfo<T> _expressionInfo;
        protected readonly Func<T, bool> _predicate;
        internal QueryExecutorBase(ExpressionInfo<T> expressionInfo)
        {
            _expressionInfo = expressionInfo ?? throw new ArgumentNullException(nameof(expressionInfo));
            _predicate = _expressionInfo?.Filter?.Compile();
        }

        internal object Execute(Expression expression, bool isEnumerable)
        {
            // get identifiers
            var identifiers = GetIdentifiersToRequest();

            // get entities 
            var entities = GetEntities(identifiers);

            // modify expression and get the result;
            var queryableEntities = entities.AsQueryable();
            var newExpressionTree = ModifyExpression(expression, queryableEntities);

            if (isEnumerable)
                return queryableEntities.Provider.CreateQuery(newExpressionTree);
            else
                return queryableEntities.Provider.Execute(newExpressionTree);
        }

        protected abstract Expression ModifyExpression(Expression expression, IQueryable queryableEntities);

        protected abstract IEnumerable GetEntities(IEnumerable<string> identifiers);

        protected abstract IEnumerable<string> GetIdentifiersToRequest();

        protected static Expression ModifyExpressionAndReturnResult(Expression expression, IQueryable queryableEntities)
        {
            var treeModifier = new ExpressionTreeCollectionInserter<T>(queryableEntities);
            return treeModifier.Visit(expression);
        }

        protected static Expression ModifyExpressionAndReturnResult(Expression expression, IQueryable queryableEntities, MethodCallExpression expressionToReplace, MethodInfo replacingMethod)
        {
            var replacingExpression = GetReplacingExpression(expressionToReplace, queryableEntities, replacingMethod);

            var treeModifier = new ExpressionReplacer(expressionToReplace, replacingExpression);
            return treeModifier.Visit(expression);
        }

        private static Expression GetReplacingExpression(MethodCallExpression expressionToReplace, IQueryable queryableEntities, MethodInfo replacingMethod)
        {
            if (replacingMethod == null)
                return Expression.Constant(queryableEntities);

            var paramsCount = replacingMethod.GetParameters().Count();

            if (paramsCount == 1)
                return Expression.Call(null, replacingMethod, Expression.Constant(queryableEntities));

            var arguments = expressionToReplace.Arguments.Skip(1).Prepend(Expression.Constant(queryableEntities))
                .Take(paramsCount).ToArray();

            return Expression.Call(null, replacingMethod, arguments);


        }
    }
}
