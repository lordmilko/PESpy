using System;
using System.Configuration.Assemblies;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class AssemblyTable : Table<AssemblyRow>
    {
        internal readonly int RowSize;

        internal readonly int HashAlgIdOffset;
        internal readonly int MajorVersionOffset;
        internal readonly int MinorVersionOffset;
        internal readonly int BuildNumberOffset;
        internal readonly int RevisionNumberOffset;
        internal readonly int FlagsOffset;
        internal readonly int PublicKeyOffset;
        internal readonly int NameOffset;
        internal readonly int CultureOffset;

        private readonly bool isBigBlobIndex;
        private readonly bool isBigStringIndex;

        private readonly CompressedModelHeap compressedModelHeap;
        private readonly Func<StringHeap?> stringHeap;
        private readonly Func<BlobHeap?> blobHeap;
        private readonly MemoryChunk tableChunk;

        internal AssemblyTable(
            int numRows,
            int blobIndexSize,
            int stringIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<StringHeap?> stringHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;
            this.compressedModelHeap = compressedModelHeap;
            this.stringHeap = stringHeap;
            this.blobHeap = blobHeap;

            isBigBlobIndex = blobIndexSize == 4;
            isBigStringIndex = stringIndexSize == 4;

            HashAlgIdOffset = 0;
            MajorVersionOffset = HashAlgIdOffset + sizeof(int);
            MinorVersionOffset = MajorVersionOffset + sizeof(ushort);
            BuildNumberOffset = MinorVersionOffset + sizeof(ushort);
            RevisionNumberOffset = BuildNumberOffset + sizeof(ushort);
            FlagsOffset = RevisionNumberOffset + sizeof(ushort);
            PublicKeyOffset = FlagsOffset + sizeof(int);
            NameOffset = PublicKeyOffset + blobIndexSize;
            CultureOffset = NameOffset + stringIndexSize;
            RowSize = CultureOffset + stringIndexSize;
        }

        public AssemblyHashAlgorithm GetHashAlgId(AssemblyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (AssemblyHashAlgorithm) tableChunk.PeekUInt32(rowOffset + HashAlgIdOffset);
        }

        public short GetMajorVersion(AssemblyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + MajorVersionOffset);
        }

        public short GetMinorVersion(AssemblyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + MinorVersionOffset);
        }

        public short GetBuildNumber(AssemblyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + BuildNumberOffset);
        }

        public short GetRevisionNumber(AssemblyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + RevisionNumberOffset);
        }

        public AssemblyFlags GetFlags(AssemblyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (AssemblyFlags) tableChunk.PeekUInt32(rowOffset + FlagsOffset);
        }

        public BlobIndex GetPublicKey(AssemblyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + PublicKeyOffset, isBigBlobIndex), blobHeap);
        }

        public StringIndex GetName(AssemblyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public StringIndex GetCulture(AssemblyIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + CultureOffset, isBigStringIndex), stringHeap);
        }

        public CustomAttributeList GetCustomAttributes(AssemblyIndex index) =>
            new CustomAttributeList(compressedModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.Assembly));

        public int GetRowOffset(AssemblyIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public AssemblyRow this[AssemblyIndex index] => this[(int) index];

        protected override AssemblyRow GetRow(int index) => new AssemblyRow((AssemblyIndex) index, this);
    }
}
