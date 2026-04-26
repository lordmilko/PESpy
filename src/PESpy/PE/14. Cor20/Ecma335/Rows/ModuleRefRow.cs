using System;
using PESpy.View;

namespace PESpy.Ecma335
{
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

        //Extensions
        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_ModuleRefRow, table.RowSize);

        int IViewable.NumChildren() => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Name.GetString().ToString();
        }
    }
}
