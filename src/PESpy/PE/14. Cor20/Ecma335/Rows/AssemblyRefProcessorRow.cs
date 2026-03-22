using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Processor = {Processor}, AssemblyRef = {AssemblyRefRow}")]
    public readonly struct AssemblyRefProcessorRow : IValue, IViewable
    {
        public AssemblyRefProcessorIndex RowIndex { get; }

        public int Processor => table.GetProcessor(RowIndex);

        public AssemblyRefIndex AssemblyRef => table.GetAssemblyRef(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public AssemblyRefRow AssemblyRefRow => table.CompressedModelHeap.AssemblyRefTable[AssemblyRef];

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

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Processor), table.ProcessorOffset, Processor);
                    break;

                case 1:
                    structWriter.WriteSimpleIndex(nameof(AssemblyRef), table.AssemblyRefOffset, (int) AssemblyRef, TableKind.AssemblyRef);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
