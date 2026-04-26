using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {ParentRow}, PropertyList = {PropertyList}")]
    public readonly struct PropertyMapRow : IValue, IViewable
    {
        public PropertyMapIndex RowIndex { get; }

        public TypeDefIndex Parent => table.GetParent(RowIndex);

        public PropertyIndex PropertyList => table.GetPropertyList(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public TypeDefRow ParentRow => table.CompressedModelHeap.TypeDefTable[Parent];

        //PropertyList points to the first property in the list

        private readonly PropertyMapTable table;

        internal PropertyMapRow(PropertyMapIndex index, PropertyMapTable table)
        {
            //II.22.35

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_PropertyMapRow, table.RowSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteSimpleIndex(nameof(Parent), table.ParentOffset, (int) Parent, TableKind.TypeDef);
                    break;

                case 1:
                    structWriter.WriteSimpleIndex(nameof(PropertyList), table.PropertyListOffset, (int) PropertyList, TableKind.Property);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
