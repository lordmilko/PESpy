using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_COR_VTABLEFIXUP"/> structure.
    /// </summary>
    public readonly struct ImageCorVTableFixup : IValue, IViewable
    {
        private const int RVAOffset = 0;
        private const int CountOffset = 4;
        private const int TypeOffset = 6;

        /// <summary>
        /// Offset of v-table array in image.
        /// </summary>
        public int RVA => chunk.PeekInt32(RVAOffset);

        /// <summary>
        /// How many entries at location.
        /// </summary>
        public short Count => chunk.PeekInt16(CountOffset);

        /// <summary>
        /// COR_VTABLE_xxx type of entries.
        /// </summary>
        public COR_VTABLE Type => (COR_VTABLE) chunk.PeekUInt16(TypeOffset);

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_COR_VTABLEFIXUP, this, ViewKind.ImageCorVTableFixup, StructSize);

        int IViewable.NumChildren => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(RVA), RVAOffset, RVA);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Count), CountOffset, Count);
                    break;

                case 2:
                    structWriter.WriteField(nameof(Type), TypeOffset, Type, sizeof(short));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
