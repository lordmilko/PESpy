using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Information describing each segment in a module<para/>
    /// "nsg" in the Microsoft C 6.0 Developer's Toolkit Reference; "oldnsg" in cvexefmt.h
    /// </summary>
    public readonly struct nsg : IValue, IViewable
    {
        /// <summary>
        /// Segment index
        /// </summary>
        public ushort Seg => chunk.PeekUInt16(0);

        /// <summary>
        /// Offset of code in segment
        /// </summary>
        public ushort Off => chunk.PeekUInt16(2);

        /// <summary>
        /// Number of bytes in segment
        /// </summary>
        public ushort cbSeg => chunk.PeekUInt16(4);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //Seg
            sizeof(short) + //Off
            sizeof(short); //cbSeg

        private readonly MemoryChunk chunk;

        internal nsg(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.nsg, this, ViewKind.nsg, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Seg), Seg);
            s.WriteField(nameof(Off), Off);
            s.WriteField(nameof(cbSeg), cbSeg);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
