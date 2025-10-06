using System.Diagnostics;
using PESpy.View;

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.AssemblyOSRow, this, ViewKind.Metadata_AssemblyOSRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(OSPlatformID), OSPlatformID);
            s.WriteValue(nameof(OSMajorVersion), OSMajorVersion);
            s.WriteValue(nameof(OSMinorVersion), OSMinorVersion);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
