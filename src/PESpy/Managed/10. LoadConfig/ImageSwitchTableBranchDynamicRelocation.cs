using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageSwitchTableBranchDynamicRelocation : IValue, IViewable
    {
        public short PageRelativeOffset => (short) (flags & 0xFFF);

        public short RegisterNumber => (short) ((flags >> 12) & 0xF);

        public int Offset { get; }

        private readonly ushort flags;

        internal ImageSwitchTableBranchDynamicRelocation(ref FileReader reader)
        {
            Offset = (int) reader.Position;

            flags = reader.ReadUInt16();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_SWITCHTABLE_BRANCH_DYNAMIC_RELOCATION), this, ViewKind.ImageSwitchTableBranchDynamicRelocation);

            using (var b = s.WriteBitFields<ushort>())
            {
                b.WriteField(nameof(PageRelativeOffset), PageRelativeOffset, 12);
                b.WriteField(nameof(RegisterNumber), RegisterNumber, 4);
            }
        }
    }
}
