namespace PESpy
{
    //symdef16_s
    //SYM is too obscure, so I think it's better to use the real name
    [Source(SourceKind.mapsym_h)]
    public readonly struct symdef16_s
    {
        /// <summary>
        /// 16 bit symbol addr or const
        /// </summary>
        public ushort sd16_val => chunk.PeekUInt16(0);

        /// <summary>
        /// 8 bit symbol name length
        /// </summary>
        public byte sd16_cbname => chunk.PeekByte(2);

        /// <summary>
        /// &lt;n&gt; symbol name
        /// </summary>
        public FixedAnsiString sd16_achname => chunk.PeekAnsiFixedLength(3, sd16_cbname);

        private readonly MemoryChunk chunk;

        internal symdef16_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public override string ToString()
        {
            return sd16_achname.ToString();
        }
    }
}
