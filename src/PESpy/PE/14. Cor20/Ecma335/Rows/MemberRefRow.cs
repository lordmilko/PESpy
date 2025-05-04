using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Class = {Class}, Name = {Name.ToString(),nq}, Signature = {Signature}")]
    public readonly struct MemberRefRow : IValue, IViewable
    {
        public MemberRefIndex RowIndex { get; }

        public int Class => table.GetClass(RowIndex);

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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MemberRef Row", this, ViewKind.Metadata_MemberRefRow);

            s.WriteMemberRefParentIndex(nameof(Class), Class);
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(Signature), Signature);
        }
    }
}
