using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the data contained in the reproducible debug directory.<para/>
    /// Some assemblies may have a reproducible debug directory without having any data in them.<para/>
    /// This type does not have a well-known native struct declaration.
    /// </summary>
    public readonly struct Reproducible : IValue, IViewable
    {
        private const int SizeOffset = 0;
        private const int HashOffset = 4;

        public int Size => chunk.PeekInt32(SizeOffset);

        public NativeSpan<byte> Hash => chunk.PeekNativeSpan<byte>(HashOffset, Size);

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(int) + //Size
            Size; //Hash

        private readonly MemoryChunk chunk;

        internal Reproducible(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.Reproducible, this, ViewKind.Reproducible, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Size), SizeOffset, Size);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Hash), HashOffset, Hash);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
