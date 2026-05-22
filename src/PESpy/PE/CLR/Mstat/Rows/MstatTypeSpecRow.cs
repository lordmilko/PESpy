using PESpy.Ecma335;

namespace PESpy.Mstat
{
    public readonly struct MstatTypeSpecRow
    {
        public BlobIndex Signature => _table.GetRefRow(_rowId).Signature;

        public MstatHeap.Enumerator<MstatMemberTable, MstatMemberRow, MstatHeap.TypeMemberIterator> Members =>
            new(_table.GetMstatRow(_rowId).FirstMemberRef, _table._mstatHeap.MemberTable);

        public MstatHeap.Enumerator<MstatFrozenObjectTable, MstatFrozenObjectRow, MstatHeap.FrozenObjectIterator> FrozenObjects =>
            new(_table.GetMstatRow(_rowId).FirstFrozenObject, _table._mstatHeap.FrozenObjectTable);

        private readonly MstatTypeSpecTable _table;
        private readonly int _rowId;

        internal MstatTypeSpecRow(MstatTypeSpecTable table, int rowId)
        {
            _table = table;
            _rowId = rowId;
        }

        public override string ToString() => _table.GetRefRow(_rowId).ToString();
    }
}
