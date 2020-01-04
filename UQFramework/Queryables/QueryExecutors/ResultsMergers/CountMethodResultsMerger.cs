using System;
using System.Collections.Generic;
using System.Text;

namespace UQFramework.Queryables.QueryExecutors.ResultsMergers
{
    class CountMethodResultsMerger : IResultsMerger
    {
        public object Merge(object result1, object result2)
        {
            if (result1 is long || result2 is long)
                return (long)result1 + (long)result2;

            return (int)result1 + (int)result2;
        }
    }
}
