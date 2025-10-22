using System;
using System.Diagnostics;
using PESpy.View;

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

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteSimpleIndex(nameof(Class), table.ClassOffset, (int) Class, TableKind.TypeDef);
                    break;

                case 1:
                    structWriter.WriteTypeDefOrRefIndex(nameof(Interface), table.InterfaceOffset, (int) Interface);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
