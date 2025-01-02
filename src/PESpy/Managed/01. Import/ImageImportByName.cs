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
        public short Hint { get; init; }

        public string Name { get; init; }

        public RawOffset Offset { get; }

        internal ImageImportByName(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            //Used to index into the "export name pointer table". If that fails, a full search is done to resolve the import
            //at runtime
            Hint = reader.ReadInt16();
            Name = reader.ReadAnsiNullTerminatedString();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_IMPORT_BY_NAME), this, ViewKind.ImageImportByName);

            s.WriteField(nameof(Hint), Hint);
            s.WriteAnsiNullTerminatedField(nameof(Name), Name);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
