using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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
            writer.NewStruct("Property Row", this, ViewKind.Metadata_PropertyRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Flags), Flags, sizeof(short));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(Type), Type);

            return s.ToArray();
        }
    }
}
