using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Mstat
{
    internal class MstatFrozenObjectTableDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public MstatFrozenObjectRow[] Items { get; }

        internal MstatFrozenObjectTableDebugView(MstatFrozenObjectTable table)
        {
            Items = table.ToArray();
        }
    }

    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    [DebuggerTypeProxy(typeof(MstatFrozenObjectTableDebugView))]
    public class MstatFrozenObjectTable : IEnumerable<MstatFrozenObjectRow>
    {
        private string DebuggerDisplay()
        {
            using var builder = new ValueStringBuilder();

            builder.Append("Frozen Objects");
            builder.Append(" (");
            builder.AppendSize(_unownedFrozenObjectSize); //Sizoscope only reports the unowned object size for some reason
            builder.Append(')');

            return builder.ToString();
        }

        public int Size => _ownedFrozenObjectSize + _unownedFrozenObjectSize;

        private MstatFrozenObjectRow[] _rows;
        private int _ownedFrozenObjectSize;
        private int _unownedFrozenObjectSize;
        private int _firstUnownedFrozenObject;

        internal MstatFrozenObjectTable(
            MstatFrozenObjectRow[] rows,
            int ownedFrozenObjectSize,
            int unownedFrozenObjectSize,
            int firstUnownedFrozenObject)
        {
            _rows = rows;
            _ownedFrozenObjectSize = ownedFrozenObjectSize;
            _unownedFrozenObjectSize= unownedFrozenObjectSize;
            _firstUnownedFrozenObject = firstUnownedFrozenObject;
        }

        public MstatFrozenObjectRow this[int index] => _rows[index];

        public IEnumerator<MstatFrozenObjectRow> GetEnumerator() => ((IEnumerable<MstatFrozenObjectRow>) _rows).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _rows.GetEnumerator();
    }
}
