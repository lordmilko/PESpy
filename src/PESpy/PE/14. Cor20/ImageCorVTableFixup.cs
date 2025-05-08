using ClrDebug;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_COR_VTABLEFIXUP"/> structure.
    /// </summary>
    public readonly struct ImageCorVTableFixup : IValue
    {
#if PEFAST
        public int RVA => chunk.PeekInt32(0);

        public short Count => chunk.PeekInt16(4);

        public COR_VTABLE Type => (COR_VTABLE) chunk.PeekUInt16(6);

        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public int RVA { get; }

        public short Count { get; }

        public COR_VTABLE Type { get; }

        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(int) + //RVA
            sizeof(short) + //Count
            sizeof(short); //Type

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageCorVTableFixup(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ImageCorVTableFixup(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            RVA = reader.ReadInt32();
            Count = reader.ReadInt16();
            Type = (COR_VTABLE) reader.ReadInt16();
        }
#endif
    }
}
