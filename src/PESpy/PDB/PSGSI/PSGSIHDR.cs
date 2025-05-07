using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct PSGSIHDR : IValue, IViewable
    {
        public int cbSymHash => chunk.PeekInt32(0);

        public int cbAddrMap => chunk.PeekInt32(4);

        public int nThunks => chunk.PeekInt32(8);

        public int cbSizeOfThunk => chunk.PeekInt32(12);

        public ISECT isectThunkTable => chunk.PeekUInt16(16);

        public short padding => chunk.PeekInt16(18);

        public int offThunkTable => chunk.PeekInt32(20);

        public int nSects => chunk.PeekInt32(24);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //cbSymHash
            sizeof(int) + //cbAddrMap
            sizeof(int) + //nThunks
            sizeof(int) + //cbSizeOfThunk
            sizeof(int) + //isectThunkTable + padding
            sizeof(int) + //offThunkTable
            sizeof(int);  //nSects
        
        private readonly MemoryChunk chunk;

        internal PSGSIHDR(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PSGSIHDR), this, ViewKind.PSGSIHDR);

            s.WriteField(nameof(cbSymHash), cbSymHash);
            s.WriteField(nameof(cbAddrMap), cbAddrMap);
            s.WriteField(nameof(nThunks), nThunks);
            s.WriteField(nameof(cbSizeOfThunk), cbSizeOfThunk);
            s.WriteField(nameof(isectThunkTable), isectThunkTable);
            s.Align(4);
            s.WriteField(nameof(offThunkTable), offThunkTable);
            s.WriteField(nameof(nSects), nSects);
        }
    }
}
