using System;
using PESpy.View;

namespace PESpy.SYM
{
    //segdef_s
    //SYM is too obscure, so I think it's better to use the real name
    [Source(SourceKind.mapsym_h)]
    public readonly struct segdef_s : IViewableValue
    {
        private const int gd_spsegnextOffset = 0;
        private const int gd_csymOffset = 2;
        private const int gd_psymoffOffset = 4;
        private const int gd_lsaOffset = 6;
        private const int gd_in0Offset = 8;
        private const int gd_in1Offset = 10;
        private const int gd_in2Offset = 12;
        private const int gd_typeOffset = 14;
        private const int gd_padOffset = 15;
        private const int gd_splineOffset = 16;
        private const int gd_floadOffset = 18;
        private const int gd_curinOffset = 19;
        private const int gd_cbnameOffset = 20;
        private const int gd_achnameOffset = 21;

        /// <summary>
        /// 16 bit SEG ptr to next segdef (0 if end), relative to mapdef
        /// </summary>
        public ushort gd_spsegnext => chunk.PeekUInt16(gd_spsegnextOffset);

        /// <summary>
        /// 16 bit count of symbols in sym list
        /// </summary>
        public ushort gd_csym => chunk.PeekUInt16(gd_csymOffset);

        /// <summary>
        /// 16 bit ptr to symbol offsets array, 16 bit SEG ptr if MSF_BIG_GROUP set, either relative to segdef
        /// </summary>
        public ushort gd_psymoff => chunk.PeekUInt16(gd_psymoffOffset);

        /// <summary>
        /// 16 bit Load Segment address
        /// </summary>
        public ushort gd_lsa => chunk.PeekUInt16(gd_lsaOffset);

        /// <summary>
        /// 16 bit instance 0 physical address
        /// </summary>
        public ushort gd_in0 => chunk.PeekUInt16(gd_in0Offset);

        /// <summary>
        /// 16 bit instance 1 physical address
        /// </summary>
        public ushort gd_in1 => chunk.PeekUInt16(gd_in1Offset);

        /// <summary>
        /// 16 bit instance 2 physical address
        /// </summary>
        public ushort gd_in2 => chunk.PeekUInt16(gd_in2Offset);

        /// <summary>
        /// 16 or 32 bit symbols in group
        /// </summary>
        public MSF gd_type => (MSF) chunk.PeekByte(gd_typeOffset);

        /// <summary>
        /// pad byte to fill space for gd_in3
        /// </summary>
        public byte gd_pad => chunk.PeekByte(gd_padOffset);

        /// <summary>
        /// 16 bit SEG ptr to linedef, relative to mapdef
        /// </summary>
        public ushort gd_spline => chunk.PeekUInt16(gd_splineOffset);

        /// <summary>
        /// 8 bit boolean 0 if seg not loaded
        /// </summary>
        public byte gd_fload => chunk.PeekByte(gd_floadOffset);

        /// <summary>
        /// 8 bit current instance
        /// </summary>
        public byte gd_curin => chunk.PeekByte(gd_curinOffset);

        /// <summary>
        /// 8 bit Segment name length
        /// </summary>
        public byte gd_cbname => chunk.PeekByte(gd_cbnameOffset);

        /// <summary>
        /// &lt;n&gt;  name of segment or group
        /// </summary>
        public FixedAnsiString gd_achname => chunk.PeekAnsiFixedLength(gd_achnameOffset, gd_cbname);

        #region PESpy

        public SymbolInfo Symbols { get; }

        public linedef_s[]? Lines { get; }

        #endregion

        public long Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(short) + //gd_spsegnext
            sizeof(short) + //gd_csym
            sizeof(short) + //gd_psymoff
            sizeof(short) + //gd_lsa
            sizeof(short) + //gd_in0
            sizeof(short) + //gd_in1
            sizeof(short) + //gd_in2
            sizeof(byte) + //gd_type
            sizeof(byte) + //gd_pad
            sizeof(short) + //gd_spline
            sizeof(byte) + //gd_fload
            sizeof(byte) + //gd_curin
            sizeof(byte); //gd_cbname

        private readonly MemoryChunk chunk;

        internal segdef_s(int segmentOffset, in MemoryChunk globalChunk)
        {
            //Information about how to read segdef_s is from from symres GetNameFromAddr in XP

            this.chunk = globalChunk.Slice(segmentOffset);

            Symbols = new SymbolInfo(this.chunk, gd_type, gd_psymoff, gd_csym, true);

            if (gd_spline != 0)
            {
                using var results = new ValueList<linedef_s>();

                var offset = gd_spline;

                while (offset != 0)
                {
                    var lineDef = new linedef_s(globalChunk.Slice(gd_spline * 16));

                    results.Add(lineDef);

                    offset = lineDef.ld_splinenext;
                }

                Lines = results.ToArray();
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            Symbols.WriteGlobals(writer);
            writer.WriteGlobal(Lines);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.segdef_s, FixedStructSize + gd_cbname); //temp

        int IViewable.NumChildren() => 14;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(gd_spsegnext), gd_spsegnextOffset, gd_spsegnext);
                    break;

                case 1:
                    structWriter.WriteField(nameof(gd_csym), gd_csymOffset, gd_csym);
                    break;

                case 2:
                    structWriter.WriteField(nameof(gd_psymoff), gd_psymoffOffset, gd_psymoff);
                    break;

                case 3:
                    structWriter.WriteField(nameof(gd_lsa), gd_lsaOffset, gd_lsa);
                    break;

                case 4:
                    structWriter.WriteField(nameof(gd_in0), gd_in0Offset, gd_in0);
                    break;

                case 5:
                    structWriter.WriteField(nameof(gd_in1), gd_in1Offset, gd_in1);
                    break;

                case 6:
                    structWriter.WriteField(nameof(gd_in2), gd_in2Offset, gd_in2);
                    break;

                case 7:
                    structWriter.WriteField(nameof(gd_type), gd_typeOffset, gd_type, sizeof(byte));
                    break;

                case 8:
                    structWriter.WriteField(nameof(gd_pad), gd_padOffset, gd_pad);
                    break;

                case 9:
                    structWriter.WriteField(nameof(gd_spline), gd_splineOffset, gd_spline);
                    break;

                case 10:
                    structWriter.WriteField(nameof(gd_fload), gd_floadOffset, gd_fload);
                    break;

                case 11:
                    structWriter.WriteField(nameof(gd_curin), gd_curinOffset, gd_curin);
                    break;

                case 12:
                    structWriter.WriteField(nameof(gd_cbname), gd_cbnameOffset, gd_cbname);
                    break;

                case 13:
                    structWriter.WriteAnsiFixedLengthField(nameof(gd_achname), gd_achnameOffset, gd_achname);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return gd_achname.ToString();
        }
    }
}
