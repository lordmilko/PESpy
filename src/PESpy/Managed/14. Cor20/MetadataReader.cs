using System.Runtime.CompilerServices;

namespace PESpy.Ecma335
{
    internal class MetadataReader
    {
        #region Heap Index Sizes

        internal int StringIndexSize { get; }
        internal int GuidIndexSize { get; }
        internal int BlobIndexSize { get; }

        #endregion
        #region Coded Index Sizes
        public int TypeDefOrRefSize { get; }
        public int HasConstantSize { get; }
        public int HasCustomAttributeSize { get; }
        public int HasFieldMarshalSize { get; }
        public int HasDeclSecuritySize { get; }
        public int MemberRefParentSize { get; }
        public int HasSemanticsSize { get; }
        public int MethodDefOrRefSize { get; }
        public int MemberForwardedSize { get; }
        public int ImplementationSize { get; }
        public int CustomAttributeTypeSize { get; }
        public int ResolutionScopeSize { get; }
        public int TypeOrMethodDefSize { get; }

        //Portable PDB
        public int HasCustomDebugInformationSize { get; }

        #endregion
        #region Read Index

        //Heaps

        internal int ReadStringHeapIndex() => ReadIndex(StringIndexSize);
        internal int ReadBlobHeapIndex() => ReadIndex(BlobIndexSize);
        internal int ReadGuidHeapIndex() => ReadIndex(GuidIndexSize);

        //Coded Index

        internal int ReadTypeDefOrRefIndex() => ReadIndex(TypeDefOrRefSize);
        internal int ReadHasConstantIndex() => ReadIndex(HasConstantSize);
        internal int ReadHasCustomAttributeIndex() => ReadIndex(HasCustomAttributeSize);
        internal int ReadHasFieldMarshalIndex() => ReadIndex(HasFieldMarshalSize);
        internal int ReadHasDeclSecurityIndex() => ReadIndex(HasDeclSecuritySize);
        internal int ReadMemberRefParentIndex() => ReadIndex(MemberRefParentSize);
        internal int ReadHasSemanticsIndex() => ReadIndex(HasSemanticsSize);
        internal int ReadMethodDefOrRefIndex() => ReadIndex(MethodDefOrRefSize);
        internal int ReadMemberForwardedIndex() => ReadIndex(MemberForwardedSize);
        internal int ReadImplementationIndex() => ReadIndex(ImplementationSize);
        internal int ReadCustomAttributeTypeIndex() => ReadIndex(CustomAttributeTypeSize);
        internal int ReadResolutionScopeIndex() => ReadIndex(ResolutionScopeSize);
        internal int ReadTypeOrMethodDefIndex() => ReadIndex(TypeOrMethodDefSize);

        //Portable PDB

        internal int ReadHasCustomDebugInformationIndex() => ReadIndex(HasCustomDebugInformationSize);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ReadSimpleIndex(TableKind tableKind) => ReadIndex(GetSimpleIndexSize(tableKind));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ReadIndex(int size) => size == 4 ? reader.ReadInt32() : reader.ReadUInt16();

        #endregion

        public int Position => (int) reader.Position;

        internal byte ReadByte() => reader.ReadByte();
        internal short ReadInt16() => reader.ReadInt16();
        internal int ReadInt32() => reader.ReadInt32();
        internal uint ReadUInt32() => reader.ReadUInt32();

        internal void Enter() => reader.Enter();
        internal void Exit() => reader.Exit();

        private IFileReader reader;
        private int[] rowCounts;

        internal MetadataReader(IFileReader reader, HeapSizes heapSizes, int[] rowCounts)
        {
            this.reader = reader;

            StringIndexSize = heapSizes.HasFlag(HeapSizes.HEAP_STRING_4) ? 4 : 2;
            GuidIndexSize = heapSizes.HasFlag(HeapSizes.HEAP_GUID_4) ? 4 : 2;
            BlobIndexSize = heapSizes.HasFlag(HeapSizes.HEAP_BLOB_4) ? 4 : 2;

            this.rowCounts = rowCounts;

            TypeDefOrRefSize = GetCodedIndexSize(CodedIndexTag.TypeDefOrRef);
            HasConstantSize = GetCodedIndexSize(CodedIndexTag.HasConstant);
            HasCustomAttributeSize = GetCodedIndexSize(CodedIndexTag.HasCustomAttribute);
            HasFieldMarshalSize = GetCodedIndexSize(CodedIndexTag.HasFieldMarshal);
            HasDeclSecuritySize = GetCodedIndexSize(CodedIndexTag.HasDeclSecurity);
            MemberRefParentSize = GetCodedIndexSize(CodedIndexTag.MemberRefParent);
            HasSemanticsSize = GetCodedIndexSize(CodedIndexTag.HasSemantics);
            MethodDefOrRefSize = GetCodedIndexSize(CodedIndexTag.MethodDefOrRef);
            MemberForwardedSize = GetCodedIndexSize(CodedIndexTag.MemberForwarded);
            ImplementationSize = GetCodedIndexSize(CodedIndexTag.Implementation);
            CustomAttributeTypeSize = GetCodedIndexSize(CodedIndexTag.CustomAttributeType);
            ResolutionScopeSize = GetCodedIndexSize(CodedIndexTag.ResolutionScope);
            TypeOrMethodDefSize = GetCodedIndexSize(CodedIndexTag.TypeOrMethodDef);

            HasCustomDebugInformationSize = GetCodedIndexSize(CodedIndexTag.HasCustomDebugInformation);
        }

        internal void Seek(int offset)
        {
            if (Position == offset)
                return;

            reader.Seek(offset);
        }

        internal int GetCodedIndexSize(CodedIndexTag tag)
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

            if (numRows > ushort.MaxValue)
                return 4;

            return 2;
        }

        internal int GetRowCount(TableKind tableKind) => rowCounts[(int) tableKind];
    }
}
