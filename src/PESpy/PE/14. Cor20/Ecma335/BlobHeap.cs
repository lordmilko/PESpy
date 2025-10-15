using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PESpy.Ecma335
{
    public class BlobHeap : IEnumerable<BlobEntry> //Massively reduces memory usage
    {
        /// <summary>
        /// Gets the size of this heap in bytes.
        /// </summary>
        private int Size { get; }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal BlobHeap(in MemoryChunk chunk, int size)
        {
            this.chunk = chunk;
            Size = size;
        }

        public BlobEntry GetBlob(BlobIndex index) => GetBlob(index.Offset);

        internal unsafe BlobEntry GetBlob(int offset)
        {
            var byteCount = chunk.PeekCorCompressedInteger(offset, out var bytesRead);

            return new BlobEntry(chunk.AbsoluteOffset + offset, chunk.Pointer + offset, bytesRead, byteCount);
        }

        public string GetDocumentName(DocumentNameBlobIndex index) => GetDocumentName(index.Offset);

        internal string GetDocumentName(int offset)
        {
            var byteCount = chunk.PeekCorCompressedInteger(offset, out var bytesRead);
            var end = offset + byteCount + bytesRead; //byteCount is the number of bytes to read from the primary stream. i.e. the number of bytes that the separator and all partOffsets take up

            var read = offset + bytesRead;

            var separator = chunk.PeekByte(read);
            read++;

            using var builder = new ValueStringBuilder();

            var isFirst = true;

            /* If you have a string C:\foo\bar\baz.cs, each component ("foo", "bar") is stored separately, so that when you have multiple paths under a given path you only need to store "foo" and "bar" once
             *
             * e.g. suppose we have C:\TestApp\Program.cs, and the start offset is 23
             *
             * 23: the byteCount (which is 4, which encompasses 1 byte). Therefore bytes 24-27 (inclusive) contain the data that byteCount refers to
             * 24: the separator \
             * 25: the offset of C:
             * 26: the offset of TestApp
             * 27: the offset of Program.cs
             */

            //If you have a string C:\foo\bar\baz.cs, each component ("foo", "bar") is stored separately, so that when you have multiple paths under a given path you only need to store "foo" and "bar" once
            //e.g. suppose offset is
            while (read < end)
            {
                var partOffset = chunk.PeekCorCompressedInteger(read, out var partOffsetBytesRead);
                read += partOffsetBytesRead;

                var partByteCount = chunk.PeekCorCompressedInteger(partOffset, out var partByteCountBytesRead);

                var str = chunk.PeekUtf8FixedLength(partOffset + partByteCountBytesRead, partByteCount);

                if (!isFirst)
                    builder.Append((char) separator);
                else
                    isFirst = false;

                builder.Append(str.ToString());

            }

            return builder.ToString();
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<BlobEntry> IEnumerable<BlobEntry>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<BlobEntry>
        {
            public BlobEntry Current { get; private set; }

            object IEnumerator.Current => Current;

            private readonly BlobHeap blobHeap;
            private int currentOffset;
            private readonly int endOffset;

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

                Current = blobHeap.GetBlob(currentOffset);
                currentOffset += Current.CompressedSize.Length + Current.Value.Length;
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
