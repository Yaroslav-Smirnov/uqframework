using System.Linq.Expressions;

namespace UQFramework.Queryables.QueryExecutors
{
    internal interface IQueryExecutor
    {
        object Execute(Expression expression, bool isEnumerable);
    }
}