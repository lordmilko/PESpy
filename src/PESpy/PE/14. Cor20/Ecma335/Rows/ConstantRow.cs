using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Type = {Type}, Parent = {Parent}, Value = {Value}")]
    public readonly struct ConstantRow : IValue, IViewable
    {
        public ConstantIndex RowIndex { get; }

        public CorElementType Type => table.GetType(RowIndex);

        public byte Padding => table.GetPadding(RowIndex);

        public Index Parent => table.GetParent(RowIndex);

        public BlobIndex Value => table.GetValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ConstantTable table;

        internal ConstantRow(ConstantIndex index, ConstantTable table)
        {
            //II.22.9

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ConstantRow, this, ViewKind.Metadata_ConstantRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Type), Type, sizeof(byte));
            s.WriteValue(nameof(Padding), Padding);
            s.WriteHasConstantIndex(nameof(Parent), (int) Parent);
            s.WriteBlobHeapIndex(nameof(Value), Value);

            return s.ToArray();
        }
    }
}
