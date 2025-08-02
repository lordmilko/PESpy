using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {Parent}, Kind = {Kind}, Value = {Value}")]
    public readonly struct CustomDebugInformationRow : IValue, IViewable
    {
        public CustomDebugInformationIndex RowIndex { get; }

        public Index Parent => table.GetParent(RowIndex);

        public GuidIndex Kind => table.GetKind(RowIndex);

        public BlobIndex Value => table.GetValue(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly CustomDebugInformationTable table;

        internal CustomDebugInformationRow(CustomDebugInformationIndex index, CustomDebugInformationTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CustomDebugInformationRow, this, ViewKind.PortablePdb_CustomDebugInformationRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteHasCustomDebugInformationIndex(nameof(Parent), (int) Parent);
            s.WriteGuidHeapIndex(nameof(Kind), Kind);
            s.WriteBlobHeapIndex(nameof(Value), Value);

            return s.ToArray();
        }
    }
}
