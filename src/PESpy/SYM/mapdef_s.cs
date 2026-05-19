using System;
using PESpy.View;

namespace PESpy.SYM
{
    //mapdef_s
    //SYM is too obscure, so I think it's better to use the real name
    [Source(SourceKind.mapsym_h)]
    public readonly struct mapdef_s : IViewableValue
    {
        private const int md_spmapOffset = 0;
        private const int md_abstypeOffset = 2;
        private const int md_padOffset = 3;
        private const int md_segentryOffset = 4;
        private const int md_cabsOffset = 6;
        private const int md_pabsoffOffset = 8;
        private const int md_csegOffset = 10;
        private const int md_spsegOffset = 12;
        private const int md_cbnamemaxOffset = 14;
        private const int md_cbnameOffset = 15;
        private const int md_achnameOffset = 16;

        /// <summary>
        /// 16 bit SEG ptr to next map (0 if end)
        /// </summary>
        public ushort md_spmap => chunk.PeekUInt16(md_spmapOffset);

        /// <summary>
        /// 8 bit map/abs sym flags
        /// </summary>
        public MSF md_abstype => (MSF) chunk.PeekByte(md_abstypeOffset);

        /// <summary>
        /// 8 bit pad
        /// </summary>
        public byte md_pad => chunk.PeekByte(md_padOffset);

        /// <summary>
        /// 16 bit entry point segment value
        /// </summary>
        public ushort md_segentry => chunk.PeekUInt16(md_segentryOffset);

        /// <summary>
        /// 16 bit count of constants in map
        /// </summary>
        public ushort md_cabs => chunk.PeekUInt16(md_cabsOffset);

        /// <summary>
        /// 16 bit ptr to constant offsets
        /// </summary>
        public ushort md_pabsoff => chunk.PeekUInt16(md_pabsoffOffset);

        /// <summary>
        /// 16 bit count of segments in map
        /// </summary>
        public ushort md_cseg => chunk.PeekUInt16(md_csegOffset);

        /// <summary>
        /// 16 bit SEG ptr to segment chain
        /// </summary>
        public ushort md_spseg => chunk.PeekUInt16(md_spsegOffset);

        /// <summary>
        /// 8 bit maximum symbol name length
        /// </summary>
        public byte md_cbnamemax => chunk.PeekByte(md_cbnamemaxOffset);

        /// <summary>
        /// 8 bit symbol table name length
        /// </summary>
        public byte md_cbname => chunk.PeekByte(md_cbnameOffset);

        /// <summary>
        /// &lt;n&gt; name of symbol table (.sym )
        /// </summary>
        public FixedAnsiString md_achname => chunk.PeekAnsiFixedLength(md_achnameOffset, md_cbname);

        public long Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(short) + //md_spmap
            sizeof(byte) + //md_abstype
            sizeof(byte) + //md_pad
            sizeof(short) + //md_segentry
            sizeof(short) + //md_cabs
            sizeof(short) + //md_pabsoff
            sizeof(short) + //md_cseg
            sizeof(short) + //md_spseg
            sizeof(byte) + //md_cbnamemax
            sizeof(byte); //md_cbname

        private readonly MemoryChunk chunk;

        internal mapdef_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.mapdef_s, FixedStructSize + md_cbname);

        int IViewable.NumChildren() => 11;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(md_spmap), md_spmapOffset, md_spmap);
                    break;

                case 1:
                    structWriter.WriteField(nameof(md_abstype), md_abstypeOffset, md_abstype, sizeof(byte));
                    break;

                case 2:
                    structWriter.WriteField(nameof(md_pad), md_padOffset, md_pad);
                    break;

                case 3:
                    structWriter.WriteField(nameof(md_segentry), md_segentryOffset, md_segentry);
                    break;

                case 4:
                    structWriter.WriteField(nameof(md_cabs), md_cabsOffset, md_cabs);
                    break;

                case 5:
                    structWriter.WriteField(nameof(md_pabsoff), md_pabsoffOffset, md_pabsoff);
                    break;

                case 6:
                    structWriter.WriteField(nameof(md_cseg), md_csegOffset, md_cseg);
                    break;

                case 7:
                    structWriter.WriteField(nameof(md_spseg), md_spsegOffset, md_spseg);
                    break;

                case 8:
                    structWriter.WriteField(nameof(md_cbnamemax), md_cbnamemaxOffset, md_cbnamemax);
                    break;

                case 9:
                    structWriter.WriteField(nameof(md_cbname), md_cbnameOffset, md_cbname);
                    break;

                case 10:
                    structWriter.WriteAnsiFixedLengthField(nameof(md_achname), md_achnameOffset, md_achname);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return md_achname.ToString();
        }
    }
}
