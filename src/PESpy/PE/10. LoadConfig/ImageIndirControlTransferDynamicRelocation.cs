using System.Diagnostics;
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

        private ushort flags => chunk.PeekUInt16(0);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short);

        private readonly MemoryChunk chunk;

        internal ImageIndirControlTransferDynamicRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_INDIR_CONTROL_TRANSFER_DYNAMIC_RELOCATION, this, ViewKind.ImageIndirControlTransferDynamicRelocation, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            using (var b = s.WriteBitFields<ushort>())
            {
                b.WriteField(nameof(PageRelativeOffset), PageRelativeOffset, 12);
                b.WriteField(nameof(IndirectCall), IndirectCall, 1);
                b.WriteField(nameof(RexWPrefix), RexWPrefix, 1);
                b.WriteField(nameof(CfgCheck), CfgCheck, 1);
                b.WriteField(nameof(Reserved), Reserved, 1);
            }

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
