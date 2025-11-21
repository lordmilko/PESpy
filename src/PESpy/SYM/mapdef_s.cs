namespace PESpy.SYM
{
    //mapdef_s
    //SYM is too obscure, so I think it's better to use the real name
    [Source(SourceKind.mapsym_h)]
    public readonly struct mapdef_s
    {
        /// <summary>
        /// 16 bit SEG ptr to next map (0 if end)
        /// </summary>
        public ushort md_spmap => chunk.PeekUInt16(0);

        /// <summary>
        /// 8 bit map/abs sym flags
        /// </summary>
        public MSF md_abstype => (MSF) chunk.PeekByte(2);

        /// <summary>
        /// 8 bit pad
        /// </summary>
        public byte md_pad => chunk.PeekByte(3);

        /// <summary>
        /// 16 bit entry point segment value
        /// </summary>
        public ushort md_segentry => chunk.PeekUInt16(4);

        /// <summary>
        /// 16 bit count of constants in map
        /// </summary>
        public ushort md_cabs => chunk.PeekUInt16(6);

        /// <summary>
        /// 16 bit ptr to constant offsets
        /// </summary>
        public ushort md_pabsoff => chunk.PeekUInt16(8);

        /// <summary>
        /// 16 bit count of segments in map
        /// </summary>
        public ushort md_cseg => chunk.PeekUInt16(10);

        /// <summary>
        /// 16 bit SEG ptr to segment chain
        /// </summary>
        public ushort md_spseg => chunk.PeekUInt16(12);

        /// <summary>
        /// 8 bit maximum symbol name length
        /// </summary>
        public byte md_cbnamemax => chunk.PeekByte(14);

        /// <summary>
        /// 8 bit symbol table name length
        /// </summary>
        public byte md_cbname => chunk.PeekByte(15);

        /// <summary>
        /// &lt;n&gt; name of symbol table (.sym )
        /// </summary>
        public FixedAnsiString md_achname => chunk.PeekAnsiFixedLength(16, md_cbname);

        public int Offset => chunk.AbsoluteOffset;

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

        public override string ToString()
        {
            return md_achname.ToString();
        }
    }
}
