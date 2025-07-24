using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents a Profile Guided Optimization entry. This type does not have a well-known native struct declaration.
    /// </summary>
    public readonly struct PogoItem : IValue, IViewable
    {
#if PEFAST
        public int RVA => chunk.PeekInt32(0);
#else
        public int RVA { get; }
#endif

#if PEFAST
        public int Size => chunk.PeekInt32(4);
#else
        public int Size { get; }
#endif

#if PEFAST
        public AnsiString Name => chunk.PeekAnsiNullTerminatedString(8);
#else
        public string Name { get; }
#endif

        internal const int FixedStructSize =
            sizeof(int) + //RVA
            sizeof(int); //Size

        internal int StructSize =>
            FixedStructSize +
            Name.Length + 1; //Name

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;

        internal PogoItem(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal PogoItem(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            RVA = reader.ReadInt32();
            Size = reader.ReadInt32();
            Name = reader.ReadAnsiNullTerminatedString();

            //Each entry should be aligned to 4 bytes
            var alignmentTarget = (reader.Position + 3) & ~3;

            while (reader.Position < alignmentTarget)
                reader.ReadByte();
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.PogoItem, this, ViewKind.PogoItem, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(RVA), RVA);
            s.WriteField(nameof(Size), Size);
            s.WriteAnsiNullTerminatedField(nameof(Name), Name);

            return s.ToArray();
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
