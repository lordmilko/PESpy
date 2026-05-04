namespace PESpy
{
    //linerec0_s
    //SYM is too obscure, so I think it's better to use the real name

    /// <summary>
    /// Normal line record (<see cref="linedef_s.ld_itype"/> == 0)
    /// </summary>
    [Source(SourceKind.mapsym_h)]
    public readonly struct linerec0_s : IValue
    {
        /// <summary>
        /// start offset for this linenumber
        /// </summary>
        public ushort lr0_codeoffset => chunk.PeekUInt16(0);

        /// <summary>
        /// file offset for this linenumber
        /// </summary>
        public int lr0_fileoffset => chunk.PeekInt32(2);

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal linerec0_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    };
}
