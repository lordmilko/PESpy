using System;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImagePrologueDynamicRelocationHeader : IValue, IViewable
    {
#if PEFAST
        public byte PrologueByteCount => chunk.PeekByte(0);
#else
        public int PrologueByteCount { get; }
#endif

#if PEFAST
        public NativeSpan<byte> PrologueBytes => chunk.PeekNativeSpan<byte>(1, PrologueByteCount);
#else
        public byte[] PrologueBytes { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImagePrologueDynamicRelocationHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ImagePrologueDynamicRelocationHeader(IFileReader reader)
        {
            Offset = (int) reader.Position;

            PrologueByteCount = reader.ReadByte();
            PrologueBytes = reader.ReadBytes(PrologueByteCount);
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_PROLOGUE_DYNAMIC_RELOCATION_HEADER), this, ViewKind.ImagePrologueDynamicRelocationHeader);

            s.WriteField(nameof(PrologueByteCount), PrologueByteCount);
            s.WriteField(nameof(PrologueBytes), PrologueBytes);
        }
    }
}
