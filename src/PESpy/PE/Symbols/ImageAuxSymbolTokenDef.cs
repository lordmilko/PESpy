using System;

namespace PESpy
{
    //IMAGE_AUX_SYMBOL_TOKEN_DEF
    public readonly struct ImageAuxSymbolTokenDef : IValue
    {
        public ImageAuxSymbolType bAuxType => (ImageAuxSymbolType) chunk.PeekByte(0);

        public byte bReserved => chunk.PeekByte(1);

        public int SymbolTableIndex => chunk.PeekInt32(2);

        public NativeSpan<byte> rgbReserved => chunk.PeekNativeSpan<byte>(6, 14);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ImageAuxSymbolTokenDef(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
