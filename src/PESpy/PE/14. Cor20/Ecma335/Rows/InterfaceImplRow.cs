using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Class = {Class}, Interface = {Interface}")]
    public readonly struct InterfaceImplRow : IValue, IViewable
    {
        public InterfaceImplIndex RowIndex { get; }

        public TypeDefIndex Class => table.GetClass(RowIndex);

        public Index Interface => table.GetInterface(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly InterfaceImplTable table;

        internal InterfaceImplRow(InterfaceImplIndex index, InterfaceImplTable table)
        {
            //II.22.23

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.InterfaceImplRow, this, ViewKind.Metadata_InterfaceImplRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteSimpleIndex(nameof(Class), (int) Class, TableKind.TypeDef);
            s.WriteTypeDefOrRefIndex(nameof(Interface), (int) Interface);

            return s.ToArray();
        }
    }
}
