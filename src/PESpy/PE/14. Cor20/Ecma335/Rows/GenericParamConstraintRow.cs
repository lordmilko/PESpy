using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteSimpleIndex(nameof(Owner), (int) Owner, TableKind.GenericParam);
            s.WriteTypeDefOrRefIndex(nameof(Constraint), (int) Constraint);

            return s.ToArray();
        }
    }
}
