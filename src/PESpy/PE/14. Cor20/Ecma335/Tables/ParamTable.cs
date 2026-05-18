using System;
using ClrDebug;

namespace PESpy.Ecma335
{
    public sealed class ParamTable : Table<ParamRow>
    {
        private readonly bool isBigStringIndex;

        internal readonly int FlagsOffset;
        internal readonly int SequenceOffset;
        internal readonly int NameOffset;

        internal readonly ModelHeap ModelHeap;
        private readonly Func<StringHeap?> stringHeap;

        internal ParamTable(
            int numRows,
            int stringIndexSize,
            ModelHeap modelHeap,
            Func<StringHeap?> stringHeap,
            in MemoryChunk tableChunk) : base(tableChunk, numRows)
        {
            ModelHeap = modelHeap;
            this.stringHeap = stringHeap;

            isBigStringIndex = stringIndexSize == 4;

            FlagsOffset = 0;
            SequenceOffset = FlagsOffset + sizeof(ushort);
            NameOffset = SequenceOffset + sizeof(ushort);
            RowSize = NameOffset + stringIndexSize;
        }

        public CorParamAttr GetFlags(ParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return (CorParamAttr) tableChunk.PeekUInt16(rowOffset + FlagsOffset);
        }

        public short GetSequence(ParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return tableChunk.PeekInt16(rowOffset + SequenceOffset);
        }

        public StringIndex GetName(ParamIndex index)
        {
            var rowOffset = (index.RowId - 1) * RowSize;
            return new StringIndex(tableChunk.PeekEcmaIndex(rowOffset + NameOffset, isBigStringIndex), stringHeap);
        }

        internal void GetRange(MethodDefIndex methodDef, out int firstParamRowId, out int lastParamRowId)
        {
            firstParamRowId = (int) ModelHeap.MethodDefTable.GetParamList(methodDef);

            if (firstParamRowId == 0)
            {
                firstParamRowId = 0;
                lastParamRowId = 0;
            }
            else if (methodDef.RowId == ModelHeap.MethodDefTable.Count)
            {
                lastParamRowId = (ModelHeap.MethodPtrTable?.Count > 0 ? ModelHeap.MethodPtrTable.Count : Count) + 1;
            }
            else
            {
                lastParamRowId = (int) ModelHeap.MethodDefTable.GetParamList((MethodDefIndex) (methodDef.RowId + 1));
            }
        }

        public CustomAttributeList GetCustomAttributes(ParamIndex index) =>
            new CustomAttributeList(ModelHeap, HasCustomAttributeTag.CreateIndex((int) index, TableKind.Param));

        public long GetRowOffset(ParamIndex index) => tableChunk.AbsoluteOffset + (index.RowId - 1) * RowSize;

        public ParamRow this[ParamIndex index] => GetRowSafe((int) index);

        protected override ParamRow GetRow(int index) => new ParamRow((ParamIndex) index, this);
    }
}
