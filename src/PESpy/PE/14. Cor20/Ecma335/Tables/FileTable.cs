using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class FileTable : Table<FileRow>
    {
        internal readonly int FlagsOffset;
        internal readonly int NameOffset;
        internal readonly int HashValueOffset;

        private readonly bool isBigStringIndex;
        private readonly bool isBigBlobIndex;

        private readonly CompressedModelHeap compressedModelHeap;
        private readonly Func<StringHeap?> stringHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal FileTable(
            int numRows,
            int stringIndexSize,
            int blobIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<StringHeap?> stringHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            this.compressedModelHeap = compressedModelHeap;
            this.stringHeap = stringHeap;
            this.blobHeap = blobHeap;

            isBigStringIndex = stringIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            FlagsOffset = 0;
            NameOffset = FlagsOffset + sizeof(int);
            HashValueOffset = NameOffset + stringIndexSize;
            RowSize = HashValueOffset + blobIndexSize;
        }

        public CorFileFlags GetFlags(FileIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorFileFlags) tableChunk.PeekUInt32(rowOffset + FlagsOffset);
        }

        public StringIndex GetName(FileIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public BlobIndex GetHashValue(FileIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + HashValueOffset, isBigBlobIndex), blobHeap);
        }

        public CustomAttributeList GetCustomAttributes(FileIndex index) =>
            new CustomAttributeList(compressedModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.File));

        public int GetRowOffset(FileIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public FileRow this[FileIndex index] => GetRowSafe((int) index);

        protected override FileRow GetRow(int index) => new FileRow((FileIndex) index, this);
    }
}
