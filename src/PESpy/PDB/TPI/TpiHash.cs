using PESpy.View;

namespace PESpy.PDB
{
    //TpiHash
    public readonly struct TpiHash : IValue, IViewable
    {
        /// <summary>
        /// main hash stream
        /// </summary>
        public SN sn
        {
            get => chunk.PeekUInt16(0);
            set => chunk.PokeUInt16(0, value);
        }

        /// <summary>
        /// auxilliary hash data if necessary
        /// </summary>
        public SN snPad
        {
            get => chunk.PeekUInt16(2);
            set => chunk.PokeUInt16(2, value);
        }

        /// <summary>
        /// size of hash key
        /// </summary>
        public int cbHashKey
        {
            get => chunk.PeekInt32(4);
            set => chunk.PokeInt32(4, value);
        }

        /// <summary>
        /// how many buckets we have
        /// </summary>
        public int cHashBuckets
        {
            get => chunk.PeekInt32(8);
            set => chunk.PokeInt32(8, value);
        }

        /// <summary>
        /// offcb of hashvals
        /// </summary>
        public OffCb offcbHashVals
        {
            get => chunk.PeekUnmanaged<OffCb>(12);
            set => chunk.PokeUnmanaged<OffCb>(12, value);
        }

        /// <summary>
        /// offcb of (TI,OFF) pairs
        /// </summary>
        public OffCb offcbTiOff
        {
            get => chunk.PeekUnmanaged<OffCb>(20);
            set => chunk.PokeUnmanaged<OffCb>(20, value);
        }

        /// <summary>
        /// offcb of hash head list, maps (hashval,ti), where ti is the head of the hashval chain.
        /// </summary>
        public OffCb offcbHashAdj
        {
            get => chunk.PeekUnmanaged<OffCb>(28);
            set => chunk.PokeUnmanaged<OffCb>(28, value);
        }

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.TpiHash, this, ViewKind.TpiHash, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(sn), sn);
            s.WriteField(nameof(snPad), snPad);
            s.WriteField(nameof(cbHashKey), cbHashKey);
            s.WriteField(nameof(cHashBuckets), cHashBuckets);
            s.WriteStructField(nameof(offcbHashVals), offcbHashVals);
            s.WriteStructField(nameof(offcbTiOff), offcbTiOff);
            s.WriteStructField(nameof(offcbHashAdj), offcbHashAdj);

            return s.ToArray();
        }
    }
}
