using System;
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
            writer.NewStruct(this, ViewKind.PortablePdb_LocalVariableRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Attributes), table.AttributesOffset, Attributes, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteField(nameof(Index), table.IndexOffset, Index);
                    break;

                case 2:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString() => Name.GetString().ToString();
    }
}
