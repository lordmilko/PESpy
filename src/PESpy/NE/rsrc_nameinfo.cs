namespace PESpy.NE
{
    //rsrc_nameinfo

    /// <summary>
    /// Resource name information block
    /// </summary>
    public readonly struct rsrc_nameinfo
    {
        /// <summary>
        /// if high bit of ID set then integer id otherwise ID is offset of string from
        /// the beginning of the resource table
        /// </summary>
        public const ushort RSORDID = 0x8000;

        /* The following two fields must be shifted left by the value of
         * the rs_align field to compute their actual value.  This allows
         * resources to be larger than 64k, but they do not need to be
         * aligned on 512 byte boundaries, the way segments are */

        /// <summary>
        /// file offset to resource data
        /// </summary>
        public ushort rn_offset => chunk.PeekUInt16(0);

        /// <summary>
        /// length of resource data
        /// </summary>
        public ushort rn_length => chunk.PeekUInt16(2);

        /// <summary>
        /// resource flags
        /// </summary>
        public ushort rn_flags => chunk.PeekUInt16(4);

        /// <summary>
        /// resource name id
        /// </summary>
        public ushort rn_id => chunk.PeekUInt16(6);

        /// <summary>
        /// If loaded, then global handle
        /// </summary>
        public ushort rn_handle => chunk.PeekUInt16(8);

        /// <summary>
        /// Initially zero. Number of times the handle for this resource has been given out
        /// </summary>
        public ushort rn_usage => chunk.PeekUInt16(10);

        private readonly MemoryChunk chunk;

        internal rsrc_nameinfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
