using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Name = {Name.ToString(),nq}, Signature = {Signature}")]
    public readonly struct LocalConstantRow : IValue, IViewable
    {
        public LocalConstantIndex RowIndex { get; }

        public StringIndex Name => table.GetName(RowIndex);

        public BlobIndex Signature => table.GetSignature(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly LocalConstantTable table;

        internal LocalConstantRow(LocalConstantIndex index, LocalConstantTable table)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#localconstant-table-0x34

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("LocalConstant Row", this, ViewKind.PortablePdb_LocalConstantRow);

            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(Signature), Signature);
        }
    }
}
