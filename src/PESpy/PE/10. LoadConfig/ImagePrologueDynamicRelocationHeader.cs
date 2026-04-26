using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImagePrologueDynamicRelocationHeader : IValue, IViewable
    {
        private const int PrologueByteCountOffset = 0;
        private const int PrologueBytesOffset = 1;

        public byte PrologueByteCount => chunk.PeekByte(PrologueByteCountOffset);

        public NativeSpan<byte> PrologueBytes => chunk.PeekNativeSpan<byte>(PrologueBytesOffset, PrologueByteCount);

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
            writer.NewStruct(this, ViewKind.ImagePrologueDynamicRelocationHeader, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(PrologueByteCount), PrologueByteCountOffset, PrologueByteCount);
                    break;

                case 1:
                    structWriter.WriteField(nameof(PrologueBytes), PrologueBytesOffset, PrologueBytes);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
