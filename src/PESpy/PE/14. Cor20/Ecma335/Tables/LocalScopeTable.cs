namespace PESpy.Ecma335
{
    public sealed class LocalScopeTable : Table<LocalScopeRow>
    {
        internal readonly int RowSize;

        private readonly int MethodOffset;
        private readonly int ImportScopeOffset;
        private readonly int VariableListOffset;
        private readonly int ConstantListOffset;
        private readonly int StartOffset;
        private readonly int LengthOffset;

        private readonly MemoryChunk tableChunk;

        internal LocalScopeTable(int numRows, in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            MethodOffset = 0;
            ImportScopeOffset = MethodOffset + sizeof(int);
            VariableListOffset = ImportScopeOffset + sizeof(int);
            ConstantListOffset = VariableListOffset + sizeof(int);
            StartOffset = ConstantListOffset + sizeof(int);
            LengthOffset = StartOffset + sizeof(int);
            RowSize = LengthOffset + sizeof(int);
        }

        public int GetMethod(LocalScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + MethodOffset);
        }

        public int GetImportScope(LocalScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + ImportScopeOffset);
        }

        public int GetVariableList(LocalScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + VariableListOffset);
        }

        public int GetConstantList(LocalScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + ConstantListOffset);
        }

        public uint GetStartOffset(LocalScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekUInt32(rowOffset + StartOffset);
        }

        public int GetLength(LocalScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt32(rowOffset + LengthOffset);
        }

        public int GetRowOffset(LocalScopeIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public LocalScopeRow this[LocalScopeIndex index] => this[(int) index];

        protected override LocalScopeRow GetRow(int index) => new LocalScopeRow((LocalScopeIndex) index, this);
    }
}
