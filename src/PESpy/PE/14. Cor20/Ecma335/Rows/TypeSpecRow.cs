using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Signature = {Signature}")]
    public readonly struct TypeSpecRow : IValue, IViewable
    {
        public TypeSpecIndex RowIndex { get; }

        public BlobIndex Signature => table.GetSignature(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly TypeSpecTable table;

        internal TypeSpecRow(TypeSpecIndex index, TypeSpecTable table)
        {
            //II.22.39

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.TypeSpecRow, this, ViewKind.Metadata_TypeSpecRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteBlobHeapIndex(nameof(Signature), Signature);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
