using ClrDebug.OMF;
using PESpy.View;

namespace PESpy
{
    public readonly struct OMFDirEntry : IValue, IViewable
    {
        public SST SubSection => (SST) chunk.PeekUInt16(0);

        public ushort iMod => chunk.PeekUInt16(2);

        public int lfo => chunk.PeekInt32(4);

        public int cb => chunk.PeekInt32(8);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //SubSection
            sizeof(ushort) + //iMod
            sizeof(int) + //lfo
            sizeof(int); //cb

        private readonly MemoryChunk chunk;

        internal OMFDirEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(ClrDebug.OMF.OMFDirEntry), this, ViewKind.OMFDirEntry);

            s.WriteField(nameof(SubSection), SubSection, sizeof(ushort));
            s.WriteField(nameof(iMod), iMod);
            s.WriteField(nameof(lfo), lfo);
            s.WriteField(nameof(cb), cb);
        }

        public override string ToString()
        {
            return SubSection.ToString();
        }
    }
}
