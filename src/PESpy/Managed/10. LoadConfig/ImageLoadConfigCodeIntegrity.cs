using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_LOAD_CONFIG_CODE_INTEGRITY"/> structure.
    /// </summary>
    public readonly struct ImageLoadConfigCodeIntegrity : IValue, IViewable
    {
#if PEFAST
        public ushort Flags => chunk.PeekUInt16(0);
#else
        public ushort Flags { get; init; } //Flags
#endif
#if PEFAST
        public ushort Catalog => chunk.PeekUInt16(2);
#else
        public ushort Catalog { get; init; }
#endif
#if PEFAST
        public int CatalogOffset => chunk.PeekInt32(4);
#else
        public int CatalogOffset { get; init; }
#endif
#if PEFAST
        public int Reserved => chunk.PeekInt32(8);
#else
        public int Reserved { get; init; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(ushort) + //Flags
            sizeof(ushort) + //Catalog
            sizeof(int) +    //CatalogOffset
            sizeof(int);     //Reserved

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageLoadConfigCodeIntegrity(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ImageLoadConfigCodeIntegrity(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Flags = reader.ReadUInt16();
            Catalog = reader.ReadUInt16();
            CatalogOffset = reader.ReadInt32();
            Reserved = reader.ReadInt32();
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_LOAD_CONFIG_CODE_INTEGRITY), this, ViewKind.ImageLoadConfigCodeIntegrity);

            s.WriteField(nameof(Flags), Flags);
            s.WriteField(nameof(Catalog), Catalog);
            s.WriteField(nameof(CatalogOffset), CatalogOffset);
            s.WriteField(nameof(Reserved), Reserved);
        }
    }
}
