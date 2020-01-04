using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UQFramework.XTest.Dummies
{
    class MethodCallsCounter
    {
        private readonly Dictionary<string, int> _methodCalls = 
            new Dictionary<string, int>();
        public void AddMethodCall([CallerMemberName] string callerMemeberName = "")
        {
            if (!_methodCalls.TryGetValue(callerMemeberName, out var calls))
                _methodCalls[callerMemeberName] = 1;

            _methodCalls[callerMemeberName] = calls + 1;
        }

        public int GetCallsCount(string methodName)
        {
            if (!_methodCalls.TryGetValue(methodName, out var calls))
                return 0;

            return calls;
        }

        public void Reset()
        {
            _methodCalls.Clear();
        }
    }
}
