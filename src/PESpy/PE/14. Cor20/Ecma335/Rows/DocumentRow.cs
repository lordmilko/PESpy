using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("Document Row", this, ViewKind.PortablePdb_DocumentRow);

            s.WriteBlobHeapIndex(nameof(Name), Name);
            s.WriteGuidHeapIndex(nameof(HashAlgorithm), HashAlgorithm);
            s.WriteBlobHeapIndex(nameof(Hash), Hash);
            s.WriteGuidHeapIndex(nameof(Language), Language);
        }
    }
}
