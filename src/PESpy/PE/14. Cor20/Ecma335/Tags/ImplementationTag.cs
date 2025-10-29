namespace PESpy.Ecma335
{
    internal static class ImplementationTag
    {
        //log2(3) = 1.58 = 2
        public const int LogN = 2; //Number of bits needed
        public const int LargeRowThreshold = 1 << (16 - LogN);

        private const int File = 0;
        private const int AssemblyRef = 1;
        private const int ExportedType = 2;

        private const int TagMask = (1 << LogN) - 1;

        internal const uint TagToTokenTypeByteVector = ((uint) TableKind.File << 24) >> 24 | ((uint) TableKind.AssemblyRef << 24) >> 16 | ((uint) TableKind.ExportedType << 24) >> 8;

        public const TableMask CandidateTables =
            TableMask.File |
            TableMask.AssemblyRef |
            TableMask.ExportedType;

        internal static CodedIndex CreateIndex(int rowId, TableKind tableKind)
        {
            var value = rowId << LogN | (tableKind switch
            {
                TableKind.File => File,
                TableKind.AssemblyRef => AssemblyRef,
                TableKind.ExportedType => ExportedType
            });

            return new CodedIndex(value, CodedIndexType.Implementation);
        }

        internal static TableKind GetTableKind(int value) =>
            unchecked((TableKind) (TagToTokenTypeByteVector >> ((value & TagMask) << 3)));
    }
}
