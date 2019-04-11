using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace UQFramework.Queryables
{
    public class QueryableData<T> : IOrderedQueryable<T>
    {
        // parameterless constructor to use in subclasses
        protected QueryableData()
        {
            Expression = Expression.Constant(this);
        }

        public QueryableData(IQueryProvider provider)
        {
            Expression = Expression.Constant(this);
            Provider = provider;
        }
        public QueryableData(IQueryProvider provider, Expression expression)
        {
            Expression = expression;
            Provider = provider;
        }
        public Expression Expression { get; }

        public Type ElementType => typeof(T);

        public IQueryProvider Provider { get; protected set; }

        public IEnumerator<T> GetEnumerator() => 
            (Provider.Execute<IEnumerable<T>>(Expression)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            (Provider.Execute<IEnumerable>(Expression)).GetEnumerator();
    }
}
