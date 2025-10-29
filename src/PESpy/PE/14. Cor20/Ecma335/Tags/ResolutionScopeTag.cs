namespace PESpy.Ecma335
{
    internal static class ResolutionScopeTag
    {
        //log2(4) = 2
        public const int LogN = 2; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        private const int Module = 0;
        private const int ModuleRef = 1;
        private const int AssemblyRef = 2;
        private const int TypeRef = 3;

        private const int TagMask = (1 << LogN) - 1;

        internal const uint TagToTokenTypeByteVector = ((uint) TableKind.Module << 24) >> 24 | ((uint) TableKind.ModuleRef << 24) >> 16 | ((uint) TableKind.AssemblyRef << 24) >> 8 | ((uint) TableKind.TypeRef << 24);

        public const TableMask CandidateTables =
            TableMask.Module |
            TableMask.ModuleRef |
            TableMask.AssemblyRef |
            TableMask.TypeRef;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.Module => Module,
                TableKind.ModuleRef => ModuleRef,
                TableKind.AssemblyRef => AssemblyRef,
                TableKind.TypeRef => TypeRef
            });

            return new CodedIndex(value, CodedIndexType.ResolutionScope);
        }

        internal static TableKind GetTableKind(int value) =>
            unchecked((TableKind) (TagToTokenTypeByteVector >> ((value & TagMask) << 3)));
    }
}
