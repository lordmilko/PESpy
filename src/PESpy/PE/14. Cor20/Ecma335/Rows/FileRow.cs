using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Flags = {Flags}, Name = {Name.ToString(),nq}, HashValue = {HashValue}")]
    public readonly struct FileRow : IValue, IViewable
    {
        public FileIndex RowIndex { get; }

        public CorFileFlags Flags => table.GetFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public BlobIndex HashValue => table.GetHashValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly FileTable table;

        internal FileRow(FileIndex index, FileTable table)
        {
            //II.22.19

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FileRow, this, ViewKind.Metadata_FileRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(HashValue), HashValue);

            return s.ToArray();
        }
    }
}
