using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Method = {Method}, ImportScope = {ImportScope}, VariableList = {VariableList}, ConstantList = {ConstantList}, StartOffset = {StartOffset}, Length = {Length}")]
    public readonly struct LocalScopeRow : IValue, IViewable
    {
        public LocalScopeIndex RowIndex { get; }

        public int Method => table.GetMethod(RowIndex);

        public int ImportScope => table.GetImportScope(RowIndex);

        public int VariableList => table.GetVariableList(RowIndex);

        public int ConstantList => table.GetConstantList(RowIndex);

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
