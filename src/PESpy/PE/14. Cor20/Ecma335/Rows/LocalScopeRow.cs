using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Method = {MethodRow}, ImportScope = {ImportScopeRow}, VariableList = {VariableList}, ConstantList = {ConstantList}, StartOffset = {StartOffset}, Length = {Length}")]
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

        //Extensions
        public LocalVariableList Variables => new LocalVariableList(RowIndex, table.CompressedModelHeap);

        public LocalConstantList Constants => new LocalConstantList(RowIndex, table.CompressedModelHeap);

        public ChildScopeList Children => new ChildScopeList(RowIndex, table);

        public MethodDebugInformationRow MethodRow => table.CompressedModelHeap.MethodDebugInformationTable[Method];

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

        int IViewable.NumChildren() => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Method), table.MethodOffset, (int) Method);
                    break;

                case 1:
                    structWriter.WriteField(nameof(ImportScope), table.ImportScopeOffset, (int) ImportScope);
                    break;

                case 2:
                    structWriter.WriteField(nameof(VariableList), table.VariableListOffset, (int) VariableList);
                    break;

                case 3:
                    structWriter.WriteField(nameof(ConstantList), table.ConstantListOffset, (int) ConstantList);
                    break;

                case 4:
                    structWriter.WriteField(nameof(StartOffset), table.StartOffsetOffset, StartOffset);
                    break;

                case 5:
                    structWriter.WriteField(nameof(Length), table.LengthOffset, Length);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
