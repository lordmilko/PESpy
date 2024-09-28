using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    public readonly struct LocalScopeRow : IValue, IViewable
    {
        public int Method { get; init; }

        public int ImportScope { get; init; }

        public int VariableList { get; init; }

        public int ConstantList { get; init; }

        public uint StartOffset { get; init; }

        public uint Length { get; init; }

        public RawOffset Offset { get; }

        internal static LocalScopeRow New(MetadataReader metadataReader) => new LocalScopeRow(metadataReader);

        internal static int GetRowSize(MetadataReader metadataReader) =>
            sizeof(int) + //Method
            sizeof(int) + //ImportScope
            sizeof(int) + //VariableList
            sizeof(int) + //ConstantList
            sizeof(int) + //StartOffset
            sizeof(int);  //Length

        internal LocalScopeRow(MetadataReader metadataReader)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#localscope-table-0x32

            Offset = (RawOffset) metadataReader.Position;

            Method = metadataReader.ReadInt32();
            ImportScope = metadataReader.ReadInt32();
            VariableList = metadataReader.ReadInt32();
            ConstantList = metadataReader.ReadInt32();
            StartOffset = metadataReader.ReadUInt32();
            Length = metadataReader.ReadUInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("LocalScope Row", this, ViewKind.PortablePdb_LocalScopeRow);

            s.WriteValue(nameof(Method), Method);
            s.WriteValue(nameof(ImportScope), ImportScope);
            s.WriteValue(nameof(VariableList), VariableList);
            s.WriteValue(nameof(ConstantList), ConstantList);
            s.WriteValue(nameof(StartOffset), StartOffset);
            s.WriteValue(nameof(Length), Length);
        }
    }
}
