using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("ResourceOffset = {ResourceOffset}, Flags = {Flags}, Name = {Name.ToString(),nq}, Implementation = {Implementation}")]
    public readonly struct ManifestResourceRow : IValue, IViewable
    {
        public ManifestResourceIndex RowIndex { get; }

        public int ResourceOffset => table.GetResourceOffset(RowIndex);

        public CorManifestResourceFlags Flags => table.GetFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public int Implementation => table.GetImplementation(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ManifestResourceTable table;

        internal ManifestResourceRow(ManifestResourceIndex index, ManifestResourceTable table)
        {
            //II.22.24

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ManifestResource Row", this, ViewKind.Metadata_ManifestResourceRow);

            s.WriteValue(nameof(ResourceOffset), ResourceOffset);
            s.WriteValue(nameof(Flags), Flags, sizeof(int));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteImplementationIndex(nameof(Implementation), Implementation);
        }
    }
}
