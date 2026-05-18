using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class AssemblyRefTable : Table<AssemblyRefRow>
    {
        internal readonly int MajorVersionOffset;
        internal readonly int MinorVersionOffset;
        internal readonly int BuildNumberOffset;
        internal readonly int RevisionNumberOffset;
        internal readonly int FlagsOffset;
        internal readonly int PublicKeyOrTokenOffset;
        internal readonly int NameOffset;
        internal readonly int CultureOffset;
        internal readonly int HashValueOffset;

        private readonly bool isBigBlobIndex;
        private readonly bool isBigStringIndex;

        private readonly ModelHeap modelHeap;
        private readonly Func<StringHeap?> stringHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal AssemblyRefTable(
            int numRows,
            int blobIndexSize,
            int stringIndexSize,
            ModelHeap modelHeap,
            Func<StringHeap?> stringHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            this.modelHeap = modelHeap;
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

        public CorAssemblyFlags GetFlags(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorAssemblyFlags) tableChunk.PeekUInt32(rowOffset + FlagsOffset);
        }

        public BlobIndex GetPublicKeyOrToken(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + PublicKeyOrTokenOffset, isBigBlobIndex), blobHeap);
        }

        public StringIndex GetName(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public StringIndex GetCulture(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + CultureOffset, isBigStringIndex), stringHeap);
        }

        public BlobIndex GetHashValue(AssemblyRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + HashValueOffset, isBigBlobIndex), blobHeap);
        }

        public CustomAttributeList GetCustomAttributes(AssemblyRefIndex index) =>
            new CustomAttributeList(modelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.AssemblyRef));

        public long GetRowOffset(AssemblyRefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public AssemblyRefRow this[AssemblyRefIndex index] => GetRowSafe((int) index);

        protected override AssemblyRefRow GetRow(int index) => new AssemblyRefRow((AssemblyRefIndex) index, this);
    }
}
