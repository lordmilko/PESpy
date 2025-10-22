using System;
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

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteBlobHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 1:
                    structWriter.WriteGuidHeapIndex(nameof(HashAlgorithm), table.HashAlgorithmOffset, HashAlgorithm);
                    break;

                case 2:
                    structWriter.WriteBlobHeapIndex(nameof(Hash), table.HashOffset, Hash);
                    break;

                case 3:
                    structWriter.WriteGuidHeapIndex(nameof(Language), table.LanguageOffset, Language);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
