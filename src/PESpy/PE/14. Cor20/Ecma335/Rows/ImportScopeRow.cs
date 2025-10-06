using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, Imports = {Imports}")]
    public readonly struct ImportScopeRow : IValue, IViewable
    {
        public ImportScopeIndex RowIndex { get; }

        public ImportScopeIndex Parent => table.GetParent(RowIndex);

        public BlobIndex Imports => table.GetImports(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ImportScopeTable table;

        internal ImportScopeRow(ImportScopeIndex index, ImportScopeTable table)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#importscope-table-0x35

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ImportScopeRow, this, ViewKind.PortablePdb_ImportScopeRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Parent), (int) Parent);
            s.WriteBlobHeapIndex(nameof(Imports), Imports);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
