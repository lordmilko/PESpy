using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ClassLayout Row", this, ViewKind.Metadata_ClassLayoutRow);

            s.WriteValue(nameof(PackingSize), PackingSize);
            s.WriteValue(nameof(ClassSize), ClassSize);
            s.WriteSimpleIndex(nameof(Parent), (int) Parent, TableKind.TypeDef);
        }
    }
}
