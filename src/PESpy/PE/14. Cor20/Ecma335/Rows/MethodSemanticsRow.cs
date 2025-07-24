using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Semantics = {Semantics}, Method = {Method}, Association = {Association}")]
    public readonly struct MethodSemanticsRow : IValue, IViewable
    {
        public MethodSemanticsIndex RowIndex { get; }

        public CorMethodSemanticsAttr Semantics => table.GetSemantics(RowIndex);

        public MethodDefIndex Method => table.GetMethod(RowIndex);

        public Index Association => table.GetAssociation(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly MethodSemanticsTable table;

        internal MethodSemanticsRow(MethodSemanticsIndex index, MethodSemanticsTable table)
        {
            //II.22.28

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MethodSemanticsRow, this, ViewKind.Metadata_MethodSemanticsRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Semantics), Semantics, sizeof(short));
            s.WriteSimpleIndex(nameof(Method), (int) Method, TableKind.MethodDef);
            s.WriteHasSemanticsIndex(nameof(Association), (int) Association);

            return s.ToArray();
        }
    }
}
