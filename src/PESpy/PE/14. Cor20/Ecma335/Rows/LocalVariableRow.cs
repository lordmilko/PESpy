using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct LocalVariableRow : IValue, IViewable
    {
        public LocalVariableAttributes Attributes { get; }

        public uint Index { get; }

        public int Name { get; }

        public RawOffset Offset { get; }

        internal static LocalVariableRow New(MetadataReader metadataReader) => new LocalVariableRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) +                   //Attributes
            sizeof(int) +                   //Index
            metadataReader.StringIndexSize; //Name

        internal LocalVariableRow(MetadataReader metadataReader)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#localvariable-table-0x33

            Offset = (RawOffset) metadataReader.Position;

            Attributes = (LocalVariableAttributes) metadataReader.ReadInt32();
            Index = metadataReader.ReadUInt32();
            Name = metadataReader.ReadStringHeapIndex();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("LocalVariable Row", this, ViewKind.PortablePdb_LocalVariableRow);

            s.WriteValue(nameof(Attributes), Attributes, sizeof(int));
            s.WriteValue(nameof(Index), Index);
            s.WriteStringHeapIndex(nameof(Name), Name);
        }
    }
}
