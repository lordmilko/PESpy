using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("NestedClass = {NestedClass}, EnclosingClass = {EnclosingClass}")]
    public readonly struct NestedClassRow : IValue, IViewable
    {
        public NestedClassIndex RowIndex { get; }

        public int NestedClass => table.GetNestedClass(RowIndex);

        public int EnclosingClass => table.GetEnclosingClass(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly NestedClassTable table;

        internal NestedClassRow(NestedClassIndex index, NestedClassTable table)
        {
            //II.22.32

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("NestedClass Row", this, ViewKind.Metadata_NestedClassRow);

            s.WriteSimpleIndex(nameof(NestedClass), NestedClass, TableKind.TypeDef);
            s.WriteSimpleIndex(nameof(EnclosingClass), EnclosingClass, TableKind.TypeDef);
        }
    }
}
