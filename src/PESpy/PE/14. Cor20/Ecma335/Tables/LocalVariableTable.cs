using System;

namespace PESpy.Ecma335
{
    public sealed class LocalVariableTable : Table<LocalVariableRow>
    {
        internal readonly int AttributesOffset;
        internal readonly int IndexOffset;
        internal readonly int NameOffset;

        private readonly bool isBigStringIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<StringHeap?> stringHeap;

        internal LocalVariableTable(
            int numRows,
            int stringIndexSize,
            Func<StringHeap?> stringHeap,
            CompressedModelHeap compressedModelHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            CompressedModelHeap = compressedModelHeap;
            this.stringHeap = stringHeap;

            isBigStringIndex = stringIndexSize == 4;

            AttributesOffset = 0;
            IndexOffset = AttributesOffset + sizeof(short);
            NameOffset = IndexOffset + sizeof(short);
            RowSize = NameOffset + stringIndexSize;
        }

        public LocalVariableAttributes GetAttributes(LocalVariableIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (LocalVariableAttributes) tableChunk.PeekUInt16(rowOffset + AttributesOffset);
        }

        public uint GetIndex(LocalVariableIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekUInt16(rowOffset + IndexOffset);
        }

        public StringIndex GetName(LocalVariableIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        internal void GetRange(LocalScopeIndex localScope, out int firstVariableRowId, out int lastVariableRowId)
        {
            firstVariableRowId = (int) CompressedModelHeap.LocalScopeTable.GetVariableList(localScope);

            if (firstVariableRowId == 0)
            {
                firstVariableRowId = 0;
                lastVariableRowId = 0;
            }
            else if (localScope.RowId == CompressedModelHeap.LocalScopeTable.Count)
            {
                lastVariableRowId = Count + 1;
            }
            else
            {
                lastVariableRowId = (int) CompressedModelHeap.LocalScopeTable.GetVariableList((LocalScopeIndex) (localScope.RowId + 1));
            }
        }

        public int GetRowOffset(LocalVariableIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public LocalVariableRow this[LocalVariableIndex index] => GetRowSafe((int) index);

        protected override LocalVariableRow GetRow(int index) => new LocalVariableRow((LocalVariableIndex) index, this);
    }
}
