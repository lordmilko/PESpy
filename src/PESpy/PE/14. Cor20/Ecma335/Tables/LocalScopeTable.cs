namespace PESpy.Ecma335
{
    public sealed class LocalScopeTable : Table<LocalScopeRow>
    {
        internal readonly int RowSize;

        internal readonly int MethodOffset;
        internal readonly int ImportScopeOffset;
        internal readonly int VariableListOffset;
        internal readonly int ConstantListOffset;
        internal readonly int StartOffsetOffset;
        internal readonly int LengthOffset;

        private readonly bool isBigMethodIndex;
        private readonly bool isBigImportScopeIndex;
        private readonly bool isBigLocalVariableIndex;
        private readonly bool isBigLocalConstantIndex;

        private readonly MemoryChunk tableChunk;

        internal LocalScopeTable(
            int numRows,
            int methodIndexSize,
            int importScopeIndexSize,
            int localVariableIndexSize,
            int localConstantIndexSize,
            in MemoryChunk tableChunk) : base(numRows)
        {
            this.tableChunk = tableChunk;

            isBigMethodIndex = methodIndexSize == 4;
            isBigImportScopeIndex = importScopeIndexSize == 4;
            isBigLocalVariableIndex = localVariableIndexSize == 4;
            isBigLocalConstantIndex = localConstantIndexSize == 4;

            MethodOffset = 0;
            ImportScopeOffset = MethodOffset + methodIndexSize;
            VariableListOffset = ImportScopeOffset + importScopeIndexSize;
            ConstantListOffset = VariableListOffset + localVariableIndexSize;
            StartOffsetOffset = ConstantListOffset + localConstantIndexSize;
            LengthOffset = StartOffsetOffset + sizeof(int);
            RowSize = LengthOffset + sizeof(int);
        }

        public MethodDefIndex GetMethod(LocalScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (MethodDefIndex) tableChunk.PeekEcmaIndex(rowOffset + MethodOffset, isBigMethodIndex);
        }

        public ImportScopeIndex GetImportScope(LocalScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (ImportScopeIndex) tableChunk.PeekEcmaIndex(rowOffset + ImportScopeOffset, isBigImportScopeIndex);
        }

        public LocalVariableIndex GetVariableList(LocalScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (LocalVariableIndex) tableChunk.PeekEcmaIndex(rowOffset + VariableListOffset, isBigLocalVariableIndex);
        }

        public LocalConstantIndex GetConstantList(LocalScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (LocalConstantIndex) tableChunk.PeekEcmaIndex(rowOffset + ConstantListOffset, isBigLocalConstantIndex);
        }

        public uint GetStartOffset(LocalScopeIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekUInt32(rowOffset + StartOffsetOffset);
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
