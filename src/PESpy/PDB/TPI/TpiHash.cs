using PESpy.View;

namespace PESpy.PDB
{
    //TpiHash
    public readonly struct TpiHash : IValue, IViewable
    {
        /// <summary>
        /// main hash stream
        /// </summary>
        public SN sn => chunk.PeekUInt16(0);

        /// <summary>
        /// auxilliary hash data if necessary
        /// </summary>
        public SN snPad => chunk.PeekUInt16(2);

        /// <summary>
        /// size of hash key
        /// </summary>
        public int cbHashKey => chunk.PeekInt32(4);

        /// <summary>
        /// how many buckets we have
        /// </summary>
        public int cHashBuckets => chunk.PeekInt32(8);

        /// <summary>
        /// offcb of hashvals
        /// </summary>
        public OffCb offcbHashVals => new OffCb(chunk.Slice(12));

        /// <summary>
        /// offcb of (TI,OFF) pairs
        /// </summary>
        public OffCb offcbTiOff => new OffCb(chunk.Slice(20));

        /// <summary>
        /// offcb of hash head list, maps (hashval,ti), where ti is the head of the hashval chain.
        /// </summary>
        public OffCb offcbHashAdj => new OffCb(chunk.Slice(28));

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //sn
            sizeof(ushort) + //snPad
            sizeof(int) + //cbHashKey
            sizeof(int) + //cHashBuckets
            8 + //offcbHashVals
            8 + //offcbTiOff
            8; //offcbHashAdj

        private readonly MemoryChunk chunk;

        internal TpiHash(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(TpiHash), this, ViewKind.TpiHash);

            s.WriteField(nameof(sn), sn);
            s.WriteField(nameof(snPad), snPad);
            s.WriteField(nameof(cbHashKey), cbHashKey);
            s.WriteField(nameof(cHashBuckets), cHashBuckets);
            s.WriteStructField(nameof(offcbHashVals), offcbHashVals);
            s.WriteStructField(nameof(offcbTiOff), offcbTiOff);
            s.WriteStructField(nameof(offcbHashAdj), offcbHashAdj);
        }
    }
}
