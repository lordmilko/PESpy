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
        public ushort Flags { get; init; } //Flags
        public ushort Catalog { get; init; }
        public int CatalogOffset { get; init; }
        public int Reserved { get; init; }

        public RawOffset Offset { get; }

        internal const int StructSize =
            sizeof(ushort) + //Flags
            sizeof(ushort) + //Catalog
            sizeof(int) +    //CatalogOffset
            sizeof(int);     //Reserved

        internal ImageLoadConfigCodeIntegrity(ref FileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Flags = reader.ReadUInt16();
            Catalog = reader.ReadUInt16();
            CatalogOffset = reader.ReadInt32();
            Reserved = reader.ReadInt32();
        }

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
