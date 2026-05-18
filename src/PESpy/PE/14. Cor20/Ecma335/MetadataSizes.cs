using System;
using System.Diagnostics;
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

        public readonly int ExternalMethodDefSize;

        //Indicates we have a #JTD stream which means that all metadata references are always 4 bytes and not potentially 2
        public readonly bool IsMinimalDelta;

        private readonly int[] rowCounts;

        public unsafe MetadataSizes(HeapSizes heapSizes, bool isMinimalDelta, int[] rowCounts, PdbHeap? pdbHeap)
        {
            IsMinimalDelta = isMinimalDelta;
            this.rowCounts = rowCounts;

            StringIndexSize = ((heapSizes & HeapSizes.HEAP_STRING_4) != 0) ? 4 : 2;
            GuidIndexSize = ((heapSizes & HeapSizes.HEAP_GUID_4) != 0) ? 4 : 2;
            BlobIndexSize = ((heapSizes & HeapSizes.HEAP_BLOB_4) != 0) ? 4 : 2;

            TypeDefOrRefSize        = GetCodedIndexSize(rowCounts, isMinimalDelta, TypeDefOrRefTag.CandidateTables,        TypeDefOrRefTag.LargeRowThreshold);
            HasConstantSize         = GetCodedIndexSize(rowCounts, isMinimalDelta, HasConstantTag.CandidateTables,         HasConstantTag.LargeRowThreshold);
            HasCustomAttributeSize  = GetCodedIndexSize(rowCounts, isMinimalDelta, HasCustomAttributeTag.CandidateTables,  HasCustomAttributeTag.LargeRowThreshold);
            HasFieldMarshalSize     = GetCodedIndexSize(rowCounts, isMinimalDelta, HasFieldMarshalTag.CandidateTables,     HasFieldMarshalTag.LargeRowThreshold);
            HasDeclSecuritySize     = GetCodedIndexSize(rowCounts, isMinimalDelta, HasDeclSecurityTag.CandidateTables,     HasDeclSecurityTag.LargeRowThreshold);
            MemberRefParentSize     = GetCodedIndexSize(rowCounts, isMinimalDelta, MemberRefParentTag.CandidateTables,     MemberRefParentTag.LargeRowThreshold);
            HasSemanticsSize        = GetCodedIndexSize(rowCounts, isMinimalDelta, HasSemanticsTag.CandidateTables,        HasSemanticsTag.LargeRowThreshold);
            MethodDefOrRefSize      = GetCodedIndexSize(rowCounts, isMinimalDelta, MethodDefOrRefTag.CandidateTables,      MethodDefOrRefTag.LargeRowThreshold);
            MemberForwardedSize     = GetCodedIndexSize(rowCounts, isMinimalDelta, MemberForwardedTag.CandidateTables,     MemberForwardedTag.LargeRowThreshold);
            ImplementationSize      = GetCodedIndexSize(rowCounts, isMinimalDelta, ImplementationTag.CandidateTables,      ImplementationTag.LargeRowThreshold);
            CustomAttributeTypeSize = GetCodedIndexSize(rowCounts, isMinimalDelta, CustomAttributeTypeTag.CandidateTables, CustomAttributeTypeTag.LargeRowThreshold);
            ResolutionScopeSize     = GetCodedIndexSize(rowCounts, isMinimalDelta, ResolutionScopeTag.CandidateTables,     ResolutionScopeTag.LargeRowThreshold);
            TypeOrMethodDefSize     = GetCodedIndexSize(rowCounts, isMinimalDelta, TypeOrMethodDefTag.CandidateTables,     TypeOrMethodDefTag.LargeRowThreshold);

            //Portable PDB
            if (pdbHeap != null)
            {
                /* If we're a Portable PDB, the row counts we've been reading are the counts of our debug entities,
                 * (which you would expect have been 0 so far). However, some debug entities refer to indices in
                 * metadata tables; e.g. LocalScope lists a MethodDef index. We need to know how many methods were
                 * in the original .NET assembly to know how big these indices should be! Now you might be asking:
                 * can't we just use the MethodDebugInformation table for that? Arguably, yes, however there's a bit
                 * of a catch in that the specification says
                 * 
                 *     MethodDebugInformation table is either empty (missing) or has exactly as many rows as MethodDef
                 *     table
                 * 
                 * I'm not exactly sure under which circumstance it may be missing, but that alone seems to be enough to spook us
                 * into retrieving the row count from the external MethodDef info instead.
                 * 
                 * The real reason we need to leverage these external counts is for processing custom debug information.
                 * Almost every type of entity can have custom debug information associated with it, so we need to look
                 * at the row counts of each of these to see if any of them warrant us using 4 byte coded indices instead
                 * of 2
                 * 
                 * Note that we don't merge above, because all of these properties are used for computing the number of rows
                 * in _this current image_. The merged count gives us the total number of rows across _either_ the .NET
                 * assembly file or the current Portable PDB file.
                 */

                //At first, this will just be the external row counts, but then we'll copy the Portable PDB specific row counts on top
                Span<int> allRowCounts = stackalloc int[64];
                pdbHeap.GetRowCounts(allRowCounts);

                //Copy the counts from the Portable PDB on top
                rowCounts.AsSpan((int) TableKind.Document).CopyTo(allRowCounts.Slice((int) TableKind.Document));

                //Query the size of each method index based on the number of rows in the external assembly.
                //We also could have queried this from allRowCounts directly
                ExternalMethodDefSize = GetSimpleIndexSize(allRowCounts, TableKind.MethodDef);

                //Analyze all row counts (either within this Portable PDB or within the external .NET assembly) to determine
                //whether any given record may have so many rows that it warrants using 4 byte coded indices for custom debug inforamtion
                //rather than 2
                HasCustomAttributeSize = GetCodedIndexSize(allRowCounts, isMinimalDelta, HasCustomDebugInformationTag.CandidateTables, HasCustomDebugInformationTag.LargeRowThreshold);
            }
        }

        internal static int GetCodedIndexSize(Span<int> rowCounts, bool isMinimalDelta, TableMask candidateTables, int largeRowThreshold)
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
                if (((ulong) candidateTables & bit) != 0)
                {
                    //LargeRowThreshold is 2^(16 – (log n))
                    var isBigTable = rowCount > largeRowThreshold;

                    if (isBigTable)
                        return 4;
                }

                bit <<= 1;
            }

            return 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetSimpleIndexSize(TableKind tableKind) => GetSimpleIndexSize(rowCounts, tableKind);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetSimpleIndexSize(Span<int> rowCounts, TableKind tableKind)
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
