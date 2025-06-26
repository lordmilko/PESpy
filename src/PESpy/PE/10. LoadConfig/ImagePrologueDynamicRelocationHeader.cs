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
        internal int StructSize =>
            sizeof(byte) + //PrologueByteCount
            PrologueByteCount; //PrologueBytes

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(IMAGE_PROLOGUE_DYNAMIC_RELOCATION_HEADER), this, ViewKind.ImagePrologueDynamicRelocationHeader, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(PrologueByteCount), PrologueByteCount);
            s.WriteField(nameof(PrologueBytes), PrologueBytes);

            return s.ToArray();
        }
    }
}
