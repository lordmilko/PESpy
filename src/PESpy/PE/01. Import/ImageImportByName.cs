using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_IMPORT_BY_NAME"/> structure.
    /// </summary>
    public readonly struct ImageImportByName : IValue, IViewable
    {
        //Used to index into the "export name pointer table". If that fails, a full search is done to resolve the import
        //at runtime
        public short Hint => chunk.PeekInt16(0);

        public AnsiString Name => chunk.PeekAnsiNullTerminatedString(2);

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(short) +
            Name.Length + 1;

        private readonly MemoryChunk chunk;

        internal ImageImportByName(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

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
