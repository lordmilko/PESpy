using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("{EnclosingClassRow.ToString(),nq}+{_NestedClassRow.ToString(),nq}")]
    public readonly struct NestedClassRow : IValue, IViewable
    {
        public NestedClassIndex RowIndex { get; }

        public TypeDefIndex NestedClass => table.GetNestedClass(RowIndex);

        public TypeDefIndex EnclosingClass => table.GetEnclosingClass(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public TypeDefRow _NestedClassRow => table.ModelHeap.TypeDefTable[NestedClass];

        public TypeDefRow EnclosingClassRow => table.ModelHeap.TypeDefTable[EnclosingClass];

        private readonly NestedClassTable table;

        internal NestedClassRow(NestedClassIndex index, NestedClassTable table)
        {
            //II.22.32

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_NestedClassRow, table.RowSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteSimpleIndex(nameof(NestedClass), table.NestedClassOffset, (int) NestedClass, TableKind.TypeDef);
                    break;

                case 1:
                    structWriter.WriteSimpleIndex(nameof(EnclosingClass), table.EnclosingClassOffset, (int) EnclosingClass, TableKind.TypeDef);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
