namespace PESpy
{
    public static partial class Bundle
    {
        //file_entry_fixed_t
        public readonly struct FileEntryFixed : IValue
        {
#if PEFAST
            public long Offset => chunk.PeekInt64(0);

            public long Size => chunk.PeekInt64(8);

            public long CompressedSize => hasCompressedSize ? chunk.PeekInt64(16) : 0;

            public file_type_t Type => (file_type_t) chunk.PeekByte(hasCompressedSize ? 24 : 16);

            int IValue.Offset => chunk.AbsoluteOffset;
#else
            public long Offset { get; }

            public long Size { get; }

            public long CompressedSize { get; }

            public file_type_t Type { get; }

            int IValue.Offset => throw new System.NotImplementedException();
#endif

#if PEFAST
            private readonly MemoryChunk chunk;
            private readonly bool hasCompressedSize;

            internal FileEntryFixed(in MemoryChunk chunk, int majorVersion)
            {
                this.chunk = chunk;
                hasCompressedSize = majorVersion >= 6;
            }
#else
            internal FileEntryFixed(IFileReader reader, int majorVersion)
            {
                Offset = reader.ReadInt64();
                Size = reader.ReadInt64();

                CompressedSize = majorVersion >= 6 ? reader.ReadInt64() : 0;

                Type = (file_type_t) reader.ReadByte();
            }
#endif
        }
    }    
}
