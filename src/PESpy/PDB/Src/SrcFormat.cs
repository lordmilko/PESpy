using System;

namespace PESpy
{
    public readonly struct SrcFormat : IValue
    {
        public Guid language => chunk.PeekGuid(0);

        public Guid languageVendor => chunk.PeekGuid(16);

        public Guid documentType => chunk.PeekGuid(32);

        public Guid algorithmId => chunk.PeekGuid(48);

        public int checkSumSize => chunk.PeekInt32(64);

        public int sourceSize => chunk.PeekInt32(68);

        public NativeSpan<byte> checkSum => chunk.PeekNativeSpan<byte>(FixedStructSize, checkSumSize);

        //Note that depending on the type of compression being described on the SrcHeaderOut, these bytes may
        //or may not be plain text
        public NativeSpan<byte> source => chunk.PeekNativeSpan<byte>(FixedStructSize + checkSumSize, sourceSize);

        public long Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            16 + //language
            16 + //languageVendor
            16 + //documentType
            16 + //algorithmId
            4 + //checkSumSize
            4; //sourceSize

        private readonly MemoryChunk chunk;

        internal SrcFormat(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
