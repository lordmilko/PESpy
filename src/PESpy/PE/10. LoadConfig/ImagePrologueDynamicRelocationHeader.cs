using System;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImagePrologueDynamicRelocationHeader : IValue, IViewable
    {
        public byte PrologueByteCount => chunk.PeekByte(0);

        public NativeSpan<byte> PrologueBytes => chunk.PeekNativeSpan<byte>(1, PrologueByteCount);

        public int Offset => chunk.AbsoluteOffset;
        internal int StructSize =>
            sizeof(byte) + //PrologueByteCount
            PrologueByteCount; //PrologueBytes

        private readonly MemoryChunk chunk;

        internal ImagePrologueDynamicRelocationHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_PROLOGUE_DYNAMIC_RELOCATION_HEADER, this, ViewKind.ImagePrologueDynamicRelocationHeader, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(PrologueByteCount), PrologueByteCount);
            s.WriteField(nameof(PrologueBytes), PrologueBytes);

            return s.ToArray();
        }
    }
}
