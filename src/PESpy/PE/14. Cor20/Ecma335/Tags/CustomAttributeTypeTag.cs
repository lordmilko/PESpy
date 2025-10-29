namespace PESpy.Ecma335
{
    internal static class CustomAttributeTypeTag
    {
        //log2(5) = 2.3 = 3. Only 2 of the 5 tags are used
        public const int LogN = 3; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        internal const int MethodDef = 2;
        internal const int MemberRef = 3;

        private const int TagMask = (1 << LogN) - 1;

        internal const ulong TagToTokenTypeByteVector = ((uint) TableKind.MethodDef << 24) >> 8 | ((uint) TableKind.MemberRef << 24);

        public const TableMask CandidateTables =
            TableMask.MethodDef |
            TableMask.MemberRef;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.MethodDef => MethodDef,
                TableKind.MemberRef => MemberRef,
            });

            return new CodedIndex(value, CodedIndexType.CustomAttributeType);
        }

        internal static TableKind GetTableKind(int value) =>
            unchecked((TableKind) (TagToTokenTypeByteVector >> ((value & TagMask) << 3)));
    }
}
