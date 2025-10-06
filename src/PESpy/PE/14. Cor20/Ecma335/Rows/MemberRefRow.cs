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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteMemberRefParentIndex(nameof(Class), (int) Class);
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(Signature), Signature);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
