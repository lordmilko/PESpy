namespace PESpy.Ecma335
{
    internal static class MethodDefOrRefTag
    {
        //log2(2) = 1
        public const int LogN = 1; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        private const int MethodDef = 0;
        private const int MemberRef = 1;

        private const int TagMask = (1 << LogN) - 1;

        internal const uint TagToTokenTypeByteVector = ((uint) TableKind.MethodDef << 24) >> 24 | ((uint) TableKind.MemberRef << 24) >> 16;

        public const TableMask CandidateTables =
            TableMask.MethodDef |
            TableMask.MemberRef;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.MethodDef => MethodDef,
                TableKind.MemberRef => MemberRef
            });

            return new CodedIndex(value, CodedIndexType.MethodDefOrRef);
        }

        internal static TableKind GetTableKind(int value) =>
            unchecked((TableKind) (TagToTokenTypeByteVector >> ((value & TagMask) << 3)));
    }
}
