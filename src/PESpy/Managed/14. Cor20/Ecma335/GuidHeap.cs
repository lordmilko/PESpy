using System;
using System.Collections;
using System.Collections.Generic;

namespace PESpy
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

        public int Offset { get; }

        private FileReader reader;

        internal GuidHeap(ref FileReader reader, int size)
        {
            Offset = (int) reader.Position;
            Size = size;
            Count = size / 16;
            this.reader = reader;
        }

        public RawValue<Guid> this[int index]
        {
            get
            {
                if (index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                reader.Enter();

                try
                {
                    var offset = Offset + 16 * index;
                    reader.Seek(offset);

                    var value = reader.ReadGuid();

                    return new RawValue<Guid>(offset, value);
                }
                finally
                {
                    reader.Exit();
                }
            }
        }

        public IEnumerator<RawValue<Guid>> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator : IEnumerator<RawValue<Guid>>
        {
            public RawValue<Guid> Current { get; private set; }

            object IEnumerator.Current => Current;

            private GuidHeap guidHeap;
            private int index;

            internal Enumerator(GuidHeap guidHeap)
            {
                this.guidHeap = guidHeap;
                index = 0;
                Current = default;
            }

            public bool MoveNext()
            {
                if (index >= guidHeap.Count)
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
