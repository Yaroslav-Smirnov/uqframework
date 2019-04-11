using System;
using System.Linq.Expressions;
using System.Reflection;

namespace UQFramework.Queryables.ExpressionAnalysis
{
    internal class FilterAnalizer
    {
        public FilterAnalisysResult Process(MethodCallExpression filterExpression, Type entitiesType, PropertyInfo keyProperty)
        {
            if (filterExpression == null || filterExpression.Arguments.Count < 2)
                return new FilterAnalisysResult
                {
                    IsEmpty = true
                };

            var lambdaExpression = (LambdaExpression)((UnaryExpression)filterExpression.Arguments[1]).Operand;

            // Send the lambda expression through the partial evaluator.
            lambdaExpression = (LambdaExpression)Evaluator.PartialEval(lambdaExpression);

            // Get the identifiers 
            var identifiersFinder = new IdentifiersFinder(lambdaExpression.Body, entitiesType, keyProperty);
            identifiersFinder.Process();

            return new FilterAnalisysResult
            {
                IdentifiersRangeFound = identifiersFinder.IdenfitiersRangeDetected,
                Identifiers = identifiersFinder.Identifiers,
                IsContainsExpression = identifiersFinder.IsContainsExpression
            };
        }
    }
}
