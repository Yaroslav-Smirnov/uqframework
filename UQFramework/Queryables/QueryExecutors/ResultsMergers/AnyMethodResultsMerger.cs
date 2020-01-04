﻿using System;
using System.Collections.Generic;
using System.Text;

namespace UQFramework.Queryables.QueryExecutors.ResultsMergers
{
    class AnyMethodResultsMerger : IResultsMerger
    {
        public object Merge(object result1, object result2)
        {
            return (bool)result1 || (bool)result2;
        }
    }
}
