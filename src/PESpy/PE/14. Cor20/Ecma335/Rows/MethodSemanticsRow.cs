using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Semantics = {Semantics}, Method = {MethodRow}, Association = {AssociationRow}")]
    public readonly struct MethodSemanticsRow : IValue, IViewable
    {
        public MethodSemanticsIndex RowIndex { get; }

        public CorMethodSemanticsAttr Semantics => table.GetSemantics(RowIndex);

        public MethodDefIndex Method => table.GetMethod(RowIndex);

        public CodedIndex Association => table.GetAssociation(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public MethodDefRow MethodRow => table.CompressedModelHeap.MethodDefTable[Method];

        public object AssociationRow => Association.GetRow(table.CompressedModelHeap);

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
            writer.NewStruct(this, ViewKind.Metadata_MethodSemanticsRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Semantics), table.SemanticsOffset, Semantics, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteSimpleIndex(nameof(Method), table.MethodOffset, (int) Method, TableKind.MethodDef);
                    break;

                case 2:
                    structWriter.WriteHasSemanticsIndex(nameof(Association), table.AssociationOffset, Association);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
