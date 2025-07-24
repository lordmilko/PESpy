using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_IMPORT_BY_NAME"/> structure.
    /// </summary>
    public readonly struct ImageImportByName : IValue, IViewable
    {
#if PEFAST
        public short Hint => chunk.PeekInt16(0);
#else
        public short Hint { get; init; }
#endif

#if PEFAST
        public AnsiString Name => chunk.PeekAnsiNullTerminatedString(2);
#else
        public string Name { get; init; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal int StructSize =>
            sizeof(short) +
            Name.Length + 1;

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageImportByName(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ImageImportByName(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            //Used to index into the "export name pointer table". If that fails, a full search is done to resolve the import
            //at runtime
            Hint = reader.ReadInt16();
            Name = reader.ReadAnsiNullTerminatedString();
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_IMPORT_BY_NAME, this, ViewKind.ImageImportByName, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Hint), Hint);
            s.WriteAnsiNullTerminatedField(nameof(Name), Name);

            return s.ToArray();
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
