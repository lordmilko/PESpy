using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, Imports = {Imports}")]
    public readonly struct ImportScopeRow : IValue, IViewable
    {
        public ImportScopeIndex RowIndex { get; }

        public int Parent => table.GetParent(RowIndex);

        public BlobIndex Imports => table.GetImports(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ImportScopeTable table;

        internal ImportScopeRow(ImportScopeIndex index, ImportScopeTable table)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#importscope-table-0x35

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ImportScope Row", this, ViewKind.PortablePdb_ImportScopeRow);

            s.WriteValue(nameof(Parent), Parent);
            s.WriteBlobHeapIndex(nameof(Imports), Imports);
        }
    }
}
