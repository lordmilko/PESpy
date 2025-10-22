using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Class = {Class}, Name = {Name.ToString(),nq}, Signature = {Signature}")]
    public readonly struct MemberRefRow : IValue, IViewable
    {
        public MemberRefIndex RowIndex { get; }

        public Index Class => table.GetClass(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public BlobIndex Signature => table.GetSignature(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly MemberRefTable table;

        internal MemberRefRow(MemberRefIndex index, MemberRefTable table)
        {
            //II.22.25

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MemberRefRow, this, ViewKind.Metadata_MemberRefRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteMemberRefParentIndex(nameof(Class), table.ClassOffset, (int) Class);
                    break;

                case 1:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 2:
                    structWriter.WriteBlobHeapIndex(nameof(Signature), table.SignatureOffset, Signature);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
