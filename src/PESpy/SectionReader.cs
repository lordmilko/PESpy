using System.IO;
using PESpy.PDB;

namespace PESpy
{
    internal struct SectionReader
    {
        private PdbPageStream pageStream;
        private int startByteOffset;
        private long length;

        private IFileReader fileReader;

        public long FilePosition => fileReader.Position;

        public long SectionPosition => pageStream.ByteOffset - startByteOffset;

        internal SectionReader(Stream stream, object readerLock, long fileReaderStart, int length, byte[] valueBuffer)
        {
            this.pageStream = (PdbPageStream) stream;
            fileReader = new StreamFileReader(stream, readerLock, fileReaderStart, valueBuffer);

            startByteOffset = pageStream.ByteOffset;

            this.length = length;
        }

        internal byte ReadByte() => fileReader.ReadByte();

        internal short ReadInt16() => fileReader.ReadInt16();

        internal ushort ReadUInt16() => fileReader.ReadUInt16();

        internal int ReadInt32() => fileReader.ReadInt32();

        internal T[] ReadArray<T>(int count) where T : unmanaged => fileReader.ReadArray<T>(count);

        internal string ReadAnsiNullTerminatedString() => fileReader.ReadAnsiNullTerminatedString();

        internal string ReadUTF8NullTerminatedString() => fileReader.ReadUTF8NullTerminatedString();

        internal byte[] ReadBytes(int count) => fileReader.ReadBytes(count);

        internal void Seek(long offset)
        {
            //We need to seek relative to the position we captured when we created this reader
            pageStream.Seek(offset + startByteOffset, SeekOrigin.Begin);
        }

        internal bool CanRead() => SectionPosition < length;
    }
}
