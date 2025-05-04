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

        public int Method => table.GetMethod(RowIndex);

        public int Association => table.GetAssociation(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly MethodSemanticsTable table;

        internal MethodSemanticsRow(MethodSemanticsIndex index, MethodSemanticsTable table)
        {
            //II.22.28

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MethodSemantics Row", this, ViewKind.Metadata_MethodSemanticsRow);

            s.WriteValue(nameof(Semantics), Semantics, sizeof(short));
            s.WriteSimpleIndex(nameof(Method), Method, TableKind.MethodDef);
            s.WriteHasSemanticsIndex(nameof(Association), Association);
        }
    }
}
