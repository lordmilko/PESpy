using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("OSPlatformID = {OSPlatformID}, OSMajorVersion = {OSMajorVersion}, OSMinorVersion = {OSMinorVersion}, AssemblyRef = {AssemblyRefRow}")]
    public readonly struct AssemblyRefOSRow : IValue, IViewable
    {
        public AssemblyRefOSIndex RowIndex { get; }

        public int OSPlatformID => table.GetOSPlatformID(RowIndex);
        public int OSMajorVersion => table.GetOSMajorVersion(RowIndex);
        public int OSMinorVersion => table.GetOSMinorVersion(RowIndex);

        public AssemblyRefIndex AssemblyRef => table.GetAssemblyRef(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public AssemblyRefRow AssemblyRefRow => table.ModelHeap.AssemblyRefTable[AssemblyRef];

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
            writer.NewStruct(this, ViewKind.Metadata_AssemblyRefOSRow, table.RowSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(OSPlatformID), table.OSPlatformIDOffset, OSPlatformID);
                    break;

                case 1:
                    structWriter.WriteField(nameof(OSMajorVersion), table.OSMajorVersionOffset, OSMajorVersion);
                    break;

                case 2:
                    structWriter.WriteField(nameof(OSMinorVersion), table.OSMinorVersionOffset, OSMinorVersion);
                    break;

                case 3:
                    structWriter.WriteSimpleIndex(nameof(AssemblyRef), table.AssemblyRefOffset, (int) AssemblyRef, TableKind.AssemblyRef);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
