using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.cvexefmt)]
    public readonly struct OMFSegDesc : IValue, IViewable
    {
        public ushort Seg => chunk.PeekUInt16(0);

        public ushort pad => chunk.PeekUInt16(2);

        public int Off => chunk.PeekInt32(4);

        public int cbSeg => chunk.PeekInt32(8);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //Seg
            sizeof(ushort) + //pad
            sizeof(int) +    //Off
            sizeof(int);     //cbSeg

        private readonly MemoryChunk chunk;

        internal OMFSegDesc(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.OMFModule, this, ViewKind.OMFSegDesc, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Seg), Seg);
            s.WriteField(nameof(pad), pad);
            s.WriteField(nameof(Off), Off);
            s.WriteField(nameof(cbSeg), cbSeg);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
