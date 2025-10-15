using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("NestedClass = {NestedClass}, EnclosingClass = {EnclosingClass}")]
    public readonly struct NestedClassRow : IValue, IViewable
    {
        public NestedClassIndex RowIndex { get; }

        public TypeDefIndex NestedClass => table.GetNestedClass(RowIndex);

        public TypeDefIndex EnclosingClass => table.GetEnclosingClass(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

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
            writer.NewStruct(Strings.NestedClassRow, this, ViewKind.Metadata_NestedClassRow, table.RowSize);

        int IViewable.NumChildren => 2;

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
