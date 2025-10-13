using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Old subsection module information<para/>
    /// "smd" in the Microsoft C 6.0 Developer's Toolkit Reference; "oldsmd" in cvexefmt.h
    /// </summary>
    public readonly struct smd : IValue, IViewable
    {
        /// <summary>
        /// Describes first segment in module
        /// </summary>
        public nsg SegInfo => new nsg(chunk);

        /// <summary>
        /// Overlay number
        /// </summary>
        public ushort ovlNbr => chunk.PeekUInt16(nsg.StructSize);

        public ushort iLib => chunk.PeekUInt16(nsg.StructSize + 2);

        /// <summary>
        /// Number of segments in module
        /// </summary>
        public byte cSeg => chunk.PeekByte(nsg.StructSize + 4);

        public byte reserved => chunk.PeekByte(nsg.StructSize + 5);

        public FixedAnsiString name
        {
            get
            {
                var length = chunk.PeekByte(nsg.StructSize + 6);
                return chunk.PeekAnsiFixedLength(nsg.StructSize + 7, length);
            }
        }

        public nsg[] arnsg
        {
            get
            {
                //The spec doesn't seem to say this, but cSeg can be 0, in which case we don't need to read any of these
                if (cSeg == 0)
                    return Array.Empty<nsg>();

                var results = new nsg[cSeg - 1];

                var read = FixedStructSize + name.Length + 1;

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteStructField(nameof(SegInfo), SegInfo);
            s.WriteField(nameof(ovlNbr), ovlNbr);
            s.WriteField(nameof(iLib), iLib);
            s.WriteField(nameof(cSeg), cSeg);
            s.WriteField(nameof(reserved), reserved);
            s.WriteLengthPrefixedAnsiField(nameof(name), name);

            var items = arnsg;

            if (items.Length > 0)
                s.WriteStructField("arnsg", items);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
