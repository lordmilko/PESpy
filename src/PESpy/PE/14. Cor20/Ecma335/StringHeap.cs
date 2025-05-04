using System.Collections;
using System.Collections.Generic;

namespace PESpy.Ecma335
{
    /// <summary>
    /// Provides lazy access to the strings contained in the #Strings heap
    /// </summary>
    public class StringHeap : IEnumerable<RawValue<Utf8String>> //Massively reduces memory usage
    {
        /// <summary>
        /// Gets the size of this heap in bytes.
        /// </summary>
        private int Size { get; }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal StringHeap(in MemoryChunk chunk, int size)
        {
            this.chunk = chunk;
            Size = size;
        }

        public RawValue<Utf8String> GetString(StringIndex offset) => GetString(offset.Offset);

        internal RawValue<Utf8String> GetString(int offset)
        {
            //From II.24.2.3:

            /* The stream of bytes pointed to by a “#Strings” header is the physical representation of the logical string
             * heap. The physical heap can contain garbage, that is, it can contain parts that are unreachable from any
             * of the tables, but parts that are reachable from a table shall contain a valid null-terminated UTF8 string.
             * When the #String heap is present, the first entry is always the empty string (i.e., \0). */

            //The default UTF8Encoding (in Encoding.UTF8) doesn't throw on invalid bytes. This is important, because
            //the #Strings heap is allowed to contain garbage

            var str = chunk.PeekUtf8NullTerminatedString(offset);

            return new RawValue<Utf8String>(chunk.AbsoluteOffset + offset, str);
        }

        public IEnumerator<RawValue<Utf8String>> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator : IEnumerator<RawValue<Utf8String>>
        {
            public RawValue<Utf8String> Current { get; private set; }

            object IEnumerator.Current => Current;

            private readonly StringHeap stringHeap;
            private int currentOffset;
            private readonly int endOffset;

            internal Enumerator(StringHeap stringHeap)
            {
                this.stringHeap = stringHeap;
                currentOffset = 0;
                endOffset = stringHeap.Size;
                Current = default;
            }

            public bool MoveNext()
            {
                if (currentOffset >= endOffset)
                    return false;

                Current = stringHeap.GetString(currentOffset);
                currentOffset += Current.Value.Length + 1;
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
