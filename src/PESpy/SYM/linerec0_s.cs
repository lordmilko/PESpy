using System;
using PESpy.View;

namespace PESpy.SYM
{
    //linerec0_s
    //SYM is too obscure, so I think it's better to use the real name

    /// <summary>
    /// Normal line record (<see cref="linedef_s.ld_itype"/> == 0)
    /// </summary>
    [Source(SourceKind.mapsym_h)]
    public readonly struct linerec0_s : IViewableValue
    {
        private const int lr0_codeoffsetOffset = 0;
        private const int lr0_fileoffsetOffset = 2;

        /// <summary>
        /// start offset for this linenumber
        /// </summary>
        public ushort lr0_codeoffset => chunk.PeekUInt16(lr0_codeoffsetOffset);

        /// <summary>
        /// file offset for this linenumber
        /// </summary>
        public int lr0_fileoffset => chunk.PeekInt32(lr0_fileoffsetOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //lr0_codeoffset
            sizeof(int); //lr0_fileoffset

        private readonly MemoryChunk chunk;

        internal linerec0_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.linerec0_s, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(lr0_codeoffset), lr0_codeoffsetOffset, lr0_codeoffset);
                    break;

                case 1:
                    structWriter.WriteField(nameof(lr0_fileoffset), lr0_fileoffsetOffset, lr0_codeoffset);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    };
}
