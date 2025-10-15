using System;
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

        int IViewable.NumChildren => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteField(nameof(Sequence), table.SequenceOffset, Sequence);
                    break;

                case 2:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
