using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageSwitchTableBranchDynamicRelocation : IValue, IViewable
    {
        public short PageRelativeOffset => (short) (flags & 0xFFF);

        public short RegisterNumber => (short) ((flags >> 12) & 0xF);

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

#if PEFAST
        private ushort flags => chunk.PeekUInt16(0);
#else
        private readonly ushort flags;
#endif

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageSwitchTableBranchDynamicRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ImageSwitchTableBranchDynamicRelocation(IFileReader reader)
        {
            Offset = (int) reader.Position;

            flags = reader.ReadUInt16();
        }
#endif

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
