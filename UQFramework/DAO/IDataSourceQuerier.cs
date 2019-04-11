using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace UQFramework.DAO
{
    internal interface IDataSourceQuerier<T>
    {
        IEnumerable<T> GetEntities(Expression<Func<T, bool>> filter);
    }
}
