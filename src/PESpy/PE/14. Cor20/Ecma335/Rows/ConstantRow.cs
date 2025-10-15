using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Type = {Type}, Parent = {Parent}, Value = {Value}")]
    public readonly struct ConstantRow : IValue, IViewable
    {
        public ConstantIndex RowIndex { get; }

        public CorElementType Type => table.GetType(RowIndex);

        public byte Padding => table.GetPadding(RowIndex);

        public Index Parent => table.GetParent(RowIndex);

        public BlobIndex Value => table.GetValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ConstantTable table;

        internal ConstantRow(ConstantIndex index, ConstantTable table)
        {
            //II.22.9

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ConstantRow, this, ViewKind.Metadata_ConstantRow, table.RowSize);

        int IViewable.NumChildren => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Type), table.TypeOffset, Type, sizeof(byte));
                    break;

                case 1:
                    structWriter.WriteField(nameof(Padding), table.PaddingOffset, Padding);
                    break;

                case 2:
                    structWriter.WriteHasConstantIndex(nameof(Parent), table.ParentOffset, (int) Parent);
                    break;

                case 3:
                    structWriter.WriteBlobHeapIndex(nameof(Value), table.ValueOffset, Value);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
