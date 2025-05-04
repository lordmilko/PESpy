using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Attributes = {Attributes}, Index = {Index}, Name = {Name.ToString(),nq}")]
    public readonly struct LocalVariableRow : IValue, IViewable
    {
        public LocalVariableIndex RowIndex { get; }

        public LocalVariableAttributes Attributes => table.GetAttributes(RowIndex);

        public uint Index => table.GetIndex(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly LocalVariableTable table;

        internal LocalVariableRow(LocalVariableIndex index, LocalVariableTable table)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#localvariable-table-0x33

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("LocalVariable Row", this, ViewKind.PortablePdb_LocalVariableRow);

            s.WriteValue(nameof(Attributes), Attributes, sizeof(int));
            s.WriteValue(nameof(RowIndex), Index);
            s.WriteStringHeapIndex(nameof(Name), Name);
        }
    }
}
