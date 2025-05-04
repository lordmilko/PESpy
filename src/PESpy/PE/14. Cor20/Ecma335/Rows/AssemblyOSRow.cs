using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("OSPlatformID = {OSPlatformID}, OSMajorVersion = {OSMajorVersion}, OSMinorVersion = {OSMinorVersion}")]
    public readonly struct AssemblyOSRow : IValue, IViewable
    {
        public AssemblyOSIndex RowIndex { get; }

        public int OSPlatformID => table.GetOSPlatformID(RowIndex);

        public int OSMajorVersion => table.GetOSMajorVersion(RowIndex);

        public int OSMinorVersion => table.GetOSMinorVersion(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly AssemblyOSTable table;

        internal AssemblyOSRow(AssemblyOSIndex index, AssemblyOSTable table)
        {
            //II.22.3

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("AssemblyOS Row", this, ViewKind.Metadata_AssemblyOSRow);

            s.WriteValue(nameof(OSPlatformID), OSPlatformID);
            s.WriteValue(nameof(OSMajorVersion), OSMajorVersion);
            s.WriteValue(nameof(OSMinorVersion), OSMinorVersion);
        }
    }
}
