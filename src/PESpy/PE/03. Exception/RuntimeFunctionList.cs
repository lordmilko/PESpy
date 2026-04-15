using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

namespace PESpy
{
    internal class RuntimeFunctionListDebugView
    {
        private RuntimeFunctionList runtimeFunctionList;

        public RuntimeFunctionListDebugView(RuntimeFunctionList runtimeFunctionList)
        {
            this.runtimeFunctionList = runtimeFunctionList;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public RuntimeFunction[] Items => runtimeFunctionList.ToArray();
    }

    /// <summary>
    /// Provides access to <see cref="RuntimeFunction"/> instances without allocating an array.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    [DebuggerTypeProxy(typeof(RuntimeFunctionListDebugView))]
    public class RuntimeFunctionList : IEnumerable<RuntimeFunction>, ILightweightList<RuntimeFunctionList.Enumerator, RuntimeFunction> //Must be a class to denote that the ExceptionTable is missing
    {
        private string DebuggerDisplay() => chunk.block == null ? "null" : $"Count = {Count}";

        private readonly MemoryChunk chunk;

        public int Count { get; }

        internal RuntimeFunctionList(int count, in MemoryChunk chunk)
        {
            Count = count;
            this.chunk = chunk;
        }

        public RuntimeFunction this[int index]
        {
            get
            {
                if (chunk.block == null)
                    throw new NullReferenceException();

                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();

                var value = new RuntimeFunction(chunk.Slice(index * RuntimeFunction.StructSize));

                return value;
            }
        }

        public unsafe bool TryFindEntry(long rva, out RuntimeFunction runtimeFunction)
        {
            var span = new Span<RUNTIME_FUNCTION>(chunk.Pointer, Count);

            var lo = 0;
            var hi = Count - 1;

            //Based on the logic employed by DbgHelp
            while (hi >= lo)
            {
                var mid = (lo + hi) >> 1;

                ref var entry = ref span[mid];

                if (rva < entry.BeginAddress)
                    hi = mid - 1;
                else if (rva >= entry.EndAddress)
                    lo = mid + 1;
                else
                {
                    runtimeFunction = new RuntimeFunction(chunk.Slice(mid * RuntimeFunction.StructSize));
                    return true;
                }
            }

            runtimeFunction = default;
            return false;
        }

        public override bool Equals(object obj) => this == obj;

        public override int GetHashCode() => chunk.block.GetHashCode();

        public Enumerator GetEnumerator() => new Enumerator(Count, chunk);

        IEnumerator<RuntimeFunction> IEnumerable<RuntimeFunction>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<RuntimeFunction>
        {
            private readonly MemoryChunk chunk;
            private int index;
            private readonly int count;

            internal Enumerator(int count, in MemoryChunk chunk)
            {
                this.chunk = chunk;
                this.count = count;
                index = default;

                Current = default;
            }

            public RuntimeFunction Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (index < count)
                {
                    Current = new RuntimeFunction(chunk.Slice(index * RuntimeFunction.StructSize));
                    index++;
                    return true;
                }

                Current = default;
                return false;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
