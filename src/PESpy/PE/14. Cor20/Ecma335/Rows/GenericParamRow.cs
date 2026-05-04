using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly struct GenericParamRow : IValue, IViewable
    {
        public GenericParamIndex RowIndex { get; }

        public short Number => table.GetNumber(RowIndex);

        public CorGenericParamAttr Flags => table.GetFlags(RowIndex);

        public CodedIndex Owner => table.GetOwner(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object OwnerRow => Owner.GetRow(table.CompressedModelHeap);

        private readonly GenericParamTable table;

        internal GenericParamRow(GenericParamIndex index, GenericParamTable table)
        {
            //II.22.20

            RowIndex = index;
            this.table = table;
        }

        public GenericParamConstraintList Constraints => table.CompressedModelHeap.GenericParamConstraintTable.FindConstraintsForGenericParam(RowIndex);

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_GenericParamRow, table.RowSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Number), table.NumberOffset, Number);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(short));
                    break;

                case 2:
                    structWriter.WriteTypeOrMethodDefIndex(nameof(Owner), table.OwnerOffset, Owner);
                    break;

                case 3:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString() => Name.GetString().ToString();
    }
}
