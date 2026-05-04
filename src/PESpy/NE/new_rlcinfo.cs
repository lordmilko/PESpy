namespace PESpy.NE
{
    //new_rlcinfo
    public readonly struct new_rlcinfo
    {
        /// <summary>
        /// number of relocation items that follow
        /// </summary>
        public ushort nr_nreloc => chunk.PeekUInt16(0);

        private readonly MemoryChunk chunk;

        internal new_rlcinfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
