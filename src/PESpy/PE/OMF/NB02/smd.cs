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
        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            nsg.StructSize + //SegInfo
            sizeof(short) +  //ovlNbr
            sizeof(short) +  //iLib
            sizeof(byte) +   //cSeg
            sizeof(byte);    //reserved

        internal int StructSize => FixedStructSize + name.Length + 1;

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
            s.WriteAnsiFixedLengthField(nameof(name), name);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
