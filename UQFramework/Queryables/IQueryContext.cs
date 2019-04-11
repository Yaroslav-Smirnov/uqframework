using System.Linq.Expressions;

namespace UQFramework.Queryables
{
    public interface IQueryContext
    {
        object Execute(Expression expression, bool isEnumerable);
    }
}
