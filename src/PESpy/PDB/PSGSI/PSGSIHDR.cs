using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct PSGSIHDR : IValue, IViewable
    {
        public int cbSymHash
        {
            get => chunk.PeekInt32(0);
            set => chunk.PokeInt32(0, value);
        }

        public int cbAddrMap
        {
            get => chunk.PeekInt32(4);
            set => chunk.PokeInt32(4, value);
        }

        public int nThunks
        {
            get => chunk.PeekInt32(8);
            set => chunk.PokeInt32(8, value);
        }

        public int cbSizeOfThunk
        {
            get => chunk.PeekInt32(12);
            set => chunk.PokeInt32(12, value);
        }

        public ISECT isectThunkTable
        {
            get => chunk.PeekUInt16(16);
            set => chunk.PokeUInt16(16, value);
        }

        public short padding
        {
            get => chunk.PeekInt16(18);
            set => chunk.PokeInt16(18, value);
        }

        public int offThunkTable
        {
            get => chunk.PeekInt32(20);
            set => chunk.PokeInt32(20, value);
        }

        public int nSects
        {
            get => chunk.PeekInt32(24);
            set => chunk.PokeInt32(24, value);
        }

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.PSGSIHDR, this, ViewKind.PSGSIHDR, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(cbSymHash), cbSymHash);
            s.WriteField(nameof(cbAddrMap), cbAddrMap);
            s.WriteField(nameof(nThunks), nThunks);
            s.WriteField(nameof(cbSizeOfThunk), cbSizeOfThunk);
            s.WriteField(nameof(isectThunkTable), isectThunkTable);
            s.Align(4);
            s.WriteField(nameof(offThunkTable), offThunkTable);
            s.WriteField(nameof(nSects), nSects);

            return s.ToArray();
        }
    }
}
