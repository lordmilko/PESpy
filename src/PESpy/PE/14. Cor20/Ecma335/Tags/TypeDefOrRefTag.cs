namespace PESpy.Ecma335
{
    internal static class TypeDefOrRefTag
    {
        //log2(3) = 1.58 = 2
        public const int LogN = 2; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        private const int TypeDef = 0;
        private const int TypeRef = 1;
        private const int TypeSpec = 2;

        private const int TagMask = (1 << LogN) - 1;

        internal const uint TagToTokenTypeByteVector = ((uint) TableKind.TypeDef << 24) >> 24 | ((uint) TableKind.TypeRef << 24) >> 16 | ((uint) TableKind.TypeSpec << 24) >> 8;

        public const TableMask CandidateTables =
            TableMask.TypeDef |
            TableMask.TypeRef |
            TableMask.TypeSpec;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.TypeDef => TypeDef,
                TableKind.TypeRef => TypeRef,
                TableKind.TypeSpec => TypeSpec
            });

            return new CodedIndex(value, CodedIndexType.TypeDefOrRef);
        }

        internal static TableKind GetTableKind(int value) =>
            unchecked((TableKind) (TagToTokenTypeByteVector >> ((value & TagMask) << 3)));
    }
}
