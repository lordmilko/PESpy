namespace PESpy.NE
{
    /// <summary>
    /// Resource type information block
    /// </summary>
    public readonly struct RsrcTypeInfo
    {
        public ushort rd_id => chunk.PeekUInt16(0);

        public ushort rt_nres => chunk.PeekUInt16(2);

        public int rt_proc => chunk.PeekInt32(4);

        private readonly MemoryChunk chunk;

        internal RsrcTypeInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
