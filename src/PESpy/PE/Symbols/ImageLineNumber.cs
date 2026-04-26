using System;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageLineNumber : IValue, IViewable
    {
        private const int SymbolTableIndexOrVirtualAddressOffset = 0;
        private const int LinenumberOffset = 4;

        public int SymbolTableIndex => chunk.PeekInt32(SymbolTableIndexOrVirtualAddressOffset);

        public int VirtualAddress => chunk.PeekInt32(SymbolTableIndexOrVirtualAddressOffset);

        public short Linenumber => chunk.PeekInt16(LinenumberOffset);

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
            writer.NewStruct(this, ViewKind.ImageLineNumber, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    if (Linenumber == 0)
                        structWriter.WriteField(nameof(SymbolTableIndex), SymbolTableIndexOrVirtualAddressOffset, SymbolTableIndex);
                    else
                        structWriter.WriteField(nameof(VirtualAddress), SymbolTableIndexOrVirtualAddressOffset, VirtualAddress);

                    break;

                case 1:
                    structWriter.WriteField(nameof(Linenumber), LinenumberOffset, Linenumber);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
