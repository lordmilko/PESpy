using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    [DebuggerTypeProxy(typeof(RuntimeFunctionListDebugView))]
    public readonly struct RuntimeFunctionList : IEnumerable<RuntimeFunction>
    {
        private string DebuggerDisplay() => chunk.block == null ? "null" : $"Count = {Count}";

        private readonly MemoryChunk chunk;

        public int Count { get; }

        internal RuntimeFunctionList(int count, in MemoryChunk chunk)
        {
            Count = count;
            this.chunk = chunk;
        }

        public static bool operator ==(RuntimeFunctionList value, object? other)
        {
            if (other == null)
                return value.chunk.block == null;

            if (other is RuntimeFunctionList r)
                return value.chunk.block == r.chunk.block;

            return false;
        }

        public static bool operator !=(RuntimeFunctionList value, object? other) => !(value == other);

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
