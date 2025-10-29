namespace PESpy.Ecma335
{
    internal static class HasConstantTag
    {
        //log2(3) = 1.58 = 2
        public const int LogN = 2; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        private const int Field = 0;
        private const int Param = 1;
        private const int Property = 2;

        private const int TagMask = (1 << LogN) - 1;

        internal const uint TagToTokenTypeByteVector = ((uint) TableKind.Field << 24) >> 24 | ((uint) TableKind.Param << 24) >> 16 | ((uint) TableKind.Property << 24) >> 8;

        public const TableMask CandidateTables =
            TableMask.Field |
            TableMask.Param |
            TableMask.Property;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.Field => Field,
                TableKind.Param => Param,
                TableKind.Property => Property
            });

            return new CodedIndex(value, CodedIndexType.HasConstant);
        }

        internal static TableKind GetTableKind(int value) =>
            unchecked((TableKind) (TagToTokenTypeByteVector >> ((value & TagMask) << 3)));
    }
}
