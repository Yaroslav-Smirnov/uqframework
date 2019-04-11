using System.Linq.Expressions;

namespace UQFramework.Queryables.ExpressionHelpers
{
    internal class ExpressionReplacer : ExpressionVisitor
    {
        private readonly MethodCallExpression _expressionToReplace;
        private readonly Expression _replacingExpression;

        internal ExpressionReplacer(MethodCallExpression expressionToReplace, Expression replacingExpression)
        {
            _replacingExpression = replacingExpression;
            _expressionToReplace = expressionToReplace;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node != _expressionToReplace)
                return base.VisitMethodCall(node);

            return _replacingExpression;
        }
    }
}
