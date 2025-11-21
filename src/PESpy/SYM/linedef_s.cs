using System;

namespace PESpy
{
    //linedef_s
    //SYM is too obscure, so I think it's better to use the real name
    [Source(SourceKind.mapsym_h)]
    public readonly struct linedef_s
    {
        /// <summary>
        /// 16 bit SEG ptr to next (0 if last), relative to mapdef
        /// </summary>
        public ushort ld_splinenext => chunk.PeekUInt16(0);

        /// <summary>
        /// 16 bit ptr to segdef_s (always 0)
        /// </summary>
        public ushort ld_pseg => chunk.PeekUInt16(2);

        /// <summary>
        /// 16 bit ptr to linerecs, relative to linedef
        /// </summary>
        public ushort ld_plinerec => chunk.PeekUInt16(4);

        /// <summary>
        /// line rec type 0, 1, or 2
        /// </summary>
        public ushort ld_itype => chunk.PeekUInt16(6);

        /// <summary>
        /// 16 bit count of line numbers
        /// </summary>
        public ushort ld_cline => chunk.PeekUInt16(8);

        /// <summary>
        /// 8 bit file name length
        /// </summary>
        public byte ld_cbname => chunk.PeekByte(10);

        /// <summary>
        /// &lt;n&gt; file name
        /// </summary>
        public FixedAnsiString ld_achname => chunk.PeekAnsiFixedLength(11, ld_cbname);

        public IValue LineRecord { get; }

        private readonly MemoryChunk chunk;

        internal linedef_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            var dataChunk = chunk.Slice(ld_plinerec);

            switch (ld_itype)
            {
                case 0:
                    LineRecord = new linerec0_s(dataChunk);
                    break;

                case 1:
                    LineRecord = new linerec1_s(dataChunk);
                    break;

                case 2:
                    LineRecord = new linerec2_s(dataChunk);
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(ld_itype)} '{ld_itype}'");
            }
        }

        public override string ToString()
        {
            return ld_achname.ToString();
        }
    }
}
