using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    [DebuggerDisplay("IATIndex = {IATIndex}, IndirectCall = {IndirectCall}, PageRelativeOffset = {PageRelativeOffset}")]
    public readonly struct ImageImportControlTransferDynamicRelocation : IValue, IViewable
    {
        private const int flagsOffset = 0;

        public int PageRelativeOffset => (int) (flags >> 0) & 0xFFF;
        public bool IndirectCall => ((flags >> 12) & 0x1) != 0;
        public int IATIndex => (int) (flags >> 13) & 0x7FFFF;

        private uint flags => chunk.PeekUInt32(flagsOffset);

        public int Offset => chunk.AbsoluteOffset;
        internal const int StructSize =
            sizeof(int); //flags

        private readonly MemoryChunk chunk;

        internal ImageImportControlTransferDynamicRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_IMPORT_CONTROL_TRANSFER_DYNAMIC_RELOCATION, this, ViewKind.ImageImportControlTransferDynamicRelocation, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteBitField(nameof(PageRelativeOffset), flagsOffset, PageRelativeOffset, sizeof(int), 12);
                    break;

                case 1:
                    structWriter.WriteBitField(nameof(IndirectCall), flagsOffset, IndirectCall, sizeof(int), 1);
                    break;

                case 2:
                    structWriter.WriteBitField(nameof(IATIndex), flagsOffset, IATIndex, sizeof(int), 19);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
