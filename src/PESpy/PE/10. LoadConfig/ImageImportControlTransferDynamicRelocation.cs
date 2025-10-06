using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    [DebuggerDisplay("IATIndex = {IATIndex}, IndirectCall = {IndirectCall}, PageRelativeOffset = {PageRelativeOffset}")]
    public readonly struct ImageImportControlTransferDynamicRelocation : IValue, IViewable
    {
        public int PageRelativeOffset => (int) (flags >> 0) & 0xFFF;
        public bool IndirectCall => ((flags >> 12) & 0x1) != 0;
        public int IATIndex => (int) (flags >> 13) & 0x7FFFF;

        private uint flags => chunk.PeekUInt32(0);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            using (var b = s.WriteBitFields<uint>())
            {
                b.WriteField(nameof(PageRelativeOffset), PageRelativeOffset, 12);
                b.WriteField(nameof(IndirectCall), IndirectCall, 1);
                b.WriteField(nameof(IATIndex), IATIndex, 19);
            }

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
