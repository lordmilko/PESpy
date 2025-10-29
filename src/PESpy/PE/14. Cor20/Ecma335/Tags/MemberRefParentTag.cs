namespace PESpy.Ecma335
{
    internal static class MemberRefParentTag
    {
        //log2(5) = 2.3 = 3
        public const int LogN = 3; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        private const int TypeDef = 0;
        private const int TypeRef = 1;
        private const int ModuleRef = 2;
        private const int MethodDef = 3;
        private const int TypeSpec = 4;

        private const int TagMask = (1 << LogN) - 1;

        internal const ulong TagToTokenTypeByteVector =
            ((ulong) TableKind.TypeDef << 24) >> 24
            | ((ulong) TableKind.TypeRef << 24) >> 16
            | ((ulong) TableKind.ModuleRef << 24) >> 8
            | ((ulong) TableKind.MethodDef << 24)
            | ((ulong) TableKind.TypeSpec << 24) << 8;

        public const TableMask CandidateTables =
            TableMask.TypeDef |
            TableMask.TypeRef |
            TableMask.ModuleRef |
            TableMask.MethodDef |
            TableMask.TypeSpec;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.TypeDef => TypeDef,
                TableKind.TypeRef => TypeRef,
                TableKind.ModuleRef => ModuleRef,
                TableKind.MethodDef => MethodDef,
                TableKind.TypeSpec => TypeSpec
            });

            return new CodedIndex(value, CodedIndexType.MemberRefParent);
        }

        internal static TableKind GetTableKind(int value) =>
            unchecked((TableKind) (TagToTokenTypeByteVector >> ((value & TagMask) << 3)));
    }
}
