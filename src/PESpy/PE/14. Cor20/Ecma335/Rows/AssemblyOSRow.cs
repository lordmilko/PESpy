using System;
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

        int IViewable.NumChildren => 3;

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

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
