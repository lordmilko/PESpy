using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Method = {Method}, Instantiation = {Instantiation}")]
    public readonly struct MethodSpecRow : IValue, IViewable
    {
        public MethodSpecIndex RowIndex { get; }

        public Index Method => table.GetMethod(RowIndex);

        public BlobIndex Instantiation => table.GetInstantiation(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly MethodSpecTable table;

        internal MethodSpecRow(MethodSpecIndex index, MethodSpecTable table)
        {
            //II.22.29

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MethodSpecRow, this, ViewKind.Metadata_MethodSpecRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteMethodDefOrRefIndex(nameof(Method), (int) Method);
            s.WriteBlobHeapIndex(nameof(Instantiation), Instantiation);

            return s.ToArray();
        }
    }
}
