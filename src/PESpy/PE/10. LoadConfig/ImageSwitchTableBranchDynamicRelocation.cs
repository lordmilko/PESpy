using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageSwitchTableBranchDynamicRelocation : IValue, IViewable
    {
        public short PageRelativeOffset => (short) (flags & 0xFFF);

        public short RegisterNumber => (short) ((flags >> 12) & 0xF);

        private ushort flags => chunk.PeekUInt16(0);

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
            writer.NewStruct(Strings.IMAGE_SWITCHTABLE_BRANCH_DYNAMIC_RELOCATION, this, ViewKind.ImageSwitchTableBranchDynamicRelocation, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            using (var b = s.WriteBitFields<ushort>())
            {
                b.WriteField(nameof(PageRelativeOffset), PageRelativeOffset, 12);
                b.WriteField(nameof(RegisterNumber), RegisterNumber, 4);
            }

            return s.ToArray();
        }
    }
}
