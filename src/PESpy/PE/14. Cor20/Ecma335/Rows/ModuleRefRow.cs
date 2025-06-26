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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct("ModuleRef Row", this, ViewKind.Metadata_ModuleRefRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteStringHeapIndex(nameof(Name), Name);

            return s.ToArray();
        }
    }
}
