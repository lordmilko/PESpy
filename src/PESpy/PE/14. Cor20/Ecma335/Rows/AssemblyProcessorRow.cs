using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Processor = {Processor}")]
    public readonly struct AssemblyProcessorRow : IValue, IViewable
    {
        public AssemblyProcessorIndex RowIndex { get; }

        public int Processor => table.GetProcessor(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly AssemblyProcessorTable table;

        internal AssemblyProcessorRow(AssemblyProcessorIndex index, AssemblyProcessorTable table)
        {
            //II.22.4

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.AssemblyProcessorRow, this, ViewKind.Metadata_AssemblyProcessorRow, table.RowSize);

        int IViewable.NumChildren => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Processor), table.ProcessorOffset, Processor);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
