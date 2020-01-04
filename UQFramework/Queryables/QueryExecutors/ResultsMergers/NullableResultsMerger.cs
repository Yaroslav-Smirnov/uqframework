using System;
using System.Collections.Generic;
using System.Text;

namespace UQFramework.Queryables.QueryExecutors.ResultsMergers
{
    class NullableResultsMerger : IResultsMerger
    {
        public object Merge(object result1, object result2)
        {
            return result1 ?? result2;
        }
    }
}
