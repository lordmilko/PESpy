using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly struct GenericParamConstraintRow : IValue, IViewable
    {
        public GenericParamConstraintIndex RowIndex { get; }

        public GenericParamIndex Owner => table.GetOwner(RowIndex);

        public CodedIndex Constraint => table.GetConstraint(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public GenericParamRow OwnerRow => table.CompressedModelHeap.GenericParamTable[Owner];

        public object ConstraintRow => Constraint.GetRow(table.CompressedModelHeap);

        private readonly GenericParamConstraintTable table;

        internal GenericParamConstraintRow(GenericParamConstraintIndex index, GenericParamConstraintTable table)
        {
            //II.22.21

            RowIndex = index;
            this.table = table;
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_GenericParamConstraintRow, table.RowSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteSimpleIndex(nameof(Owner), table.OwnerOffset, (int) Owner, TableKind.GenericParam);
                    break;

                case 1:
                    structWriter.WriteTypeDefOrRefIndex(nameof(Constraint), table.ConstraintOffset, Constraint);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            using var builder = new ValueStringBuilder();

            builder.Append(table.CompressedModelHeap.GenericParamTable[Owner].Name.GetString().AsSpan());
            builder.Append(" is ");
            builder.Append(Constraint.GetRow(table.CompressedModelHeap).ToString());

            return builder.ToString();
        }
    }
}
