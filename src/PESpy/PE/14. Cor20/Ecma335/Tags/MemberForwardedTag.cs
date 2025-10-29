namespace PESpy.Ecma335
{
    internal static class MemberForwardedTag
    {
        //log2(2) = 1
        public const int LogN = 1; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        private const int Field = 0;
        private const int MethodDef = 1;

        private const int TagMask = (1 << LogN) - 1;

        internal const uint TagToTokenTypeByteVector = ((uint) TableKind.Field << 24) >> 24 | ((uint) TableKind.MethodDef << 24) >> 16;

        public const TableMask CandidateTables =
            TableMask.Field |
            TableMask.MethodDef;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.Field => Field,
                TableKind.MethodDef => MethodDef
            });

            return new CodedIndex(value, CodedIndexType.MemberForwarded);
        }

        internal static TableKind GetTableKind(int value) =>
            unchecked((TableKind) (TagToTokenTypeByteVector >> ((value & TagMask) << 3)));
    }
}
