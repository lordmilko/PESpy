using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("OSPlatformID = {OSPlatformID}, OSMajorVersion = {OSMajorVersion}, OSMinorVersion = {OSMinorVersion}, AssemblyRef = {AssemblyRef}")]
    public readonly struct AssemblyRefOSRow : IValue, IViewable
    {
        public AssemblyRefOSIndex RowIndex { get; }

        public int OSPlatformID => table.GetOSPlatformID(RowIndex);
        public int OSMajorVersion => table.GetOSMajorVersion(RowIndex);
        public int OSMinorVersion => table.GetOSMinorVersion(RowIndex);

        public AssemblyRefIndex AssemblyRef => table.GetAssemblyRef(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly AssemblyRefOSTable table;

        internal AssemblyRefOSRow(AssemblyRefOSIndex index, AssemblyRefOSTable table)
        {
            //II.22.6

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct("AssemblyRefOS Row", this, ViewKind.Metadata_AssemblyRefOSRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(OSPlatformID), OSPlatformID);
            s.WriteValue(nameof(OSMajorVersion), OSMajorVersion);
            s.WriteValue(nameof(OSMinorVersion), OSMinorVersion);

            s.WriteSimpleIndex(nameof(AssemblyRef), (int) AssemblyRef, TableKind.AssemblyRef);

            return s.ToArray();
        }
    }
}
