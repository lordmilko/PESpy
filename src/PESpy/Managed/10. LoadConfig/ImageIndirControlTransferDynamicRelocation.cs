using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageIndirControlTransferDynamicRelocation : IValue, IViewable
    {
        public short PageRelativeOffset => (short) ((flags >> 0) & 0xFFF);
        public bool IndirectCall        => ((flags >> 12) & 0x1) != 0;
        public bool RexWPrefix          => ((flags >> 13) & 0x1) != 0;
        public bool CfgCheck            => ((flags >> 14) & 0x1) != 0;
        public bool Reserved            => ((flags >> 15) & 0x1) != 0;

        public int Offset { get; }

        private readonly ushort flags;

        internal ImageIndirControlTransferDynamicRelocation(ref FileReader reader)
        {
            Offset = (int) reader.Position;

            flags = reader.ReadUInt16();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_INDIR_CONTROL_TRANSFER_DYNAMIC_RELOCATION), this, ViewKind.ImageIndirControlTransferDynamicRelocation);

            using (var b = s.WriteBitFields<ushort>())
            {
                b.WriteField(nameof(PageRelativeOffset), PageRelativeOffset, 12);
                b.WriteField(nameof(IndirectCall), IndirectCall, 1);
                b.WriteField(nameof(RexWPrefix), RexWPrefix, 1);
                b.WriteField(nameof(CfgCheck), CfgCheck, 1);
                b.WriteField(nameof(Reserved), Reserved, 1);
            }
        }
    }
}
