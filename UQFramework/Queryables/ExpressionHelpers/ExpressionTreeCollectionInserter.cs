using System.Linq;
using System.Linq.Expressions;

namespace UQFramework.Queryables.ExpressionHelpers
{
    internal class ExpressionTreeCollectionInserter<T> : ExpressionVisitor
    {
        private readonly object _queryableItems;

        internal ExpressionTreeCollectionInserter(object queryableItems)
        {
            _queryableItems = queryableItems;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            // Replace the constant QueryableTerraServerData arg with the queryable T collection. 
            if (c.Type == typeof(QueryableData<T>) || typeof(QueryableData<T>).IsAssignableFrom(c.Type))
                //TODO: maybe need a more strong type here (like UQColletion<>) ?
                return Expression.Constant(_queryableItems);
            else
                return c;
        }
    }
}
