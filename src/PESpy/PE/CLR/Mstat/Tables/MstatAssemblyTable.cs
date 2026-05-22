using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug;
using PESpy.Ecma335;

namespace PESpy.Mstat
{
    internal class MstatAssemblyTableDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public MstatAssemblyRow[] Items { get; }

        internal MstatAssemblyTableDebugView(MstatAssemblyTable table)
        {
            Items = table.ToArray();
        }
    }

    [DebuggerTypeProxy(typeof(MstatAssemblyTableDebugView))]
    public class MstatAssemblyTable : IEnumerable<MstatAssemblyRow>
    {
        private readonly AssemblyRefTable _refTable;
        internal readonly MstatHeap _mstatHeap;

        private readonly RowBuilder[] _rowBuilders;

        internal MstatAssemblyTable(AssemblyRefTable refTable, MstatHeap mstatHeap)
        {
            _refTable = refTable;
            _mstatHeap = mstatHeap;
            _rowBuilders = new RowBuilder[refTable.Count];
        }

        public MstatAssemblyRow this[int index] => new MstatAssemblyRow(this, index + 1);

        internal ref RowBuilder GetRowBuilder(mdAssemblyRef token, ref MstatHeapBuilder builder)
        {
            ref var rowBuilder = ref _rowBuilders[token.Rid - 1]; //RID is 1-based

            return ref rowBuilder;
        }

        internal ref RowBuilder GetMstatRow(int rowId) => ref _rowBuilders[rowId - 1];

        internal AssemblyRefRow GetRefRow(int rowId) => _refTable[rowId - 1];

        internal struct RowBuilder
        {
            //Used to maintain a linked list of top-level types
            public mdTypeRef FirstTypeRef;

            public int FirstManifestResource;

            public int TotalSize;

            internal void PropagateSize(int size)
            {
                TotalSize += size;
            }
        }

        public struct AssemblyRefIterator : MstatHeap.IMstatIterator<MstatAssemblyTable, MstatAssemblyRow>
        {
            public MstatAssemblyRow GetCurrent(MstatAssemblyTable table, int currentRowId) =>
                new MstatAssemblyRow(table, currentRowId);

            public int MoveNext(MstatAssemblyTable table, int currentRowId)
            {
                if (currentRowId < table._rowBuilders.Length)
                    return currentRowId + 1;

                //We're the last RID, so there's no further token
                return default;
            }
        }

        public struct ManifestResourceIterator : MstatHeap.IMstatIterator<MstatManifestResourceTable, MstatManifestResourceRow>
        {
            public MstatManifestResourceRow GetCurrent(MstatManifestResourceTable table, int currentRowId) =>
                table[currentRowId - 1];

            public int MoveNext(MstatManifestResourceTable table, int currentRowId) =>
                table[currentRowId - 1].NextManifestResource;
        }

        public MstatHeap.Enumerator<MstatAssemblyTable, MstatAssemblyRow, AssemblyRefIterator> GetEnumerator() => new(1, this);

        IEnumerator<MstatAssemblyRow> IEnumerable<MstatAssemblyRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
