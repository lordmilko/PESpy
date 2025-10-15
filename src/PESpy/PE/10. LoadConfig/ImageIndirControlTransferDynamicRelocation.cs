using System;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageIndirControlTransferDynamicRelocation : IValue, IViewable
    {
        private const int flagsOffset = 0;

        public short PageRelativeOffset => (short) ((flags >> 0) & 0xFFF);
        public bool IndirectCall        => ((flags >> 12) & 0x1) != 0;
        public bool RexWPrefix          => ((flags >> 13) & 0x1) != 0;
        public bool CfgCheck            => ((flags >> 14) & 0x1) != 0;
        public bool Reserved            => ((flags >> 15) & 0x1) != 0;

        private ushort flags => chunk.PeekUInt16(flagsOffset);

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

        int IViewable.NumChildren => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteBitField(nameof(PageRelativeOffset), flagsOffset, PageRelativeOffset, sizeof(ushort), 12);
                    break;

                case 1:
                    structWriter.WriteBitField(nameof(IndirectCall), flagsOffset, IndirectCall, sizeof(ushort), 1);
                    break;

                case 2:
                    structWriter.WriteBitField(nameof(RexWPrefix), flagsOffset, RexWPrefix, sizeof(ushort), 1);
                    break;

                case 3:
                    structWriter.WriteBitField(nameof(CfgCheck), flagsOffset, CfgCheck, sizeof(ushort), 1);
                    break;

                case 4:
                    structWriter.WriteBitField(nameof(Reserved), flagsOffset, Reserved, sizeof(ushort), 1);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
