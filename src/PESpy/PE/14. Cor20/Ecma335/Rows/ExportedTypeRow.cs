using System.Diagnostics;
using ClrDebug;
using PESpy.View;

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

        public Index Implementation => table.GetImplementation(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ExportedTypeTable table;

        internal ExportedTypeRow(ExportedTypeIndex index, ExportedTypeTable table)
        {
            //II.22.14

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ExportedTypeRow, this, ViewKind.Metadata_ExportedTypeRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteSimpleIndex(nameof(TypeDefId), TypeDefId, TableKind.TypeDef);
            s.WriteStringHeapIndex(nameof(TypeName), TypeName);
            s.WriteStringHeapIndex(nameof(TypeNamespace), TypeNamespace);
            s.WriteImplementationIndex(nameof(Implementation), (int) Implementation);

            return s.ToArray();
        }
    }
}
