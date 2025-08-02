using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Method = {Method}, ImportScope = {ImportScope}, VariableList = {VariableList}, ConstantList = {ConstantList}, StartOffset = {StartOffset}, Length = {Length}")]
    public readonly struct LocalScopeRow : IValue, IViewable
    {
        public LocalScopeIndex RowIndex { get; }

        public MethodDefIndex Method => table.GetMethod(RowIndex);

        public ImportScopeIndex ImportScope => table.GetImportScope(RowIndex);

        public LocalVariableIndex VariableList => table.GetVariableList(RowIndex);

        public LocalConstantIndex ConstantList => table.GetConstantList(RowIndex);

        public uint StartOffset => table.GetStartOffset(RowIndex);

        public int Length => table.GetLength(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly LocalScopeTable table;

        internal LocalScopeRow(LocalScopeIndex index, LocalScopeTable table)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#localscope-table-0x32

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.LocalScopeRow, this, ViewKind.PortablePdb_LocalScopeRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Method), (int) Method);
            s.WriteValue(nameof(ImportScope), (int) ImportScope);
            s.WriteValue(nameof(VariableList), (int) VariableList);
            s.WriteValue(nameof(ConstantList), (int) ConstantList);
            s.WriteValue(nameof(StartOffset), StartOffset);
            s.WriteValue(nameof(Length), Length);

            return s.ToArray();
        }
    }
}
