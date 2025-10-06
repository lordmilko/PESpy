using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Param = {Param}")]
    public readonly struct ParamPtrRow : IValue, IViewable
    {
        public ParamPtrIndex RowIndex { get; }

        public ParamIndex Param => table.GetParam(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ParamPtrTable table;

        internal ParamPtrRow(ParamPtrIndex index, ParamPtrTable table)
        {
            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ParamPtrRow, this, ViewKind.Metadata_ParamPtrRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Param), (int) Param);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
