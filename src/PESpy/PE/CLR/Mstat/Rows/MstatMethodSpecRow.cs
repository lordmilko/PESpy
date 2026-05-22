namespace PESpy.Mstat
{
    public readonly struct MstatMethodSpecRow
    {
        public int Size => _table.GetMstatRow(_rowId).Size;

        private readonly MstatMethodSpecTable _table;
        private readonly int _rowId;

        internal MstatMethodSpecRow(MstatMethodSpecTable table, int rowId)
        {
            _table = table;
            _rowId = rowId;
        }

        public override string ToString() => _table.GetRefRow(_rowId).ToString();
    }
}
