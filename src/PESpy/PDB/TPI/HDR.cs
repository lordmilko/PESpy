using ClrDebug.PDB;

namespace PESpy.PDB
{
    //HDR

    /// <summary>
    /// type database header
    /// </summary>
    public readonly struct HDR
    {
        /// <summary>
        /// version which created this TypeServer
        /// </summary>
        public TPIImpv vers => (TPIImpv) chunk.PeekUInt32(0);

        /// <summary>
        /// size of the header, allows easier upgrading and backwards compatibility
        /// </summary>
        public int cbHdr => chunk.PeekInt32(4);

        /// <summary>
        /// lowest TI
        /// </summary>
        public CV_typ_t tiMin => chunk.PeekInt32(8);

        /// <summary>
        /// highest TI + 1
        /// </summary>
        public CV_typ_t tiMac => chunk.PeekInt32(12);

        /// <summary>
        /// count of bytes used by the gprec which follows.
        /// </summary>
        public int cbGprec => chunk.PeekInt32(16);

        /// <summary>
        /// hash stream schema
        /// </summary>
        public TpiHash tpihash => new TpiHash(chunk.Slice(20));

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //vers
            sizeof(int) + //cbHdr
            sizeof(int) + //tiMin
            sizeof(int) + //tiMac
            sizeof(int) + //cbGprec
            TpiHash.StructSize; //tpihash

        private readonly MemoryChunk chunk;

        internal HDR(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
