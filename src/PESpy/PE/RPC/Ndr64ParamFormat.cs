using System.Diagnostics;

namespace PESpy
{
    //NDR64_PARAM_FORMAT
    public readonly struct Ndr64ParamFormat
    {
        private const int TypeOffset = 0;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int AttributesOffset => chunk.PointerSize;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int ReservedOffset => 2 + chunk.PointerSize;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int StackOffsetOffset => 4 + chunk.PointerSize;

        public long Type => (long) chunk.PeekPointer(TypeOffset);

        public NDR64_PARAM_FLAGS Attributes => chunk.PeekUInt16(AttributesOffset);

        public short Reserved => chunk.PeekInt16(ReservedOffset);

        public int StackOffset => chunk.PeekInt32(StackOffsetOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal static int StructSize(bool is32Bit) =>
            (is32Bit ? 4 : 8) + //Type
            sizeof(short) + //Attributes
            sizeof(short) + //Reserved
            sizeof(int); //StackOffset

        private readonly MemoryChunk chunk;

        internal Ndr64ParamFormat(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
