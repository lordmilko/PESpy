using System;
using System.Collections.Generic;
using System.Linq;

namespace PESpy.Tests
{
    class StructBuilder
    {
        public string Name { get; }

        public int Size => Fields.Sum(f => f.Size);

        public List<FieldBuilder> Fields { get; } = new List<FieldBuilder>();

        private GenerationContext ctx;

        public StructBuilder(string name, string help, GenerationContext ctx)
        {
            Name = name;
            this.ctx = ctx;
        }

        public StructBuilder Field(
            string name,
            Type type,

            int numElems = -1,
            Type va = null,
            bool rva = false,
            bool pointer = false,
            bool x86Only = false,
            int nullPaddedUTF8 = -1,
            Type enumImpl = null,
            string help = null,
            bool eager = false) //Specifies that the field should be eagerly loaded in the ctor, and that the property should be a readonly field that acn be ref-returned
        {
            var field = new FieldBuilder(name, type, ctx, numElems, va, rva, pointer, x86Only, nullPaddedUTF8, enumImpl, eager);

            Fields.Add(field);

            return this;
        }
    }
}
