using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, Kind = {Kind}, Value = {Value}")]
    public readonly struct CustomDebugInformationRow : IValue, IViewable
    {
        public CustomDebugInformationIndex RowIndex { get; }

        public Index Parent => table.GetParent(RowIndex);

        public GuidIndex Kind => table.GetKind(RowIndex);

        public BlobIndex Value => table.GetValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly CustomDebugInformationTable table;

        internal CustomDebugInformationRow(CustomDebugInformationIndex index, CustomDebugInformationTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CustomDebugInformationRow, this, ViewKind.PortablePdb_CustomDebugInformationRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteHasCustomDebugInformationIndex(nameof(Parent), table.ParentOffset, (int) Parent);
                    break;

                case 1:
                    structWriter.WriteGuidHeapIndex(nameof(Kind), table.KindOffset, Kind);
                    break;

                case 2:
                    structWriter.WriteBlobHeapIndex(nameof(Value), table.ValueOffset, Value);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
