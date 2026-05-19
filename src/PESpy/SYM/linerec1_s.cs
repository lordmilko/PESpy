using System;
using PESpy.View;

namespace PESpy.SYM
{
    //linerec1_s
    //SYM is too obscure, so I think it's better to use the real name

    /// <summary>
    /// Special line record - 16 bit (<see cref="linedef_s.ld_itype"/> == 1)
    /// </summary>
    [Source(SourceKind.mapsym_h)]
    public readonly struct linerec1_s : IViewableValue
    {
        private const int lr1_codeoffsetOffset = 0;
        private const int lr1_linenumberOffset = 2;

        /// <summary>
        /// start offset for this linenumber
        /// </summary>
        public ushort lr1_codeoffset => chunk.PeekUInt16(lr1_codeoffsetOffset);

        /// <summary>
        /// linenumber
        /// </summary>
        public ushort lr1_linenumber => chunk.PeekUInt16(lr1_linenumberOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //lr1_codeoffset
            sizeof(short); //lr1_linenumber

        private readonly MemoryChunk chunk;

        internal linerec1_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.linerec1_s, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(lr1_codeoffset), lr1_codeoffsetOffset, lr1_codeoffset);
                    break;

                case 1:
                    structWriter.WriteField(nameof(lr1_linenumber), lr1_linenumberOffset, lr1_linenumber);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    };
}
