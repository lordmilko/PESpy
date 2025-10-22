using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

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

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 2:
                    structWriter.WriteBlobHeapIndex(nameof(HashValue), table.HashValueOffset, HashValue);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
