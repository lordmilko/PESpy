using System.Runtime.CompilerServices;

namespace PESpy.Ecma335
{
    //Encapsulates size information. We need this not only when preparing our table readers, but also in MetadataRowWriter
    public readonly struct MetadataSizes
    {
        public readonly int StringIndexSize;
        public readonly int GuidIndexSize;
        public readonly int BlobIndexSize;

        public readonly int TypeDefOrRefSize;
        public readonly int HasConstantSize;
        public readonly int HasCustomAttributeSize;
        public readonly int HasFieldMarshalSize;
        public readonly int HasDeclSecuritySize;
        public readonly int MemberRefParentSize;
        public readonly int HasSemanticsSize;
        public readonly int MethodDefOrRefSize;
        public readonly int MemberForwardedSize;
        public readonly int ImplementationSize;
        public readonly int CustomAttributeTypeSize;
        public readonly int ResolutionScopeSize;
        public readonly int TypeOrMethodDefSize;

        public readonly int HasCustomDebugInformationSize;

        //Indicates we have a #JTD stream which means that all metadata references are always 4 bytes and not potentially 2
        public readonly bool IsMinimalDelta;

        private readonly int[] rowCounts;

        public MetadataSizes(HeapSizes heapSizes, bool isMinimalDelta, int[] rowCounts)
        {
            IsMinimalDelta = isMinimalDelta;
            this.rowCounts = rowCounts;

            StringIndexSize = ((heapSizes & HeapSizes.HEAP_STRING_4) != 0) ? 4 : 2;
            GuidIndexSize = ((heapSizes & HeapSizes.HEAP_GUID_4) != 0) ? 4 : 2;
            BlobIndexSize = ((heapSizes & HeapSizes.HEAP_BLOB_4) != 0) ? 4 : 2;

            TypeDefOrRefSize        = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.TypeDefOrRef);
            HasConstantSize         = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.HasConstant);
            HasCustomAttributeSize  = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.HasCustomAttribute);
            HasFieldMarshalSize     = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.HasFieldMarshal);
            HasDeclSecuritySize     = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.HasDeclSecurity);
            MemberRefParentSize     = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.MemberRefParent);
            HasSemanticsSize        = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.HasSemantics);
            MethodDefOrRefSize      = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.MethodDefOrRef);
            MemberForwardedSize     = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.MemberForwarded);
            ImplementationSize      = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.Implementation);
            CustomAttributeTypeSize = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.CustomAttributeType);
            ResolutionScopeSize     = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.ResolutionScope);
            TypeOrMethodDefSize     = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.TypeOrMethodDef);

            //Portable PDB
            HasCustomDebugInformationSize = GetCodedIndexSize(rowCounts, isMinimalDelta, CodedIndexTag.HasCustomDebugInformation);
        }

        internal static int GetCodedIndexSize(int[] rowCounts, bool isMinimalDelta, CodedIndexTag tag)
        {
            /* A coded index is an index that can reference one of several potential tables. Which table, and the index to then use in that table,
             * are stored in a compact format. e.g. a TypeDefOrReg coded index is an index that targets either the TypeDef, TypeRef or TypeSpec table.
             *
             * The following breaks down the description of coded indices from II.24.2.6 and translates it into english
             *
             * | Spec                                                                 | English |
             * |----------------------------------------------------------------------|------------------------------------------------------------------------------------------------|
             * | If e is a coded index                                                | if the value you're trying to parse is a coded index (say, TypeDefOrRef),
             * | that points into table t[i] out of n possible tables t[0], …t[n-1]   | that points to the TypeRef table (out of the possibilities of TypeDef, TypeRef or TypeSpec)
             * | then it is stored as e << (log n) | tag{ t[0], …t[n-1]}[ t[i] ]      | then the raw index is stored by left shifting the value by log n bits,
             * |                                                                      | and then or'ing it with the tag of the table that it resides in. e.g. in a TypeDefOrRef coded index, TypeRef has tag "1"
             * |                                                                      |
             * | using 2 bytes if the maximum number of rows of tables t[0], …t[n-1], | iterate over each of the candidate tables (i.e. TypeDef, TypeRef and TypeSpec in the case of a TypeDefOrRef column).
             * | is less than 2^(16 – (log n)), and using 4 bytes otherwise           | if all of them are lower than "the threshold" (to be explained shortly), use 2 bytes. Otherwise, use 4 bytes.
             * |                                                                      | As there are 3 potential candidates, you would do log 3 (with base 2) which gives 1.5, rounded up to 2. 16-2 = 14,
             *                                                                        | and then do 2^14 = 16,384. Thus, if either of the TypeDef, TypeRef or TypeSpec tables has more than 16,384 rows,
             *                                                                        | 2 bytes are used. Otherwise, 4 bytes are used
             */

            if (isMinimalDelta)
                return 4;

            ulong bit = 1;

            foreach (var rowCount in rowCounts)
            {
                if (((ulong) tag.CandidateTables & bit) != 0)
                {
                    //LargeRowThreshold is 2^(16 – (log n))
                    var isBigTable = rowCount > tag.LargeRowThreshold;

                    if (isBigTable)
                        return 4;
                }

                bit <<= 1;
            }

            return 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetSimpleIndexSize(TableKind tableKind)
        {
            /* II.22
             *
             * Each index is either 2 or 4 bytes wide. The index points into the same or another table, or into one of
             * the four heaps. The size of each index column in a table is only made 4 bytes if it needs to be for that
             * particular module. So, if a particular column indexes a table, or tables, whose highest row number fits
             * in a 2-byte value, the indexer column need only be 2 bytes wide. Conversely, for tables containing
             * 64K or more rows, an indexer of that table will be 4 bytes wide. */

            var numRows = rowCounts[(int) tableKind];

            if (numRows > ushort.MaxValue || IsMinimalDelta)
                return 4;

            return 2;
        }
    }
}
