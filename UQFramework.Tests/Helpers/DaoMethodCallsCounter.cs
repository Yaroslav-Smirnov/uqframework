using System.Threading;

namespace UQFramework.Tests
{
    internal class DaoMethodCallsCounter
    {
        private int _entityCallsCount = 0;
        private int _getIdentifiersCallsCount = 0;

        public int EntityCallsCount => _entityCallsCount;

        public int GetIdentifiersCallsCount => _getIdentifiersCallsCount;

        public void GetEntityCalled() => Interlocked.Increment(ref _entityCallsCount);

        public void GetIdentifiersCalled() => Interlocked.Increment(ref _getIdentifiersCallsCount);
    }
}
