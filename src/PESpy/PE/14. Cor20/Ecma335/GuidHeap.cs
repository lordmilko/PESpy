using System;
using System.Collections;
using System.Collections.Generic;

namespace PESpy.Ecma335
{
    public class GuidHeap : IEnumerable<RawValue<Guid>> //Massively reduces memory usage
    {
        /// <summary>
        /// Gets the number of records contained in this heap.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the size of this heap in bytes.
        /// </summary>
        private int Size { get; }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal GuidHeap(in MemoryChunk chunk, int size)
        {
            this.chunk = chunk;

            Size = size;
            Count = size / 16;
        }

        public RawValue<Guid> this[GuidIndex index] => this[(int) index];

        internal RawValue<Guid> this[int index]
        {
            get
            {
                //Per ECMA-335 II.22, indexes are 1 based. If an index of 0 is specified, it means
                //that the value is essentially a "null reference"

                if (index == 0)
                    return default;

                if (index > Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                var offset = 16 * (index - 1);

                var value = chunk.PeekGuid(offset);

                return new RawValue<Guid>(chunk.AbsoluteOffset + offset, value);
            }
        }

        internal RawValue<Guid> GetGuid(int offset)
        {
            var value = chunk.PeekGuid(offset);
            return new RawValue<Guid>(chunk.AbsoluteOffset + offset, value);
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<RawValue<Guid>> IEnumerable<RawValue<Guid>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<RawValue<Guid>>
        {
            public RawValue<Guid> Current { get; private set; }

            object IEnumerator.Current => Current;

            private readonly GuidHeap guidHeap;
            private int index;

            internal Enumerator(GuidHeap guidHeap)
            {
                this.guidHeap = guidHeap;
                index = 1;
                Current = default;
            }

            public bool MoveNext()
            {
                if (index > guidHeap.Count)
                    return false;

                Current = guidHeap[index];
                index++;
                return true;
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
