using PESpy.View;

namespace PESpy.PDB
{
    public enum GSIHashSCImpv : uint
    {
        GSIHashSCImpvV70 = 0xeffe0000 + 19990810
    }

    public readonly struct GSIHashHdr : IValue, IViewable
    {
        public const int hdrSignature = -1;

        public int verSignature => chunk.PeekInt16(0);

        public GSIHashSCImpv verHdr => (GSIHashSCImpv) chunk.PeekUInt32(4);

        public int cbHr => chunk.PeekInt32(8);

        public int cbBuckets => chunk.PeekInt32(12);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //verSignature
            sizeof(int) + //verHdr
            sizeof(int) + //cbHr
            sizeof(int);  //cbBuckets

        private readonly MemoryChunk chunk;

        internal GSIHashHdr(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.GSIHashHdr, this, ViewKind.GSIHashHdr, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(verSignature), verSignature);
            s.WriteField(nameof(verHdr), verHdr, sizeof(int));
            s.WriteField(nameof(cbHr), cbHr);
            s.WriteField(nameof(cbBuckets), cbBuckets);

            return s.ToArray();
        }
    }
}
