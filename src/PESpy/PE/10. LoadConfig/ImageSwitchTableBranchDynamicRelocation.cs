using System;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageSwitchTableBranchDynamicRelocation : IValue, IViewable
    {
        private const int flagsOffset = 0;

        public short PageRelativeOffset => (short) (flags & 0xFFF);

        public short RegisterNumber => (short) ((flags >> 12) & 0xF);

        private ushort flags => chunk.PeekUInt16(flagsOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short);

        private readonly MemoryChunk chunk;

        internal ImageSwitchTableBranchDynamicRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageSwitchTableBranchDynamicRelocation, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteBitField(nameof(PageRelativeOffset), flagsOffset, PageRelativeOffset, sizeof(short), 12);
                    break;

                case 1:
                    structWriter.WriteBitField(nameof(RegisterNumber), flagsOffset, RegisterNumber, sizeof(short), 4);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
