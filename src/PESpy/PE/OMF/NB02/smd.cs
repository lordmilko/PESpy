using System;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Old subsection module information<para/>
    /// "smd" in the Microsoft C 6.0 Developer's Toolkit Reference; "oldsmd" in cvexefmt.h
    /// </summary>
    public readonly struct smd : IValue, IViewable
    {
        private const int SegInfoOffset = 0;
        private const int ovlNbrOffset = nsg.StructSize;
        private const int iLibOffset = nsg.StructSize + 2;
        private const int cSegOffset = nsg.StructSize + 4;
        private const int reservedOffset = nsg.StructSize + 5;
        private const int nameOffset = nsg.StructSize + 6;
        private int arnsgOffset => FixedStructSize + name.Length + 1;

        /// <summary>
        /// Describes first segment in module
        /// </summary>
        public nsg SegInfo => new nsg(chunk);

        /// <summary>
        /// Overlay number
        /// </summary>
        public ushort ovlNbr => chunk.PeekUInt16(ovlNbrOffset);

        public ushort iLib => chunk.PeekUInt16(iLibOffset);

        /// <summary>
        /// Number of segments in module
        /// </summary>
        public byte cSeg => chunk.PeekByte(cSegOffset);

        public byte reserved => chunk.PeekByte(reservedOffset);

        public SymString name => chunk.PeekSymString(nameOffset, isLengthPrefixed: true);

        public nsg[] arnsg
        {
            get
            {
                //The spec doesn't seem to say this, but cSeg can be 0, in which case we don't need to read any of these
                if (cSeg == 0)
                    return Array.Empty<nsg>();

                var results = new nsg[cSeg - 1];

                var read = arnsgOffset;

                for (var i = 0; i < results.Length; i++)
                {
                    //The values we get for this seem wrong (segment 276 with a size of 2?)
                    //but it definitely does match what the bytes say, and the total struct size lines up with what's expected
                    results[i] = new nsg(chunk.Slice(read));
                    read += nsg.StructSize;
                }

                return results;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            nsg.StructSize + //SegInfo
            sizeof(short) +  //ovlNbr
            sizeof(short) +  //iLib
            sizeof(byte) +   //cSeg
            sizeof(byte);    //reserved

        internal int StructSize
        {
            get
            {
                var size = FixedStructSize + name.Length + 1;

                var count = this.cSeg;

                //cSeg can be 0. If it's 1, it'll become 0 in the calculation, so we should only
                //factor it in when it's above 1
                if (count > 1)
                    size += ((cSeg - 1) * nsg.StructSize);

                return size;
            }
        }

        private readonly MemoryChunk chunk;

        internal smd(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.smd, this, ViewKind.smd, StructSize);

        int IViewable.NumChildren() => cSeg > 1 ? 7 : 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteStructField(nameof(SegInfo), SegInfo);
                    break;

                case 1:
                    structWriter.WriteField(nameof(ovlNbr), ovlNbrOffset, ovlNbr);
                    break;

                case 2:
                    structWriter.WriteField(nameof(iLib), iLibOffset, iLib);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cSeg), cSegOffset, cSeg);
                    break;

                case 4:
                    structWriter.WriteField(nameof(reserved), reservedOffset, reserved);
                    break;

                case 5:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, name);
                    break;

                case 6:
                    var items = arnsg;

                    if (items.Length == 0)
                        throw new IndexOutOfRangeException();

                    structWriter.WriteStructField("arnsg", items);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
