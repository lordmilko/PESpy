using System;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents a blob of bytes read from a file.
    /// </summary>
    public readonly struct ByteBlob : IValue, IViewable  //Small enough that returning a copy from properties is OK
    {
        private const int BytesOffset = 0;

        public NativeSpan<byte> Bytes => chunk.PeekNativeSpan<byte>(BytesOffset, length);

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private readonly int length;
        internal readonly ViewKind viewKind;

        internal ByteBlob(in MemoryChunk chunk, int length, ViewKind viewKind)
        {
            this.chunk = chunk;
            this.length = length;
            this.viewKind = viewKind;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.WriteByteBlob(this);

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();
    }
}
