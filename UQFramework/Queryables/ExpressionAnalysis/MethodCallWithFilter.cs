using System;
using System.Linq.Expressions;
using System.Reflection;

namespace UQFramework.Queryables.ExpressionAnalysis
{
    internal class MethodCallWithFilter<T>
    {
        internal MethodCallWithFilter(MethodInfo method, Expression<Func<T, bool>> filter)
        {
            Method = method;
            Filter = filter;
        }
        internal MethodInfo Method { get; }

        internal Expression<Func<T, bool>> Filter { get; }
    }
}
