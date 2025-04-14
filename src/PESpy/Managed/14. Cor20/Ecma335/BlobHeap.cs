using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PESpy
{
    public class BlobHeap : IEnumerable<BlobEntry> //Massively reduces memory usage
    {
        /// <summary>
        /// Gets the size of this heap in bytes.
        /// </summary>
        private int Size { get; }

        public int Offset { get; }

        private IFileReader reader;

        internal BlobHeap(IFileReader reader, int size)
        {
            Offset = (int) reader.Position;
            Size = size;
            this.reader = reader;
        }

        public BlobEntry ReadBlob(int offset)
        {
            reader.Enter();

            try
            {
                reader.Seek(Offset + offset);

                var byteCount = reader.ReadCorCompressedInteger(out var compressedSize);

                var bytes = reader.ReadBytes(byteCount);

                return new BlobEntry(offset, compressedSize, bytes);
            }
            finally
            {
                reader.Exit();
            }
        }

        public string ReadDocumentName(int offset)
        {
            reader.Enter();

            try
            {
                reader.Seek(Offset + offset);

                var byteCount = reader.ReadCorCompressedInteger(out var compressedSize);

                var end = reader.Position + byteCount;

                if (reader.Position >= end)
                    throw new NotImplementedException("Not sure how to handle size of document name going beyond the end of the file reader");

                var separator = reader.ReadByte();

                var builder = new StringBuilder();

                var isFirst = true;

                while (reader.Position < end)
                {
                    var partOffset = reader.ReadCorCompressedInteger(out var partCompressedSize);

                    var oldPosition = reader.Position;

                    reader.Seek(Offset + partOffset);

                    var partByteCount = reader.ReadCorCompressedInteger(out _);

                    var str = reader.ReadNullPaddedUTF8(partByteCount);

                    reader.Seek(oldPosition);

                    if (!isFirst)
                    {
                        builder.Append((char) separator);
                    }
                    else
                        isFirst = false;

                    builder.Append(str);
                }

                return builder.ToString();
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
                currentOffset = 0;
                endOffset = blobHeap.Size;
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
