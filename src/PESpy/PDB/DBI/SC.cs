using ClrDebug;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct SC : IValue, IViewable
    {
        public ISECT isect => chunk.PeekUInt16(0);

        public ushort padding1 => chunk.PeekUInt16(2);

        public int off => chunk.PeekInt32(4);

        public int cb => chunk.PeekInt32(8);

        public IMAGE_SCN dwCharacteristics => (IMAGE_SCN) chunk.PeekUInt32(12);

        public IMOD imod => chunk.PeekUInt16(16);

        public ushort padding2 => chunk.PeekUInt16(18);

        public int dwDataCrc => chunk.PeekInt32(20);

        public int dwRelocCrc => chunk.PeekInt32(24);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //isect
            sizeof(ushort) + //padding1
            sizeof(int) + //off
            sizeof(int) + //cb
            sizeof(int) + //dwCharacteristics
            sizeof(ushort) + //imod
            sizeof(ushort) + //padding2
            sizeof(int) + //dwDataCrc
            sizeof(int); //dwRelocCrc

        private readonly MemoryChunk chunk;

        internal SC(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(SC), this, ViewKind.SC);

            s.WriteField(nameof(isect), isect);
            s.WriteField(nameof(padding1), padding1);
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(cb), cb);
            s.WriteField(nameof(dwCharacteristics), dwCharacteristics, sizeof(int));
            s.WriteField(nameof(imod), imod);
            s.WriteField(nameof(padding2), padding2);
            s.WriteField(nameof(dwDataCrc), dwDataCrc);
            s.WriteField(nameof(dwRelocCrc), dwRelocCrc);
        }
    }
}
