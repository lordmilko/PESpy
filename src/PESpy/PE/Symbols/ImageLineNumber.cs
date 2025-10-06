using PESpy.Native;
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
            sizeof(short); //Linenumber

        private readonly MemoryChunk chunk;

        internal ImageLineNumber(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_LINENUMBER, this, ViewKind.ImageLineNumber, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            if (Linenumber == 0)
                s.WriteField(nameof(SymbolTableIndex), SymbolTableIndex);
            else
                s.WriteField(nameof(VirtualAddress), VirtualAddress);

            s.WriteField(nameof(Linenumber), Linenumber);

            return s.ToArray();
        }
    }
}
