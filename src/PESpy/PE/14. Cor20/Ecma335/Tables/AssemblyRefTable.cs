using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class AssemblyRefTable : Table<AssemblyRefRow>
    {
        internal readonly int RowSize;

        private readonly int MajorVersionOffset;
        private readonly int MinorVersionOffset;
        private readonly int BuildNumberOffset;
        private readonly int RevisionNumberOffset;
        private readonly int FlagsOffset;
        private readonly int PublicKeyOrTokenOffset;
        private readonly int NameOffset;
        private readonly int CultureOffset;
        private readonly int HashValueOffset;

        private readonly bool isBigBlobIndex;
        private readonly bool isBigStringIndex;

        private readonly Lazy<StringHeap?> stringHeap;
        private readonly Lazy<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal AssemblyRefTable(int numRows, int blobIndexSize, int stringIndexSize, Lazy<StringHeap?> stringHeap, Lazy<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.stringHeap = stringHeap;
            this.blobHeap = blobHeap;

            isBigBlobIndex = blobIndexSize == 4;
            isBigStringIndex = stringIndexSize == 4;

            MajorVersionOffset = 0;
            MinorVersionOffset = MajorVersionOffset + sizeof(ushort);
            BuildNumberOffset = MinorVersionOffset + sizeof(ushort);
            RevisionNumberOffset = BuildNumberOffset + sizeof(ushort);
            FlagsOffset = RevisionNumberOffset + sizeof(ushort);
            PublicKeyOrTokenOffset = FlagsOffset + sizeof(uint);
            NameOffset = PublicKeyOrTokenOffset + blobIndexSize;
            CultureOffset = NameOffset + stringIndexSize;
            HashValueOffset = CultureOffset + stringIndexSize;
            RowSize = HashValueOffset + blobIndexSize;
        }

        public short GetMajorVersion(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + MajorVersionOffset);
        }

        public short GetMinorVersion(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + MinorVersionOffset);
        }

        public short GetBuildNumber(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + BuildNumberOffset);
        }

        public short GetRevisionNumber(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + RevisionNumberOffset);
        }

        public AssemblyFlags GetFlags(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (AssemblyFlags) tableChunk.PeekUInt32(rowOffset + FlagsOffset);
        }

        public BlobIndex GetPublicKeyOrToken(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + PublicKeyOrTokenOffset, isBigBlobIndex), blobHeap.Value);
        }

        public StringIndex GetName(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap.Value);
        }

        public StringIndex GetCulture(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + CultureOffset, isBigStringIndex), stringHeap.Value);
        }

        public BlobIndex GetHashValue(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + HashValueOffset, isBigBlobIndex), blobHeap.Value);
        }

        public int GetRowOffset(AssemblyRefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public AssemblyRefRow this[AssemblyRefIndex index] => this[(int) index];

        protected override AssemblyRefRow GetRow(int index) => new AssemblyRefRow((AssemblyRefIndex) index, this);
    }
}
