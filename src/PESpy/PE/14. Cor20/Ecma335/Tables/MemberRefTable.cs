using System;

namespace PESpy.Ecma335
{
    public sealed class MemberRefTable : Table<MemberRefRow>
    {
        internal readonly int ClassOffset;
        internal readonly int NameOffset;
        internal readonly int SignatureOffset;

        private readonly bool isBigMemberRefParentIndex;
        private readonly bool isBigStringIndex;
        private readonly bool isBigBlobIndex;

        internal readonly CompressedModelHeap CompressedModelHeap;
        private readonly Func<StringHeap?> stringHeap;
        private readonly Func<BlobHeap?> blobHeap;

        internal MemberRefTable(
            int numRows,
            int memberRefParentIndexSize,
            int stringIndexSize,
            int blobIndexSize,
            CompressedModelHeap compressedModelHeap,
            Func<StringHeap?> stringHeap,
            Func<BlobHeap?> blobHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            //II.22.25

            CompressedModelHeap = compressedModelHeap;
            this.stringHeap = stringHeap;
            this.blobHeap = blobHeap;

            isBigMemberRefParentIndex = memberRefParentIndexSize == 4;
            isBigStringIndex = stringIndexSize == 4;
            isBigBlobIndex = blobIndexSize == 4;

            ClassOffset = 0;
            NameOffset = ClassOffset + memberRefParentIndexSize;
            SignatureOffset = NameOffset + stringIndexSize;
            RowSize = SignatureOffset + blobIndexSize;
        }

        public CodedIndex GetClass(MemberRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekCodedIndex(rowOffset + ClassOffset, isBigMemberRefParentIndex, CodedIndexType.MemberRefParent);
        }

        public StringIndex GetName(MemberRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        public BlobIndex GetSignature(MemberRefIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new BlobIndex(tableChunk.PeekEcmaIndex(rowOffset + SignatureOffset, isBigBlobIndex), blobHeap);
        }

        public MemberRefRow this[string name]
        {
            get
            {
                var dot = name.LastIndexOf('.');

                if (dot == -1)
                    throw new ArgumentException($"Name '{name}' does not contain a dot");

                //If you have Foo..ctor you'll have two dots in a row,
                //so check if we've got a double dot

                if (dot > 0 && name[dot - 1] == '.')
                    dot--;

                var type = name.AsSpan(0, dot);
                var member = name.AsSpan(dot + 1);

                ReadOnlySpan<char> ns = default;

                dot = type.LastIndexOf('.');

                if (dot != -1)
                {
                    ns = type.Slice(0, dot);
                    type = type.Slice(dot + 1);
                }

                var heap = CompressedModelHeap;

                foreach (var item in this)
                {
                    switch (item.Class.TableKind)
                    {
                        case TableKind.TypeDef:
                            var typeDef = heap.TypeDefTable[item.Class];

                            if (typeDef.TypeNamespace.GetString() != ns || typeDef.TypeName.GetString() != type)
                                continue;

                            break;

                        case TableKind.TypeRef:
                            var typeRef = heap.TypeRefTable[item.Class];

                            if (typeRef.TypeNamespace.GetString() != ns || typeRef.TypeName.GetString() != type)
                                continue;

                            break;

                        case TableKind.ModuleRef:
                            throw new NotImplementedException();

                        case TableKind.MethodDef:
                            throw new NotImplementedException();

                        case TableKind.TypeSpec:
                            var typeSpec = heap.TypeSpecTable[item.Class];

                            //Matching TypeSpecs is not yet supported. I'm not exactly sure what I would do
                            continue;
                    }

                    if (item.Name.GetString() == member)
                        return item;
                }

                throw new InvalidOperationException($"Failed to find a type named '{name}'");
            }
        }

        public CustomAttributeList GetCustomAttributes(MemberRefIndex index) =>
            new CustomAttributeList(CompressedModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.MemberRef));

        public int GetRowOffset(MemberRefIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public MemberRefRow this[MemberRefIndex index] => GetRowSafe((int) index);

        protected override MemberRefRow GetRow(int index) => new MemberRefRow((MemberRefIndex) index, this);
    }
}
