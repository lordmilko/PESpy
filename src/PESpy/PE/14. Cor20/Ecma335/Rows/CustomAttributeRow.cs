using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, Type = {Type}, Value = {Value}")]
    public readonly struct CustomAttributeRow : IValue, IViewable
    {
        public CustomAttributeIndex RowIndex { get; }

        public CodedIndex Parent => table.GetParent(RowIndex);

        //While this property is called Type as per II.22.10,
        //it's really a MethodDef or MemberRef describing
        //the constructor of the attribute
        public CodedIndex Type => table.GetType(RowIndex);

        public BlobIndex Value => table.GetValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly CustomAttributeTable table;

        internal CustomAttributeRow(CustomAttributeIndex index, CustomAttributeTable table)
        {
            //II.22.10

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CustomAttributeRow, this, ViewKind.Metadata_CustomAttributeRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteHasCustomAttributeIndex(nameof(Parent), table.ParentOffset, Parent);
                    break;

                case 1:
                    structWriter.WriteCustomAttributeTypeIndex(nameof(Type), table.TypeOffset, Type);
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
