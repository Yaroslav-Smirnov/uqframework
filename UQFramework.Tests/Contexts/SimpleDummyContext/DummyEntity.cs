using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UQFramework.Attributes;

namespace UQFramework.Tests
{
    internal class DummyEntity
    {
        //[Key]
        [Identifier]
        public string Key { get; set; }

        [Cached]
        [Key] // fake to test identifier attribute
        public string Name { get; set; }

        [Cached]
        public string SomeData { get; set; }

        public string NonCachedField { get; set; }

        [Cached]
        [Key] // fake to test identifier attribute
        public DateTime Created { get; set; }

        [Cached]
        public IEnumerable<string> ListData { get; set; }
    }

}
