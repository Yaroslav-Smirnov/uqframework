using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UQFramework.Queryables.QueryExecutors.ResultsMergers
{
    // Concatenates two enumreables
    class EnumerableResultsMerger<T> : IResultsMerger
    {
        public object Merge(object result1, object result2)
        {
            var sequence1 = result1 as IEnumerable<T>;
            var sequence2 = result2 as IEnumerable<T>;

            return sequence1.Concat(sequence2);
        }
    }
}
