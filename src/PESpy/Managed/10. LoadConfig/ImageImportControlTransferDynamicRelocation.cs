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

        public int Offset { get; }

        private readonly uint flags;

        internal ImageImportControlTransferDynamicRelocation(IFileReader reader)
        {
            Offset = (int) reader.Position;

            flags = reader.ReadUInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_IMPORT_CONTROL_TRANSFER_DYNAMIC_RELOCATION), this, ViewKind.ImageImportControlTransferDynamicRelocation);

            using (var b = s.WriteBitFields<uint>())
            {
                b.WriteField(nameof(PageRelativeOffset), PageRelativeOffset, 12);
                b.WriteField(nameof(IndirectCall), IndirectCall, 1);
                b.WriteField(nameof(IATIndex), IATIndex, 19);
            }
        }
    }
}
