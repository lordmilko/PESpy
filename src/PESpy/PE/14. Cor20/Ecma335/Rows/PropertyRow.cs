using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Flags = {Flags}, Name = {Name.ToString(),nq}, Type = {Type}")]
    public readonly struct PropertyRow : IValue, IViewable
    {
        public PropertyIndex RowIndex { get; }

        public CorPropertyAttr Flags => table.GetFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public BlobIndex Type => table.GetType(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly PropertyTable table;

        internal PropertyRow(PropertyIndex index, PropertyTable table)
        {
            //II.22.34

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.PropertyRow, this, ViewKind.Metadata_PropertyRow, table.RowSize);

        int IViewable.NumChildren => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 2:
                    structWriter.WriteBlobHeapIndex(nameof(Type), table.TypeOffset, Type);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
