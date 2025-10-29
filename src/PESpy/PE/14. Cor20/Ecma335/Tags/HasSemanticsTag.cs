namespace PESpy.Ecma335
{
    internal static class HasSemanticsTag
    {
        //log2(2) = 1
        public const int LogN = 1; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        private const int Event = 0;
        private const int Property = 1;

        private const int TagMask = (1 << LogN) - 1;

        internal const uint TagToTokenTypeByteVector = (((uint) TableKind.Event << 24) >> 24) | (((uint) TableKind.Property << 24) >> 16);

        public const TableMask CandidateTables =
            TableMask.Event |
            TableMask.Property;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.Event => Event,
                TableKind.Property => Property
            });

            return new CodedIndex(value, CodedIndexType.HasSemantics);
        }

        internal static TableKind GetTableKind(int value) =>
            unchecked((TableKind) (TagToTokenTypeByteVector >> ((value & TagMask) << 3)));
    }
}
