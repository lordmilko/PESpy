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

        public int Parent => table.GetParent(RowIndex);

        public BlobIndex Value => table.GetValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ConstantTable table;

        internal ConstantRow(ConstantIndex index, ConstantTable table)
        {
            //II.22.9

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("Constant Row", this, ViewKind.Metadata_ConstantRow);

            s.WriteValue(nameof(Type), Type, sizeof(byte));
            s.WriteValue(nameof(Padding), Padding);
            s.WriteHasConstantIndex(nameof(Parent), Parent);
            s.WriteBlobHeapIndex(nameof(Value), Value);
        }
    }
}
