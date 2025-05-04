using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Flags = {Flags}, TypeDefId = {TypeDefId}, TypeName = {TypeName.ToString(),nq}, TypeNamespace = {TypeNamespace.ToString(),nq}, Implementation = {Implementation}")]
    public readonly struct ExportedTypeRow : IValue, IViewable
    {
        public ExportedTypeIndex RowIndex { get; }

        public CorTypeAttr Flags => table.GetFlags(RowIndex);

        public int TypeDefId => table.GetTypeDefId(RowIndex);

        public StringIndex TypeName => table.GetTypeName(RowIndex);

        public StringIndex TypeNamespace => table.GetTypeNamespace(RowIndex);

        public int Implementation => table.GetImplementation(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ExportedTypeTable table;

        internal ExportedTypeRow(ExportedTypeIndex index, ExportedTypeTable table)
        {
            //II.22.14

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ExportedType Row", this, ViewKind.Metadata_ExportedTypeRow);

            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteSimpleIndex(nameof(TypeDefId), TypeDefId, TableKind.TypeDef);
            s.WriteStringHeapIndex(nameof(TypeName), TypeName);
            s.WriteStringHeapIndex(nameof(TypeNamespace), TypeNamespace);
            s.WriteImplementationIndex(nameof(Implementation), Implementation);
        }
    }
}
