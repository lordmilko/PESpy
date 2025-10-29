namespace PESpy.Ecma335
{
    internal static class TypeOrMethodDefTag
    {
        //log2(2) = 1
        public const int LogN = 1; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        private const int TypeDef = 0;
        private const int MethodDef = 1;

        private const int TagMask = (1 << LogN) - 1;

        internal const uint TagToTokenTypeByteVector = ((uint) TableKind.TypeDef << 24) >> 24 | ((uint) TableKind.MethodDef << 24) >> 16;

        public const TableMask CandidateTables =
            TableMask.TypeDef |
            TableMask.MethodDef;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.TypeDef => TypeDef,
            });

            return new CodedIndex(value, CodedIndexType.TypeOrMethodDef);
        }

        internal static TableKind GetTableKind(int value) =>
            unchecked((TableKind) (TagToTokenTypeByteVector >> ((value & TagMask) << 3)));
    }
}
