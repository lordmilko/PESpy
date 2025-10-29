namespace PESpy.Ecma335
{
    internal static class HasDeclSecurityTag
    {
        //log2(3) = 1.58 = 2
        public const int LogN = 2; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        private const int TypeDef = 0;
        private const int MethodDef = 1;
        private const int Assembly = 2;

        private const int TagMask = (1 << LogN) - 1;

        internal const uint TagToTokenTypeByteVector = (((uint) TableKind.TypeDef << 24) >> 24) | (((uint) TableKind.MethodDef << 24) >> 16) | (((uint) TableKind.Assembly << 24) >> 8);

        public const TableMask CandidateTables =
            TableMask.TypeDef |
            TableMask.MethodDef |
            TableMask.Assembly;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.TypeDef => TypeDef,
                TableKind.MethodDef => MethodDef,
                TableKind.Assembly => Assembly
            });

            return new CodedIndex(value, CodedIndexType.HasDeclSecurity);
        }

        internal static TableKind GetTableKind(int value) =>
            unchecked((TableKind) (TagToTokenTypeByteVector >> ((value & TagMask) << 3)));
    }
}
