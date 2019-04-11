using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace UQFramework.Queryables
{
    public class QueryProvider : IQueryProvider
    {
        private readonly IQueryContext _queryContext;
        public QueryProvider(IQueryContext queryContext)
        {
            _queryContext = queryContext;
        }
        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetElementType();
            // YSV: investigate when it is called and what happens
            return (IQueryable)Activator.CreateInstance(typeof(QueryableData<>).MakeGenericType(elementType));
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new QueryableData<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            return _queryContext.Execute(expression, false);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var isEnumerable = typeof(IEnumerable).IsAssignableFrom(typeof(TResult)) && typeof(TResult).IsGenericType;

            return (TResult)_queryContext.Execute(expression, isEnumerable);
        }
    }
}
