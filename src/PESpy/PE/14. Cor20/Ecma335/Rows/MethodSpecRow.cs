using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Method = {Method}, Instantiation = {Instantiation}")]
    public readonly struct MethodSpecRow : IValue, IViewable
    {
        public MethodSpecIndex RowIndex { get; }

        public CodedIndex Method => table.GetMethod(RowIndex);

        public BlobIndex Instantiation => table.GetInstantiation(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly MethodSpecTable table;

        internal MethodSpecRow(MethodSpecIndex index, MethodSpecTable table)
        {
            //II.22.29

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MethodSpecRow, this, ViewKind.Metadata_MethodSpecRow, table.RowSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteMethodDefOrRefIndex(nameof(Method), table.MethodOffset, Method);
                    break;

                case 1:
                    structWriter.WriteBlobHeapIndex(nameof(Instantiation), table.InstantiationOffset, Instantiation);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
