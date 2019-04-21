using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UQFramework.Attributes;

namespace UQFrameWork.Demo
{
    public class Entity
    {
        [Key]
        public string Identifier { get; set; }

        [Cached]
        public string Name { get; set; }

        public string Property1 { get; set; }

        [Cached]
        public IEnumerable<string> Collection { get; set; }
    }
}
