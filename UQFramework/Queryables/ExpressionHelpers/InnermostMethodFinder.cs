using System;
using System.Linq;
using System.Linq.Expressions;

namespace UQFramework.Queryables.ExpressionHelpers
{
    internal class InnermostMethodFinder : ExpressionVisitor
    {
        private readonly string _methodName;
        private readonly Type _genericType;

        private MethodCallExpression _lastVisitedNode;

        public InnermostMethodFinder(Type genericType) : this(genericType, null)
        {
        }

        public InnermostMethodFinder(Type genericType, string methodName)
        {
            _genericType = genericType;
            _methodName = methodName;
            InnermostMethodExpression = null;
        }

        internal MethodCallExpression InnermostMethodExpression { get; private set; }

        internal MethodCallExpression InnermostMethodParentExpression { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (_methodName == null && CheckInnerMostMethod(node) || node.Method.Name == _methodName)
            {
                InnermostMethodExpression = node;
                InnermostMethodParentExpression = _lastVisitedNode;
            }

            _lastVisitedNode = node;

            return base.VisitMethodCall(node);
        }

        private bool CheckInnerMostMethod(MethodCallExpression node)
        {
            if (!node.Arguments.Any())
                return false;

            var firstParameterType = node.Arguments[0].Type;

            var expectedType = typeof(QueryableData<>).MakeGenericType(_genericType);

            return expectedType.IsAssignableFrom(firstParameterType);
        }
    }
}
