using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Processor = {Processor}, AssemblyRef = {AssemblyRef}")]
    public readonly struct AssemblyRefProcessorRow : IValue, IViewable
    {
        public AssemblyRefProcessorIndex RowIndex { get; }

        public int Processor => table.GetProcessor(RowIndex);

        public AssemblyRefIndex AssemblyRef => table.GetAssemblyRef(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly AssemblyRefProcessorTable table;

        internal AssemblyRefProcessorRow(AssemblyRefProcessorIndex index, AssemblyRefProcessorTable table)
        {
            //II.22.7

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("AssemblyRefProcessor Row", this, ViewKind.Metadata_AssemblyRefProcessorRow);

            s.WriteValue(nameof(Processor), Processor);
            s.WriteSimpleIndex(nameof(AssemblyRef), (int) AssemblyRef, TableKind.AssemblyRef);
        }
    }
}
