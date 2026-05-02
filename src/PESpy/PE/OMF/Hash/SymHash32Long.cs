using System;
using PESpy.View;

namespace PESpy
{
    public readonly struct SymHash32Long : IValue, IViewable
    {
        public int Kind { get; }

        public ushort cBuckets { get; }

        public ushort Alignment { get; }

        public NativeSpan<int> Buckets { get; }

        public NativeSpan<int> Counts { get; }

        public NativeSpan<(int symbolOffset, uint checksum)>[] Chains { get; }

        public int Offset { get; }

        internal int StructSize
        {
            get
            {
                var size =
                    sizeof(short) +
                    sizeof(short);

                size += (cBuckets * (sizeof(int) + sizeof(int))); //Buckets, Counts

                foreach (var count in Counts)
                    size += (count * (sizeof(int) + sizeof(int)));

                return size;
            }
        }

        internal SymHash32Long(
            int offset,
            int kind,
            ushort cBuckets,
            ushort alignment,
            NativeSpan<int> buckets,
            NativeSpan<int> counts,
            NativeSpan<(int symbolOffset, uint checksum)>[] chains)
        {
            Offset = offset;
            Kind = kind;
            this.cBuckets = cBuckets;
            Alignment = alignment;
            Buckets = buckets;
            Counts = counts;
            Chains = chains;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.SymHash32Long, StructSize);

        int IViewable.NumChildren() => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(cBuckets), 0, cBuckets);
                    break;

                case 1:
                    structWriter.WriteField("pad", 2, Alignment);
                    break;

                case 2:
                    //dumpsym7.cpp doesn't define a variable for this
                    structWriter.WriteField(nameof(Buckets), 4, Buckets);
                    break;

                case 3:
                    structWriter.WriteField("rgCounts", 4 + (cBuckets * sizeof(int)), Counts);
                    break;

                case 4:
                    var chainsSize = 0;

                    foreach (var count in Counts)
                        chainsSize += (count * (sizeof(int) + sizeof(int)));

                    //dumpsym7.cpp doesn't define a variable for this
                    structWriter.WriteField(nameof(Chains), 4 + (cBuckets * (sizeof(int) + sizeof(int))), Chains, chainsSize);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
