using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    //TpiHash
    public readonly struct TpiHash : IViewableValue
    {
        private const int snOffset = 0;
        private const int snPadOffset = 2;
        private const int cbHashKeyOffset = 4;
        private const int cHashBucketsOffset = 8;
        private const int offcbHashValsOffset = 12;
        private const int offcbTiOffOffset = 20;
        private const int offcbHashAdjOffset = 28;
        /// <summary>
        /// main hash stream
        /// </summary>
        public SN sn
        {
            get => chunk.PeekUInt16(snOffset);
            set => chunk.PokeUInt16(snOffset, value);
        }

        /// <summary>
        /// auxilliary hash data if necessary
        /// </summary>
        public SN snPad
        {
            get => chunk.PeekUInt16(snPadOffset);
            set => chunk.PokeUInt16(snPadOffset, value);
        }

        /// <summary>
        /// size of hash key
        /// </summary>
        public int cbHashKey
        {
            get => chunk.PeekInt32(cbHashKeyOffset);
            set => chunk.PokeInt32(cbHashKeyOffset, value);
        }

        /// <summary>
        /// how many buckets we have
        /// </summary>
        public int cHashBuckets
        {
            get => chunk.PeekInt32(cHashBucketsOffset);
            set => chunk.PokeInt32(cHashBucketsOffset, value);
        }

        /// <summary>
        /// offcb of hashvals
        /// </summary>
        public OffCb offcbHashVals
        {
            get => chunk.PeekUnmanaged<OffCb>(offcbHashValsOffset);
            set => chunk.PokeUnmanaged<OffCb>(offcbHashValsOffset, value);
        }

        /// <summary>
        /// offcb of (TI,OFF) pairs
        /// </summary>
        public OffCb offcbTiOff
        {
            get => chunk.PeekUnmanaged<OffCb>(offcbTiOffOffset);
            set => chunk.PokeUnmanaged<OffCb>(offcbTiOffOffset, value);
        }

        /// <summary>
        /// offcb of hash head list, maps (hashval,ti), where ti is the head of the hashval chain.
        /// </summary>
        public OffCb offcbHashAdj
        {
            get => chunk.PeekUnmanaged<OffCb>(offcbHashAdjOffset);
            set => chunk.PokeUnmanaged<OffCb>(offcbHashAdjOffset, value);
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
            writer.NewStruct(this, ViewKind.TpiHash, StructSize);

        int IViewable.NumChildren() => 7;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(sn), snOffset, sn);
                    break;

                case 1:
                    structWriter.WriteField(nameof(snPad), snPadOffset, snPad);
                    break;

                case 2:
                    structWriter.WriteField(nameof(cbHashKey), cbHashKeyOffset, cbHashKey);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cHashBuckets), cHashBucketsOffset, cHashBuckets);
                    break;

                case 4:
                    structWriter.WriteStructField(nameof(offcbHashVals), offcbHashValsOffset, offcbHashVals);
                    break;

                case 5:
                    structWriter.WriteStructField(nameof(offcbTiOff), offcbTiOffOffset, offcbTiOff);
                    break;

                case 6:
                    structWriter.WriteStructField(nameof(offcbHashAdj), offcbHashAdjOffset, offcbHashAdj);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
