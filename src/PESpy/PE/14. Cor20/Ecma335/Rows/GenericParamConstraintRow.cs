using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Owner = {Owner}, Constraint = {Constraint}")]
    public readonly struct GenericParamConstraintRow : IValue, IViewable
    {
        public GenericParamConstraintIndex RowIndex { get; }

        public GenericParamIndex Owner => table.GetOwner(RowIndex);

        public Index Constraint => table.GetConstraint(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly GenericParamConstraintTable table;

        internal GenericParamConstraintRow(GenericParamConstraintIndex index, GenericParamConstraintTable table)
        {
            //II.22.21

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.GenericParamConstraintRow, this, ViewKind.Metadata_GenericParamConstraintRow, table.RowSize);

        int IViewable.NumChildren => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteSimpleIndex(nameof(Owner), table.OwnerOffset, (int) Owner, TableKind.GenericParam);
                    break;

                case 1:
                    structWriter.WriteTypeDefOrRefIndex(nameof(Constraint), table.ConstraintOffset, (int) Constraint);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
