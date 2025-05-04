using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("AssemblyProcessor Row", this, ViewKind.Metadata_AssemblyProcessorRow);

            s.WriteValue(nameof(Processor), Processor);
        }
    }
}
