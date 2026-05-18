using System;

namespace PESpy.Ecma335
{
    public sealed class MethodSpecTable : Table<MethodSpecRow>
    {
        internal readonly int MethodOffset;
        internal readonly int InstantiationOffset;

        private readonly bool isBigMethodDefOrRefIndex;
        private readonly bool isBigBlobIndex;

        internal readonly ModelHeap ModelHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal MethodSpecTable(
            int numRows,
            int methodDefOrRefIndexSize,
            int blobIndexSize,
            ModelHeap modelHeap,
            Func<BlobHeap?> blobHeap, in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.29

            ModelHeap = modelHeap;
            this.blobHeap = blobHeap;

            isBigMethodDefOrRefIndex = methodDefOrRefIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            MethodOffset = 0;
            InstantiationOffset = MethodOffset + methodDefOrRefIndexSize;
            RowSize = InstantiationOffset + blobIndexSize;
        }

        public CodedIndex GetMethod(MethodSpecIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + MethodOffset, isBigMethodDefOrRefIndex, CodedIndexType.MethodDefOrRef);
        }

        public BlobIndex GetInstantiation(MethodSpecIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + InstantiationOffset, isBigBlobIndex), blobHeap);
        }

        public CustomAttributeList GetCustomAttributes(MethodSpecIndex index) =>
            new CustomAttributeList(ModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.MethodSpec));

        public long GetRowOffset(MethodSpecIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MethodSpecRow this[MethodSpecIndex index] => GetRowSafe((int) index);

        protected override MethodSpecRow GetRow(int index) => new MethodSpecRow((MethodSpecIndex) index, this);
    }
}
