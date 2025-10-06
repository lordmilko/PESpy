using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Name = {Name.ToString(),nq}, HashAlgorithm = {HashAlgorithm}, Hash = {Hash}, Language = {Language}")]
    public readonly struct DocumentRow : IValue, IViewable
    {
        public DocumentIndex RowIndex { get; }

        public DocumentNameBlobIndex Name => table.GetName(RowIndex);

        public GuidIndex HashAlgorithm => table.GetHashAlgorithm(RowIndex);

        public BlobIndex Hash => table.GetHash(RowIndex);

        public GuidIndex Language => table.GetLanguage(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly DocumentTable table;

        internal DocumentRow(DocumentIndex index, DocumentTable table)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#document-table-0x30

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.DocumentRow, this, ViewKind.PortablePdb_DocumentRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteBlobHeapIndex(nameof(Name), Name);
            s.WriteGuidHeapIndex(nameof(HashAlgorithm), HashAlgorithm);
            s.WriteBlobHeapIndex(nameof(Hash), Hash);
            s.WriteGuidHeapIndex(nameof(Language), Language);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
