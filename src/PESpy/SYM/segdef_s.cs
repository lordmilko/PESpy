namespace PESpy.SYM
{
    //segdef_s
    //SYM is too obscure, so I think it's better to use the real name
    [Source(SourceKind.mapsym_h)]
    public readonly struct segdef_s
    {
        /// <summary>
        /// 16 bit SEG ptr to next segdef (0 if end), relative to mapdef
        /// </summary>
        public ushort gd_spsegnext => chunk.PeekUInt16(0);

        /// <summary>
        /// 16 bit count of symbols in sym list
        /// </summary>
        public ushort gd_csym => chunk.PeekUInt16(2);

        /// <summary>
        /// 16 bit ptr to symbol offsets array, 16 bit SEG ptr if MSF_BIG_GROUP set, either relative to segdef
        /// </summary>
        public ushort gd_psymoff => chunk.PeekUInt16(4);

        /// <summary>
        /// 16 bit Load Segment address
        /// </summary>
        public ushort gd_lsa => chunk.PeekUInt16(6);

        /// <summary>
        /// 16 bit instance 0 physical address
        /// </summary>
        public ushort gd_in0 => chunk.PeekUInt16(8);

        /// <summary>
        /// 16 bit instance 1 physical address
        /// </summary>
        public ushort gd_in1 => chunk.PeekUInt16(10);

        /// <summary>
        /// 16 bit instance 2 physical address
        /// </summary>
        public ushort gd_in2 => chunk.PeekUInt16(12);

        /// <summary>
        /// 16 or 32 bit symbols in group
        /// </summary>
        public MSF gd_type => (MSF) chunk.PeekByte(14);

        /// <summary>
        /// pad byte to fill space for gd_in3
        /// </summary>
        public byte gd_pad => chunk.PeekByte(15);

        /// <summary>
        /// 16 bit SEG ptr to linedef, relative to mapdef
        /// </summary>
        public ushort gd_spline => chunk.PeekUInt16(16);

        /// <summary>
        /// 8 bit boolean 0 if seg not loaded
        /// </summary>
        public byte gd_fload => chunk.PeekByte(18);

        /// <summary>
        /// 8 bit current instance
        /// </summary>
        public byte gd_curin => chunk.PeekByte(19);

        /// <summary>
        /// 8 bit Segment name length
        /// </summary>
        public byte gd_cbname => chunk.PeekByte(20);

        /// <summary>
        /// &lt;n&gt;  name of segment or group
        /// </summary>
        public FixedAnsiString gd_achname => chunk.PeekAnsiFixedLength(21, gd_cbname);

        #region PESpy

        public SymbolInfo Symbols { get; }

        public linedef_s[]? Lines { get; }

        #endregion

        private readonly MemoryChunk chunk;

        internal segdef_s(int segmentOffset, in MemoryChunk globalChunk)
        {
            //Information about how to read segdef_s is from from symres GetNameFromAddr in XP

            this.chunk = globalChunk.Slice(segmentOffset);

            Symbols = new SymbolInfo(this.chunk, this);

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

        //Type is made up
        public struct SymbolInfo
        {
            public symdef16_s[] Symbols16 { get; }

            public symdef_s[] Symbols32 { get; }

            //The offsets to the symbols and the symbols themselves are relative to the offset of the segdef_s
            internal SymbolInfo(in MemoryChunk chunk, in segdef_s seg)
            {
                //gd_psymoff points to an array of _offsets_ to symbol records. These offsets are
                //2 bytes large when not using big symbols, and 3 bytes large when you are using big symbols

                int arrayOffset;
                int bytesPerOffset;

                //from symres GetNameFromAddr in XP
                if ((seg.gd_type & MSF.MSF_BIGSYMDEF) != 0)
                {
                    //When there's a large number of symbols, gd_psymoff is a distance in paragraphs
                    arrayOffset = seg.gd_psymoff * 16;

                    bytesPerOffset = 3;
                }
                else
                {
                    arrayOffset = seg.gd_psymoff;

                    bytesPerOffset = 2;
                }

                //Read the offsets of each symbol

                var symbolOffsetsChunk = chunk.Slice(arrayOffset);

                var symbolOffsets = new int[seg.gd_csym];

                for (var i = 0; i < symbolOffsets.Length; i++)
                {
                    var bytes = symbolOffsetsChunk.PeekNativeSpan<byte>(i * bytesPerOffset, bytesPerOffset);

                    if (bytesPerOffset == 2)
                        symbolOffsets[i] = bytes[0] | (bytes[1] << 8);
                    else
                        symbolOffsets[i] = bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);
                }

                //Read the actual symbols themselves
                symdef16_s[] symbols16 = null;
                symdef_s[] symbols32 = null;

                if ((seg.gd_type & MSF.MSF_32BITSYMS) != 0)
                {
                    symbols32 = new symdef_s[seg.gd_csym];

                    for (var i = 0; i < symbols32.Length; i++)
                        symbols32[i] = new symdef_s(chunk.Slice(symbolOffsets[i]));
                }
                else
                {
                    symbols16 = new symdef16_s[seg.gd_csym];

                    for (var i = 0; i < symbols16.Length; i++)
                        symbols16[i] = new symdef16_s(chunk.Slice(symbolOffsets[i]));
                }

                Symbols16 = symbols16;
                Symbols32 = symbols32;
            }
        }

        public override string ToString()
        {
            return gd_achname.ToString();
        }
    }
}
