using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("ResourceOffset = {ResourceOffset}, Flags = {Flags}, Name = {Name.ToString(),nq}, Implementation = {ImplementationRow}")]
    public readonly struct ManifestResourceRow : IValue, IViewable
    {
        public ManifestResourceIndex RowIndex { get; }

        public int ResourceOffset => table.GetResourceOffset(RowIndex);

        public CorManifestResourceFlags Flags => table.GetFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public CodedIndex Implementation => table.GetImplementation(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object ImplementationRow => Implementation.GetRow(table.ModelHeap);

        private readonly ManifestResourceTable table;

        internal ManifestResourceRow(ManifestResourceIndex index, ManifestResourceTable table)
        {
            //II.22.24

            RowIndex = index;
            this.table = table;
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_ManifestResourceRow, table.RowSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(ResourceOffset), table.ResourceOffsetOffset, ResourceOffset);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(int));
                    break;

                case 2:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 3:
                    structWriter.WriteImplementationIndex(nameof(Implementation), table.ImplementationOffset, Implementation);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString() => Name.GetString().ToString();
    }
}
