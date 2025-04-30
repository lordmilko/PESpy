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
        public Span<byte> Hash => chunk.PeekSpan<byte>(4, Size);
#else
        public byte[] Hash { get; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(Reproducible), this, ViewKind.Reproducible);

            s.WriteField(nameof(Size), Size);
            s.WriteField(nameof(Hash), Hash);
        }
    }
}
