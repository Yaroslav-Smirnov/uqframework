using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace UQFramework.Queryables.ExpressionAnalysis
{
    internal class IdentifiersFinder : ExpressionVisitor
    {
        private readonly Expression _expression;
        private readonly Type _entitiesType;
        private readonly PropertyInfo _keyProperty;

        private bool _expressionUsesMemberExpressions = false;
        private bool _thereIsNotOnlyEqualsExpressions = false;

        private bool _expressionUsesBinaryExpressions = false;
        private bool _expressionUsesOnlyKeyProperty = true;

        public IdentifiersFinder(Expression expression, Type entitiesType, PropertyInfo keyProperty)
        {
            _entitiesType = entitiesType ?? throw new ArgumentNullException(nameof(entitiesType));
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
            _keyProperty = keyProperty ?? throw new ArgumentNullException(nameof(keyProperty));
        }

        public void Process()
        {
            Identifiers = new List<string>();

            if (!(_expression is MethodCallExpression method))
            {
                base.Visit(_expression);
                return;
            }

            // if it is Contains expression
            if (!IsEnumerableContainsMethodCall(method, out var valuesExpression))
            {
                base.Visit(_expression);
                return;
            }

            IsContainsExpression = true;
            //ExpressionUsesOnlyKeyProperty = true;

            if (valuesExpression == null || valuesExpression.NodeType != ExpressionType.Constant)
                throw new InvalidOperationException("Could not find the identifiers values.");

            var ce = (ConstantExpression)valuesExpression;

            var identifiers = (IEnumerable<string>)ce.Value;
            // Add each string in the collection to the list of identifiers to obtain data about.

            Identifiers = identifiers.ToList();

        }

        public bool IsContainsExpression { get; private set; }

        public bool IdenfitiersRangeDetected
        {
            get
            {
                if (IsContainsExpression) // IsContainsExpression effectively true is when it contains identifiers
                    return true;

                if (!_expressionUsesBinaryExpressions)
                    return false;

                if (!_expressionUsesMemberExpressions)
                    return false;

                if (_expressionUsesOnlyKeyProperty && !_thereIsNotOnlyEqualsExpressions)
                    return true;

                return false;
            }
        }

        public List<string> Identifiers { get; private set; }

        #region Visits

        protected override Expression VisitMember(MemberExpression node)
        {
            _expressionUsesMemberExpressions = true;
            if (node.Member.DeclaringType == _entitiesType && node.Member != _keyProperty)
            {
                _expressionUsesOnlyKeyProperty = false;
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitBinary(BinaryExpression be)
        {
            _expressionUsesBinaryExpressions = true;

            if (be.NodeType == ExpressionType.Equal)
            {
                if (MemberExpressionHelper.IsMemberEqualsValueExpression(be, _entitiesType, _keyProperty.Name))
                {
                    Identifiers.Add(MemberExpressionHelper.GetValueFromEqualsExpression(be, _entitiesType, _keyProperty.Name));
                    // return be;
                    return base.VisitBinary(be);
                }
                else
                    return base.VisitBinary(be);
            }
            else
            {
                _thereIsNotOnlyEqualsExpressions = true;
                return base.VisitBinary(be);
            }
        }

        private bool IsEnumerableContainsMethodCall(MethodCallExpression m, out Expression valuesExpression)
        {
            valuesExpression = null;

            if (m.Method.Name != "Contains")
                return false;

            if (m.Method.DeclaringType == typeof(string))
                return false;

            if (m.Method.DeclaringType == typeof(List<string>))
            {
                if (!MemberExpressionHelper.IsSpecificMemberExpression(m.Arguments[0], _entitiesType, _keyProperty.Name))
                    return false;

                valuesExpression = m.Object;
                return true;
            }

            if (m.Method.DeclaringType == typeof(Enumerable) || m.Method.DeclaringType == typeof(Queryable))
            {
                if (!MemberExpressionHelper.IsSpecificMemberExpression(m.Arguments[1], _entitiesType, _keyProperty.Name))
                    return false;

                valuesExpression = m.Arguments[0];
                return true;
            }

            return false;
        }

        #endregion
    }
}
