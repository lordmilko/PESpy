namespace PESpy.NE
{
    //new_rlcinfo
    public readonly struct NewRlcInfo
    {
        /// <summary>
        /// number of relocation items that follow
        /// </summary>
        public ushort nr_nreloc => chunk.PeekUInt16(0);

        private readonly MemoryChunk chunk;

        internal NewRlcInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
