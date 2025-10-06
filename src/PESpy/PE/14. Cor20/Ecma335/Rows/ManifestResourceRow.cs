using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("ResourceOffset = {ResourceOffset}, Flags = {Flags}, Name = {Name.ToString(),nq}, Implementation = {Implementation}")]
    public readonly struct ManifestResourceRow : IValue, IViewable
    {
        public ManifestResourceIndex RowIndex { get; }

        public int ResourceOffset => table.GetResourceOffset(RowIndex);

        public CorManifestResourceFlags Flags => table.GetFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public Index Implementation => table.GetImplementation(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ManifestResourceTable table;

        internal ManifestResourceRow(ManifestResourceIndex index, ManifestResourceTable table)
        {
            //II.22.24

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ManifestResourceRow, this, ViewKind.Metadata_ManifestResourceRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(ResourceOffset), ResourceOffset);
            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteImplementationIndex(nameof(Implementation), (int) Implementation);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
