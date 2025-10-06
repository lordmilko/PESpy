using System;
using PESpy.View;

namespace PESpy
{
    public readonly struct OMFSymHash : IValue, IViewable
    {
        public ushort symhash => chunk.PeekUInt16(0);

        public ushort addrhash => chunk.PeekUInt16(2);

        public int cbSymbol => chunk.PeekInt32(4);

        public int cbHSym => chunk.PeekInt32(8);

        public int cbHAddr => chunk.PeekInt32(12);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //symhash
            sizeof(ushort) + //addrhash
            sizeof(int) + //cbSymbol
            sizeof(int) + //cbHSym
            sizeof(int); //cbHAddr

        private readonly MemoryChunk chunk;

        internal OMFSymHash(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.OMFSymHash, this, ViewKind.OMFSymHash, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
