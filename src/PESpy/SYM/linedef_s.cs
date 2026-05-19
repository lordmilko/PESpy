using System;
using PESpy.View;

namespace PESpy.SYM
{
    //linedef_s
    //SYM is too obscure, so I think it's better to use the real name
    [Source(SourceKind.mapsym_h)]
    public readonly struct linedef_s : IViewableValue
    {
        private const int ld_splinenextOffset = 0;
        private const int ld_psegOffset = 2;
        private const int ld_plinerecOffset = 4;
        private const int ld_itypeOffset = 6;
        private const int ld_clineOffset = 8;
        private const int ld_cbnameOffset = 10;
        private const int ld_achnameOffset = 11;

        /// <summary>
        /// 16 bit SEG ptr to next (0 if last), relative to mapdef
        /// </summary>
        public ushort ld_splinenext => chunk.PeekUInt16(ld_splinenextOffset);

        /// <summary>
        /// 16 bit ptr to segdef_s (always 0)
        /// </summary>
        public ushort ld_pseg => chunk.PeekUInt16(ld_psegOffset);

        /// <summary>
        /// 16 bit ptr to linerecs, relative to linedef
        /// </summary>
        public ushort ld_plinerec => chunk.PeekUInt16(ld_plinerecOffset);

        /// <summary>
        /// line rec type 0, 1, or 2
        /// </summary>
        public ushort ld_itype => chunk.PeekUInt16(ld_itypeOffset);

        /// <summary>
        /// 16 bit count of line numbers
        /// </summary>
        public ushort ld_cline => chunk.PeekUInt16(ld_clineOffset);

        /// <summary>
        /// 8 bit file name length
        /// </summary>
        public byte ld_cbname => chunk.PeekByte(ld_cbnameOffset);

        /// <summary>
        /// &lt;n&gt; file name
        /// </summary>
        public FixedAnsiString ld_achname => chunk.PeekAnsiFixedLength(ld_achnameOffset, ld_cbname);

        /// <summary>
        /// Gets the line records associated with this definition. Based on the value of <see cref="ld_itype"/>, this value
        /// is either a <see cref="linerec0_s"/>[], <see cref="linerec1_s"/>[] or <see cref="linerec2_s"/>[].
        /// </summary>
        public Array LineRecords { get; }

        public long Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(short) + //ld_splinenext
            sizeof(short) + //ld_pseg
            sizeof(short) + //ld_plinerec
            sizeof(short) + //ld_itype
            sizeof(short) + //ld_cline
            sizeof(byte); //ld_cbname

        private readonly MemoryChunk chunk;

        internal linedef_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            var dataChunk = chunk.Slice(ld_plinerec);

            var read = 0;

            switch (ld_itype)
            {
                case 0:
                {
                    var lineRecords = new linerec0_s[ld_cline];

                    for (var i = 0; i < lineRecords.Length; i++)
                        lineRecords[i] = new linerec0_s(dataChunk.Slice(i * linerec0_s.StructSize));

                    LineRecords = lineRecords;
                    break;
                }

                case 1:
                {
                    var lineRecords = new linerec1_s[ld_cline];

                    for (var i = 0; i < lineRecords.Length; i++)
                        lineRecords[i] = new linerec1_s(dataChunk.Slice(i * linerec1_s.StructSize));

                    LineRecords = lineRecords;
                    break;
                }

                case 2:
                {
                    var lineRecords = new linerec2_s[ld_cline];

                    for (var i = 0; i < lineRecords.Length; i++)
                        lineRecords[i] = new linerec2_s(dataChunk.Slice(i * linerec2_s.StructSize));

                    LineRecords = lineRecords;
                    break;
                }

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(ld_itype)} '{ld_itype}'");
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            switch (ld_itype)
            {
                case 0:
                    writer.WriteGlobal((linerec0_s[]) LineRecords);
                    break;

                case 1:
                    writer.WriteGlobal((linerec1_s[]) LineRecords);
                    break;

                case 2:
                    writer.WriteGlobal((linerec2_s[]) LineRecords);
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(ld_itype)} '{ld_itype}'");
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.linedef_s, FixedStructSize + ld_cbname);

        int IViewable.NumChildren() => 7;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(ld_splinenext), ld_splinenextOffset, ld_splinenext);
                    break;

                case 1:
                    structWriter.WriteField(nameof(ld_pseg), ld_psegOffset, ld_pseg);
                    break;

                case 2:
                    structWriter.WriteField(nameof(ld_plinerec), ld_plinerecOffset, ld_plinerec);
                    break;

                case 3:
                    structWriter.WriteField(nameof(ld_itype), ld_itypeOffset, ld_itype);
                    break;

                case 4:
                    structWriter.WriteField(nameof(ld_cline), ld_clineOffset, ld_cline);
                    break;

                case 5:
                    structWriter.WriteField(nameof(ld_cbname), ld_cbnameOffset, ld_cbname);
                    break;

                case 6:
                    structWriter.WriteAnsiFixedLengthField(nameof(ld_achname), ld_achnameOffset, ld_achname);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return ld_achname.ToString();
        }
    }
}
