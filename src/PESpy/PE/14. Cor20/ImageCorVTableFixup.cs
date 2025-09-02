using ClrDebug;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_COR_VTABLEFIXUP"/> structure.
    /// </summary>
    public readonly struct ImageCorVTableFixup : IValue
    {
        /// <summary>
        /// Offset of v-table array in image.
        /// </summary>
        public int RVA => chunk.PeekInt32(0);

        /// <summary>
        /// How many entries at location.
        /// </summary>
        public short Count => chunk.PeekInt16(4);

        /// <summary>
        /// COR_VTABLE_xxx type of entries.
        /// </summary>
        public COR_VTABLE Type => (COR_VTABLE) chunk.PeekUInt16(6);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //RVA
            sizeof(short) + //Count
            sizeof(short); //Type

        private readonly MemoryChunk chunk;

        internal ImageCorVTableFixup(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
