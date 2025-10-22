using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_LOAD_CONFIG_CODE_INTEGRITY"/> structure.
    /// </summary>
    public readonly struct ImageLoadConfigCodeIntegrity : IValue, IViewable
    {
        private const int FlagsOffset = 0;
        private const int _CatalogOffset = 2;
        private const int CatalogOffsetOffset = 4;
        private const int ReservedOffset = 8;

        public ushort Flags => chunk.PeekUInt16(FlagsOffset);
        public ushort Catalog => chunk.PeekUInt16(_CatalogOffset);
        public int CatalogOffset => chunk.PeekInt32(CatalogOffsetOffset);
        public int Reserved => chunk.PeekInt32(ReservedOffset);

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

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Flags), FlagsOffset, Flags);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Catalog), _CatalogOffset, Catalog);
                    break;

                case 2:
                    structWriter.WriteField(nameof(CatalogOffset), CatalogOffsetOffset, CatalogOffset);
                    break;

                case 3:
                    structWriter.WriteField(nameof(Reserved), ReservedOffset, Reserved);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
