using System.Collections;
using System.Collections.Generic;

namespace PESpy
{
    /// <summary>
    /// Provides lazy access to the strings contained in the #Strings heap
    /// </summary>
    public class StringHeap : IEnumerable<RawValue<string>> //Massively reduces memory usage
    {
        /// <summary>
        /// Gets the size of this heap in bytes.
        /// </summary>
        private int Size { get; }

        public int Offset { get; }

        private FileReader reader;

        internal StringHeap(ref FileReader reader, int size)
        {
            Offset = (int) reader.Position;
            Size = size;
            this.reader = reader;
        }

        internal RawValue<string> ReadString(int offset)
        {
            reader.Enter();

            try
            {
                //From II.24.2.3:

                /* The stream of bytes pointed to by a “#Strings” header is the physical representation of the logical string
                 * heap. The physical heap can contain garbage, that is, it can contain parts that are unreachable from any
                 * of the tables, but parts that are reachable from a table shall contain a valid null-terminated UTF8 string.
                 * When the #String heap is present, the first entry is always the empty string (i.e., \0). */

                //The default UTF8Encoding (in Encoding.UTF8) doesn't throw on invalid bytes. This is important, because
                //the #Strings heap is allowed to contain garbage

                reader.Seek(offset);

                var str = reader.ReadUTF8NullTerminatedString();

                return new RawValue<string>(offset, str);
            }
            finally
            {
                reader.Exit();
            }
        }

        public IEnumerator<RawValue<string>> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator : IEnumerator<RawValue<string>>
        {
            public RawValue<string> Current { get; private set; }

            object IEnumerator.Current => Current;

            private StringHeap stringHeap;
            private int currentOffset;
            private int endOffset;

            internal Enumerator(StringHeap stringHeap)
            {
                this.stringHeap = stringHeap;
                currentOffset = stringHeap.Offset;
                endOffset = currentOffset + stringHeap.Size;
                Current = default;
            }

            public bool MoveNext()
            {
                if (currentOffset >= endOffset)
                    return false;

                Current = stringHeap.ReadString(currentOffset);
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
