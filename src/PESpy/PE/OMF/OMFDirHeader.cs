using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    //NT 4 refers to this as the CV 4.0 dnthdr/DNTHDR
    [Source(SourceKind.cvexefmt)]
    public readonly struct OMFDirHeader : IValue, IViewable
    {
        public ushort cbDirHeader => chunk.PeekUInt16(0);

        public ushort cbDirEntry => chunk.PeekUInt16(2);

        public int cDir => chunk.PeekInt32(4);

        public int lfoNextDir => chunk.PeekInt32(8);

        public int flags => chunk.PeekInt32(12);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //cbDirHeader
            sizeof(ushort) + //cbDirEntry
            sizeof(int) + //cDir
            sizeof(int) + //lfoNextDir
            sizeof(int); //flags

        private readonly MemoryChunk chunk;

        internal OMFDirHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.OMFDirHeader, this, ViewKind.OMFDirHeader, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(cbDirHeader), cbDirHeader);
            s.WriteField(nameof(cbDirEntry), cbDirEntry);
            s.WriteField(nameof(cDir), cDir);
            s.WriteField(nameof(lfoNextDir), lfoNextDir);
            s.WriteField(nameof(flags), flags);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
