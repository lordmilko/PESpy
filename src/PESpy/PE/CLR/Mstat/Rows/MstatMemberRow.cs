using System.Diagnostics;
using ClrDebug;
using PESpy.Ecma335;

namespace PESpy.Mstat
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct MstatMemberRow
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

        public CodedIndex Class => _table.GetRefRow(_rowId).Class;

        public bool IsField
        {
            get
            {
                var memberRef = _table.GetRefRow(_rowId);
                var blobReader = memberRef.Signature.GetReader();
                var callingConv = (CorCallingConvention) blobReader.ReadByte();
                return callingConv == CorCallingConvention.FIELD;
            }
        }

        public int TotalSize => _table.GetMstatRow(_rowId).TotalSize;

        public MstatHeap.Enumerator<MstatMethodSpecTable, MstatMethodSpecRow, MstatMemberTable.MethodSpecIterator> MethodSpecs =>
            new(_table.GetMstatRow(_rowId).FirstMethodSpec, _table._mstatHeap.MethodSpecTable);

        private readonly MstatMemberTable _table;
        private readonly int _rowId;

        internal MstatMemberRow(MstatMemberTable table, int rowId)
        {
            _table = table;
            _rowId = rowId;
        }

        public override string ToString() => _table.GetRefRow(_rowId).ToString();
    }
}
