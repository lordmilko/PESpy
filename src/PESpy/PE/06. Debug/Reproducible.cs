using System;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the data contained in the reproducible debug directory.<para/>
    /// Some assemblies may have a reproducible debug directory without having any data in them.<para/>
    /// This type does not have a well-known native struct declaration.
    /// </summary>
    public readonly struct Reproducible : IValue, IViewable
    {
#if PEFAST
        public int Size => chunk.PeekInt32(0);
#else
        public int Size { get; }
#endif

#if PEFAST
        public NativeSpan<byte> Hash => chunk.PeekNativeSpan<byte>(4, Size);
#else
        public byte[] Hash { get; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal int StructSize =>
            sizeof(int) + //Size
            Size; //Hash

#if PEFAST
        private readonly MemoryChunk chunk;

        internal Reproducible(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal Reproducible(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            Size = reader.ReadInt32();
            Hash = reader.ReadBytes(Size);
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(Reproducible), this, ViewKind.Reproducible, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Size), Size);
            s.WriteField(nameof(Hash), Hash);

            return s.ToArray();
        }
    }
}
