namespace PESpy
{
    //linerec2_s
    //SYM is too obscure, so I think it's better to use the real name

    /// <summary>
    /// Special line record - 32 bit (<see cref="linedef_s.ld_itype"/> == 2)
    /// </summary>
    [Source(SourceKind.mapsym_h)]
    public readonly struct linerec2_s : IValue
    {
        /// <summary>
        /// start offset for this linenumber
        /// </summary>
        public int lr2_codeoffset => chunk.PeekInt32(0);

        /// <summary>
        /// linenumber
        /// </summary>
        public ushort lr2_linenumber => chunk.PeekUInt16(4);

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal linerec2_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    };
}
