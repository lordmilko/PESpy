using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Number = {Number}, Flags = {Flags}, Owner = {Owner}, Name = {Name.ToString(),nq}")]
    public readonly struct GenericParamRow : IValue, IViewable
    {
        public GenericParamIndex RowIndex { get; }

        public short Number => table.GetNumber(RowIndex);

        public CorGenericParamAttr Flags => table.GetFlags(RowIndex);

        public Index Owner => table.GetOwner(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly GenericParamTable table;

        internal GenericParamRow(GenericParamIndex index, GenericParamTable table)
        {
            //II.22.20

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.GenericParamRow, this, ViewKind.Metadata_GenericParamRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Number), Number);
            s.WriteValue(nameof(Flags), Flags, sizeof(short));
            s.WriteTypeOrMethodDefIndex(nameof(Owner), (int) Owner);
            s.WriteStringHeapIndex(nameof(Name), Name);

            return s.ToArray();
        }
    }
}
