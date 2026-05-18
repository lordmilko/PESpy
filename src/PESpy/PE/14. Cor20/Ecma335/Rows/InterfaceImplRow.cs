using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("{ClassRow.ToString(),nq} : {InterfaceRow.ToString(),nq}")]
    public readonly struct InterfaceImplRow : IValue, IViewable
    {
        public InterfaceImplIndex RowIndex { get; }

        public TypeDefIndex Class => table.GetClass(RowIndex);

        public CodedIndex Interface => table.GetInterface(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public TypeDefRow ClassRow => table.ModelHeap.TypeDefTable[Class];

        public object InterfaceRow => Interface.GetRow(table.ModelHeap);

        private readonly InterfaceImplTable table;

        internal InterfaceImplRow(InterfaceImplIndex index, InterfaceImplTable table)
        {
            //II.22.23

            RowIndex = index;
            this.table = table;
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_InterfaceImplRow, table.RowSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteSimpleIndex(nameof(Class), table.ClassOffset, (int) Class, TableKind.TypeDef);
                    break;

                case 1:
                    structWriter.WriteTypeDefOrRefIndex(nameof(Interface), table.InterfaceOffset, Interface);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
