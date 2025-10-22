using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("PackingSize = {PackingSize}, ClassSize = {ClassSize}, Parent = {Parent}")]
    public readonly struct ClassLayoutRow : IValue, IViewable
    {
        public ClassLayoutIndex RowIndex { get; }

        public short PackingSize => table.GetPackingSize(RowIndex);

        public int ClassSize => table.GetClassSize(RowIndex);

        public TypeDefIndex Parent => table.GetParent(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ClassLayoutTable table;

        internal ClassLayoutRow(ClassLayoutIndex index, ClassLayoutTable table)
        {
            //II.22.8

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ClassLayoutRow, this, ViewKind.Metadata_ClassLayoutRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(PackingSize), table.PackingSizeOffset, PackingSize);
                    break;

                case 1:
                    structWriter.WriteField(nameof(ClassSize), table.ClassSizeOffset, ClassSize);
                    break;

                case 2:
                    structWriter.WriteSimpleIndex(nameof(Parent), table.ParentOffset, (int) Parent, TableKind.TypeDef);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
