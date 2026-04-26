using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct PSGSIHDR : IValue, IViewable
    {
        private const int cbSymHashOffset = 0;
        private const int cbAddrMapOffset = 4;
        private const int nThunksOffset = 8;
        private const int cbSizeOfThunkOffset = 12;
        private const int isectThunkTableOffset = 16;
        private const int paddingOffset = 18;
        private const int offThunkTableOffset = 20;
        private const int nSectsOffset = 24;

        public int cbSymHash
        {
            get => chunk.PeekInt32(cbSymHashOffset);
            set => chunk.PokeInt32(cbSymHashOffset, value);
        }

        public int cbAddrMap
        {
            get => chunk.PeekInt32(cbAddrMapOffset);
            set => chunk.PokeInt32(cbAddrMapOffset, value);
        }

        public int nThunks
        {
            get => chunk.PeekInt32(nThunksOffset);
            set => chunk.PokeInt32(nThunksOffset, value);
        }

        public int cbSizeOfThunk
        {
            get => chunk.PeekInt32(cbSizeOfThunkOffset);
            set => chunk.PokeInt32(cbSizeOfThunkOffset, value);
        }

        public ISECT isectThunkTable
        {
            get => chunk.PeekUInt16(isectThunkTableOffset);
            set => chunk.PokeUInt16(isectThunkTableOffset, value);
        }

        public short padding
        {
            get => chunk.PeekInt16(paddingOffset);
            set => chunk.PokeInt16(paddingOffset, value);
        }

        public int offThunkTable
        {
            get => chunk.PeekInt32(offThunkTableOffset);
            set => chunk.PokeInt32(offThunkTableOffset, value);
        }

        public int nSects
        {
            get => chunk.PeekInt32(nSectsOffset);
            set => chunk.PokeInt32(nSectsOffset, value);
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
            writer.NewStruct(this, ViewKind.PSGSIHDR, StructSize);

        int IViewable.NumChildren() => 8;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(cbSymHash), cbSymHashOffset, cbSymHash);
                    break;

                case 1:
                    structWriter.WriteField(nameof(cbAddrMap), cbAddrMapOffset, cbAddrMap);
                    break;

                case 2:
                    structWriter.WriteField(nameof(nThunks), nThunksOffset, nThunks);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cbSizeOfThunk), cbSizeOfThunkOffset, cbSizeOfThunk);
                    break;

                case 4:
                    structWriter.WriteField(nameof(isectThunkTable), isectThunkTableOffset, isectThunkTable);
                    break;

                case 5:
                    structWriter.WriteByteBlob(isectThunkTableOffset + sizeof(ushort), sizeof(ushort));
                    break;

                case 6:
                    structWriter.WriteField(nameof(offThunkTable), offThunkTableOffset, offThunkTable);
                    break;

                case 7:
                    structWriter.WriteField(nameof(nSects), nSectsOffset, nSects);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
