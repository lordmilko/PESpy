using PESpy.View;

namespace PESpy
{
    public readonly struct ImageLineNumber : IValue, IViewable
    {
        public int SymbolTableIndex => chunk.PeekInt32(0);

        public int VirtualAddress => chunk.PeekInt32(0);

        public short Linenumber => chunk.PeekInt16(4);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //SymbolTableIndex / VirtualAddress
            sizeof(short);

        private readonly MemoryChunk chunk;

        internal ImageLineNumber(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            throw new System.NotImplementedException();
        }
    }
}
