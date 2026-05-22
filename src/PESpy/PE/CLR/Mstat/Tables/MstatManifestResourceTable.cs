using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Mstat
{
    internal class MstatManifestResourceTableDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public MstatManifestResourceRow[] Items { get; }

        internal MstatManifestResourceTableDebugView(MstatManifestResourceTable table)
        {
            Items = table.ToArray();
        }
    }

    [DebuggerTypeProxy(typeof(MstatManifestResourceTableDebugView))]
    public class MstatManifestResourceTable : IEnumerable<MstatManifestResourceRow>
    {
        public int Size { get; }

        private MstatManifestResourceRow[] _rows;

        internal MstatManifestResourceTable(MstatManifestResourceRow[] rows, int size)
        {
            _rows = rows;
            Size = size;
        }

        public MstatManifestResourceRow this[int index] => _rows[index];

        public IEnumerator<MstatManifestResourceRow> GetEnumerator() => ((IEnumerable<MstatManifestResourceRow>) _rows).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _rows.GetEnumerator();
    }
}
