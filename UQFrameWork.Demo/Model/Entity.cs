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

        [Cached]
        public string Property1 { get; set; }

        [Cached]
        public int Property2 { get; set; }

        [Cached]
        public long Property3 { get; set; }

        [Cached]
        public DateTime Property4 { get; set; }

        [Cached]
        public long Property5 { get; set; }

        [Cached]
        public bool Property6 { get; set; }

        [Cached]
        public double Property7 { get; set; }

        [Cached]
        public double Property8 { get; set; }

        [Cached]
        public short Property9 { get; set; }

        [Cached]
        public uint Property10 { get; set; }

        [Cached]
        public IEnumerable<string> Collection { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsAdded { get; set; }
    }
}
