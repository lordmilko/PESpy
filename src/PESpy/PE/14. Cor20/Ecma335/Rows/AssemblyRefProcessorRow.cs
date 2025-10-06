using System.Diagnostics;
using PESpy.View;

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.AssemblyRefProcessorRow, this, ViewKind.Metadata_AssemblyRefProcessorRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Processor), Processor);
            s.WriteSimpleIndex(nameof(AssemblyRef), (int) AssemblyRef, TableKind.AssemblyRef);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
