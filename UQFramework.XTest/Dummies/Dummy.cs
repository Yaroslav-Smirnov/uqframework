using UQFramework.Attributes;

namespace UQFramework.XTest.Dummies
{
    internal class Dummy
    {
        [Identifier]
        public string Id { get; set; }

        [Cached]
        public string Name { get; set; }

        public int Number { get; set; }
    }
}
