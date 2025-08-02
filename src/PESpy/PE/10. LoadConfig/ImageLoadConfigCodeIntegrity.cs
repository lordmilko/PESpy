using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_LOAD_CONFIG_CODE_INTEGRITY"/> structure.
    /// </summary>
    public readonly struct ImageLoadConfigCodeIntegrity : IValue, IViewable
    {
        public ushort Flags => chunk.PeekUInt16(0);
        public ushort Catalog => chunk.PeekUInt16(2);
        public int CatalogOffset => chunk.PeekInt32(4);
        public int Reserved => chunk.PeekInt32(8);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //Flags
            sizeof(ushort) + //Catalog
            sizeof(int) +    //CatalogOffset
            sizeof(int);     //Reserved

        private readonly MemoryChunk chunk;

        internal ImageLoadConfigCodeIntegrity(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_LOAD_CONFIG_CODE_INTEGRITY, this, ViewKind.ImageLoadConfigCodeIntegrity, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Flags), Flags);
            s.WriteField(nameof(Catalog), Catalog);
            s.WriteField(nameof(CatalogOffset), CatalogOffset);
            s.WriteField(nameof(Reserved), Reserved);

            return s.ToArray();
        }
    }
}
