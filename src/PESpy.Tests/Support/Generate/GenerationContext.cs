using System.Collections;
using System.Collections.Generic;

namespace PESpy.Tests
{
    class GenerationContext : IEnumerable<StructBuilder>
    {
        private Dictionary<string, StructBuilder> structs = new();

        public StructBuilder Struct(string name, string help = null)
        {
            var @struct = new StructBuilder(name, help, this);

            structs.Add(name, @struct);

            return @struct;
        }

        internal StructBuilder GetStruct(string name) => structs[name];

        public IEnumerator<StructBuilder> GetEnumerator() => structs.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
