using System;
using System.Collections.Generic;
using System.Text;
using UQFramework.Attributes;

namespace UQFramework.XTest.Dummies
{
    internal class DummyContext : UQContext
    {
        public DummyContext(int numberOfItems, MethodCallsCounter methodCallsCounter) : base("ds1", new Dictionary<string, object>
                                                                   {
                                                                       ["numberOfItems"] = numberOfItems,
                                                                       ["methodCallsCounter"] = methodCallsCounter
                                                                   })
        {
        }

        [DataAccessObject(typeof(DummyDao))]
        public IUQCollection<Dummy> Dummies { get; private set; }
    }
}
