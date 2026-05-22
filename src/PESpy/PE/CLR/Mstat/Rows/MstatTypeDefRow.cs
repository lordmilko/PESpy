using System.Diagnostics;
using PESpy.Ecma335;

namespace PESpy.Mstat
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct MstatTypeDefRow
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

        public StringIndex TypeName => _table.GetRefRow(_rowId).TypeName;

        public StringIndex TypeNamespace => _table.GetRefRow(_rowId).TypeNamespace;

        public MstatHeap.Enumerator<MstatTypeDefTable, MstatTypeDefRow, MstatTypeDefTable.NestedTypeIterator> NestedTypes =>
            new(_table.GetMstatRow(_rowId).FirstNestedTypeRef.Rid, _table);

        public MstatHeap.Enumerator<MstatTypeSpecTable, MstatTypeSpecRow, MstatTypeDefTable.TypeSpecIterator> TypeSpecs =>
            new(_table.GetMstatRow(_rowId).FirstTypeSpec.Rid, _table._mstatHeap.TypeSpecTable);

        public MstatHeap.Enumerator<MstatMemberTable, MstatMemberRow, MstatHeap.TypeMemberIterator> Members =>
            new(_table.GetMstatRow(_rowId).FirstMemberRef.Rid, _table._mstatHeap.MemberTable);

        public MstatHeap.Enumerator<MstatFrozenObjectTable, MstatFrozenObjectRow, MstatHeap.FrozenObjectIterator> FrozenObjects =>
            new(_table.GetMstatRow(_rowId).FirstFrozenObject, _table._mstatHeap.FrozenObjectTable);

        public int TotalSize => _table.GetMstatRow(_rowId).TotalSize;

        private readonly MstatTypeDefTable _table;
        private readonly int _rowId;

        internal MstatTypeDefRow(MstatTypeDefTable table, int rowId)
        {
            _table = table;
            _rowId = rowId;
        }

        public override string ToString() => ModelHeap.FormatType(TypeNamespace, TypeName);
    }
}
