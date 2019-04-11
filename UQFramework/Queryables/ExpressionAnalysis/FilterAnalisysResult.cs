using System.Collections.Generic;

namespace UQFramework.Queryables.ExpressionAnalysis
{
    internal class FilterAnalisysResult
    {
        internal static FilterAnalisysResult Empty { get; } = new FilterAnalisysResult { IsEmpty = true };
        public bool IsEmpty { get; set; }

        public IEnumerable<string> Identifiers { get; set; }

        public bool IdentifiersRangeFound { get; set; }

        // YSV: should be replaced with some generic replacement
        public bool IsContainsExpression { get; set; }
    }
}
