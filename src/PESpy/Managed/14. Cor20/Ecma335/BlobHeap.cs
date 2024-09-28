using System.Collections;
using System.Collections.Generic;

namespace PESpy
{
    public class BlobHeap : IEnumerable<BlobEntry> //Massively reduces memory usage
    {
        /// <summary>
        /// Gets the size of this heap in bytes.
        /// </summary>
        private int Size { get; }

        public int Offset { get; }

        private FileReader reader;

        internal BlobHeap(ref FileReader reader, int size)
        {
            Offset = (int) reader.Position;
            Size = size;
            this.reader = reader;
        }

        internal BlobEntry ReadBlob(int offset)
        {
            reader.Enter();

            try
            {
                reader.Seek(offset);

                var byteCount = reader.ReadCorCompressedInteger(out var compressedSize);

                var bytes = reader.ReadBytes(byteCount);

                return new BlobEntry(offset, compressedSize, bytes);
            }
            finally
            {
                reader.Exit();
            }
        }

        public IEnumerator<BlobEntry> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator : IEnumerator<BlobEntry>
        {
            public BlobEntry Current { get; private set; }

            object IEnumerator.Current => Current;

            private BlobHeap blobHeap;
            private int currentOffset;
            private int endOffset;

            internal Enumerator(BlobHeap blobHeap)
            {
                this.blobHeap = blobHeap;
                currentOffset = blobHeap.Offset;
                endOffset = currentOffset + blobHeap.Size;
                Current = default;
            }

            public bool MoveNext()
            {
                if (currentOffset >= endOffset)
                    return false;

                Current = blobHeap.ReadBlob(currentOffset);
                currentOffset += Current.CompressedSize.Length + Current.Bytes.Length;
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
