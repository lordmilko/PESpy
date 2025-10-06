using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Flags = {Flags}, Sequence = {Sequence}, Name = {Name.ToString(),nq}")]
    public readonly struct ParamRow : IValue, IViewable
    {
        public ParamIndex RowIndex { get; }

        public CorParamAttr Flags => table.GetFlags(RowIndex);

        public short Sequence => table.GetSequence(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ParamTable table;

        internal ParamRow(ParamIndex index, ParamTable table)
        {
            //II.22.33

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ParamRow, this, ViewKind.Metadata_ParamRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Flags), Flags, sizeof(short));
            s.WriteValue(nameof(Sequence), Sequence);
            s.WriteStringHeapIndex(nameof(Name), Name);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
