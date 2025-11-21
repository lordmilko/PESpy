namespace PESpy
{
    //symdef_s
    //SYM is too obscure, so I think it's better to use the real name
    [Source(SourceKind.mapsym_h)]
    public readonly struct symdef_s
    {
        /// <summary>
        /// 32 bit symbol addr or const
        /// </summary>
        public int sd_lval => chunk.PeekInt32(0);

        /// <summary>
        /// 8 bit symbol name length
        /// </summary>
        public byte sd_cbname => chunk.PeekByte(4);

        /// <summary>
        /// &lt;n&gt; symbol name
        /// </summary>
        public FixedAnsiString sd_achname => chunk.PeekAnsiFixedLength(5, sd_cbname);

        private readonly MemoryChunk chunk;

        internal symdef_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public override string ToString()
        {
            return sd_achname.ToString();
        }
    };
}
