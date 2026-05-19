using System;
using PESpy.View;

namespace PESpy.SYM
{
    //linerec2_s
    //SYM is too obscure, so I think it's better to use the real name

    /// <summary>
    /// Special line record - 32 bit (<see cref="linedef_s.ld_itype"/> == 2)
    /// </summary>
    [Source(SourceKind.mapsym_h)]
    public readonly struct linerec2_s : IViewableValue
    {
        private const int lr2_codeoffsetOffset = 0;
        private const int lr2_linenumberOffset = 4;

        /// <summary>
        /// start offset for this linenumber
        /// </summary>
        public int lr2_codeoffset => chunk.PeekInt32(lr2_codeoffsetOffset);

        /// <summary>
        /// linenumber
        /// </summary>
        public ushort lr2_linenumber => chunk.PeekUInt16(lr2_linenumberOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //lr2_codeoffset
            sizeof(short); //lr2_linenumber

        private readonly MemoryChunk chunk;

        internal linerec2_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.linerec2_s, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(lr2_codeoffset), lr2_codeoffsetOffset, lr2_codeoffset);
                    break;

                case 1:
                    structWriter.WriteField(nameof(lr2_linenumber), lr2_linenumberOffset, lr2_linenumber);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    };
}
