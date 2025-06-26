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
            writer.NewStruct("NestedClass Row", this, ViewKind.Metadata_NestedClassRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteSimpleIndex(nameof(NestedClass), (int) NestedClass, TableKind.TypeDef);
            s.WriteSimpleIndex(nameof(EnclosingClass), (int) EnclosingClass, TableKind.TypeDef);

            return s.ToArray();
        }
    }
}
