using System;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageDynamicRelocationV2 : IValue
    {
        public int HeaderSize => chunk.PeekInt32(0);

        public int FixupInfoSize => chunk.PeekInt32(4);

        public ulong Symbol => chunk.PeekPointer(8);

        public int SymbolGroup => chunk.PeekInt32(8 + chunk.PointerSize);

        public int Flags => chunk.PeekInt32(12 + chunk.PointerSize); //todo: enum?

        // ...     variable length header fields
        // BYTE    FixupInfo[FixupInfoSize]
        public NativeSpan<byte> FixupInfo => chunk.PeekNativeSpan<byte>(16 + chunk.PointerSize, FixupInfoSize);

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ImageDynamicRelocationV2(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
