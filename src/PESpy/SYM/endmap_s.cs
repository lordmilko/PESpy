using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.SYM
{
    //endmap_s
    //SYM is too obscure, so I think it's better to use the real name
    [DebuggerDisplay("em_spmap = {em_spmap}, em_ver = {em_ver}, em_rel = {em_rel}")]
    [Source(SourceKind.mapsym_h)]
    public readonly struct endmap_s : IViewableValue
    {
        private const int em_spmapOffset = 0;
        private const int em_relOffset = 2;
        private const int em_verOffset = 3;

        /// <summary>
        /// end of map chain (SEG ptr 0)
        /// </summary>
        public ushort em_spmap => chunk.PeekUInt16(em_spmapOffset);

        /// <summary>
        /// release
        /// </summary>
        public byte em_rel => chunk.PeekByte(em_relOffset);

        /// <summary>
        /// version
        /// </summary>
        public byte em_ver => chunk.PeekByte(em_verOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //em_spmap
            sizeof(byte) + //em_rel
            sizeof(byte); //version

        private readonly MemoryChunk chunk;

        internal endmap_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.endmap_s, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(em_spmap), em_spmapOffset, em_spmap);
                    break;

                case 1:
                    structWriter.WriteField(nameof(em_rel), em_relOffset, em_rel);
                    break;

                case 2:
                    structWriter.WriteField(nameof(em_ver), em_verOffset, em_ver);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
