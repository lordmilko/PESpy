using System;
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

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Number), table.NumberOffset, Number);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(short));
                    break;

                case 2:
                    structWriter.WriteTypeOrMethodDefIndex(nameof(Owner), table.OwnerOffset, (int) Owner);
                    break;

                case 3:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
