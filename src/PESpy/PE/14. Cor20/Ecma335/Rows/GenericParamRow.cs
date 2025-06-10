using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("GenericParam Row", this, ViewKind.Metadata_GenericParamRow);

            s.WriteValue(nameof(Number), Number);
            s.WriteValue(nameof(Flags), Flags, sizeof(short));
            s.WriteTypeOrMethodDefIndex(nameof(Owner), (int) Owner);
            s.WriteStringHeapIndex(nameof(Name), Name);
        }
    }
}
