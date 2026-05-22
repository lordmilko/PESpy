using System.Diagnostics;
using PESpy.Ecma335;

namespace PESpy.Mstat
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct MstatAssemblyRow
    {
        private string DebuggerDisplay()
        {
            using var builder = new ValueStringBuilder();

            builder.Append(ToString());
            builder.Append(" (");
            builder.AppendSize(TotalSize);
            builder.Append(')');

            return builder.ToString();
        }

        public StringIndex Name => _table.GetRefRow(_rowId).Name;

        public int TotalSize => _table.GetMstatRow(_rowId).TotalSize;

        public MstatHeap.Enumerator<MstatTypeDefTable, MstatTypeDefRow, MstatTypeDefTable.AssemblyTypeRefIterator> Types =>
            new(_table.GetMstatRow(_rowId).FirstTypeRef.Rid, _table._mstatHeap.TypeDefTable);

        public MstatHeap.Enumerator<MstatManifestResourceTable, MstatManifestResourceRow, MstatAssemblyTable.ManifestResourceIterator> ManifestResources =>
            new(_table.GetMstatRow(_rowId).FirstManifestResource, _table._mstatHeap.ManifestResourceTable);

        private readonly MstatAssemblyTable _table;
        private readonly int _rowId;

        internal MstatAssemblyRow(MstatAssemblyTable table, int rowId)
        {
            _table = table;
            _rowId = rowId;
        }

        public override string ToString()
        {
            return Name.GetString().ToString();
        }
    }
}
