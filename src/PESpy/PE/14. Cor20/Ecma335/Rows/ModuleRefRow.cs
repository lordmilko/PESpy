using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Name = {Name.ToString(),nq}")]
    public readonly struct ModuleRefRow : IValue, IViewable
    {
        public ModuleRefIndex RowIndex { get; }

        public StringIndex Name => table.GetName(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ModuleRefTable table;

        internal ModuleRefRow(ModuleRefIndex index, ModuleRefTable table)
        {
            //II.22.31

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ModuleRef Row", this, ViewKind.Metadata_ModuleRefRow);

            s.WriteStringHeapIndex(nameof(Name), Name);
        }
    }
}
