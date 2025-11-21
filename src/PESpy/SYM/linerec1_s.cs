namespace PESpy
{
    //linerec1_s
    //SYM is too obscure, so I think it's better to use the real name

    /// <summary>
    /// Special line record - 16 bit (<see cref="linedef_s.ld_itype"/> == 1)
    /// </summary>
    [Source(SourceKind.mapsym_h)]
    public readonly struct linerec1_s : IValue
    {
        /// <summary>
        /// start offset for this linenumber
        /// </summary>
        public ushort lr1_codeoffset => chunk.PeekUInt16(0);

        /// <summary>
        /// linenumber
        /// </summary>
        public ushort lr1_linenumber => chunk.PeekUInt16(2);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal linerec1_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    };
}
