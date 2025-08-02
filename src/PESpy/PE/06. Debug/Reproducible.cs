using System;
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
        public int Size => chunk.PeekInt32(0);

        public NativeSpan<byte> Hash => chunk.PeekNativeSpan<byte>(4, Size);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Size), Size);
            s.WriteField(nameof(Hash), Hash);

            return s.ToArray();
        }
    }
}
