using System.Diagnostics;
using PESpy.View;

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.LocalVariableRow, this, ViewKind.PortablePdb_LocalVariableRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Attributes), Attributes, sizeof(int));
            s.WriteValue(nameof(RowIndex), Index);
            s.WriteStringHeapIndex(nameof(Name), Name);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
