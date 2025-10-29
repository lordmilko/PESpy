using ClrDebug;

namespace PESpy.Ecma335
{
    internal static class HasFieldMarshalTag
    {
        //log2(1) = 1
        public const int LogN = 1; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        private const int Field = 0;
        private const int Param = 1;

        private const int TagMask = (1 << LogN) - 1;

        internal const ulong TagToTokenTypeByteVector =
           ((ulong) TableKind.TypeDef << 24) >> 24
           | ((ulong) TableKind.TypeRef << 24) >> 16
           | ((ulong) TableKind.ModuleRef << 24) >> 8
           | ((ulong) TableKind.MethodDef << 24)
           | ((ulong) TableKind.TypeSpec << 24) << 8;

        public const TableMask CandidateTables =
            TableMask.Field |
            TableMask.Param;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.Field => Field,
                TableKind.Param => Param
            });

            return new CodedIndex(value, CodedIndexType.HasFieldMarshal);
        }

        internal static TableKind GetTableKind(int value) =>
            unchecked((TableKind) (TagToTokenTypeByteVector >> ((value & TagMask) << 3)));
    }
}
