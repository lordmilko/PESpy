using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {ParentRow}, NativeType = {NativeType}")]
    public readonly struct FieldMarshalRow : IValue, IViewable
    {
        public FieldMarshalIndex RowIndex { get; }

        public CodedIndex Parent => table.GetParent(RowIndex);

        //System.Reflection.Metadata doesn't seem to parse this? But ILLink does
        public BlobIndex NativeType => table.GetNativeType(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object ParentRow => Parent.GetRow(table.CompressedModelHeap);
        
        private readonly FieldMarshalTable table;

        internal FieldMarshalRow(FieldMarshalIndex index, FieldMarshalTable table)
        {
            //II.22.17

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FieldMarshalRow, this, ViewKind.Metadata_FieldMarshalRow, table.RowSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteHasFieldMarshalIndex(nameof(Parent), table.ParentOffset, Parent);
                    break;

                case 1:
                    structWriter.WriteBlobHeapIndex(nameof(NativeType), table.NativeTypeOffset, NativeType);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
