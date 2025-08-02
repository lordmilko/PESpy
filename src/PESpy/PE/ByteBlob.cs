using System;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents a blob of bytes read from a file.
    /// </summary>
    public readonly struct ByteBlob : IValue, IViewable  //Small enough that returning a copy from properties is OK
    {
        public NativeSpan<byte> Bytes => chunk.PeekNativeSpan<byte>(0, length);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private readonly int length;

        internal ByteBlob(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;
            this.length = length;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.WriteByteBlob(this);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();
    }
}
