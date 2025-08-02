namespace PESpy
{
    public static partial class Bundle
    {
        //file_entry_fixed_t
        public readonly struct FileEntryFixed : IValue
        {
            public long Offset => chunk.PeekInt64(0);

            public long Size => chunk.PeekInt64(8);

            public long CompressedSize => hasCompressedSize ? chunk.PeekInt64(16) : 0;

            public file_type_t Type => (file_type_t) chunk.PeekByte(hasCompressedSize ? 24 : 16);

            int IValue.Offset => chunk.AbsoluteOffset;

            internal const int FixedStructSize =
                sizeof(long) + //Offset
                sizeof(long) + //Size
                //CompressedSize is optional
                sizeof(byte);  //Type

            private readonly MemoryChunk chunk;
            private readonly bool hasCompressedSize;

            internal FileEntryFixed(in MemoryChunk chunk, bool hasCompressedSize)
            {
                this.chunk = chunk;
                this.hasCompressedSize = hasCompressedSize;
            }
        }
    }    
}
