using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug;
using PESpy.Ecma335;

namespace PESpy.Mstat
{
    internal class MstatMethodSpecTableDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public MstatMethodSpecRow[] Items { get; }

        internal MstatMethodSpecTableDebugView(MstatMethodSpecTable table)
        {
            Items = table.ToArray();
        }
    }

    [DebuggerTypeProxy(typeof(MstatMethodSpecTableDebugView))]
    public class MstatMethodSpecTable : IEnumerable<MstatMethodSpecRow>
    {
        public int Size { get; internal set; }

        private readonly MethodSpecTable _refTable;
        internal readonly MstatHeap _mstatHeap;

        private RowBuilder[] _rowBuilders;

        internal MstatMethodSpecTable(MethodSpecTable refTable, MstatHeap mstatHeap)
        {
            _refTable = refTable;
            _mstatHeap = mstatHeap;
            _rowBuilders = new RowBuilder[refTable.Count];
        }

        public MstatMethodSpecRow this[int index] => new MstatMethodSpecRow(this, index + 1);

        internal ref RowBuilder GetRowBuilder(mdMethodSpec token, ref MstatHeapBuilder builder)
        {
            ref var rowBuilder = ref _rowBuilders[token.Rid - 1]; //RID is 1-based

            if (!rowBuilder.IsInitialized)
                rowBuilder.Initialize(token, ref builder);

            return ref rowBuilder;
        }

        internal ref RowBuilder GetMstatRow(int rowId) => ref _rowBuilders[rowId - 1];

        internal MethodSpecRow GetRefRow(int rowId) => _refTable[rowId - 1];

        internal struct RowBuilder
        {
            public mdMethodSpec NextMethodSpec;

            public int Size;

            internal bool IsInitialized;

            internal void Initialize(mdMethodSpec token, ref MstatHeapBuilder builder)
            {
                var methodSpec = builder.ModelHeap.MethodSpecTable.FromToken(token);

                ref var memberRefBuilder = ref builder.MstatHeap.MemberTable.GetRowBuilder((mdMemberRef) methodSpec.Method, ref builder);
                NextMethodSpec = memberRefBuilder.FirstMethodSpec;
                memberRefBuilder.FirstMethodSpec = token;

                IsInitialized = true;
            }

            internal void PropagateSize(mdMethodSpec token, int size, ref MstatHeapBuilder builder)
            {
                var methodSpec = builder.ModelHeap.MethodSpecTable.FromToken(token);

                var memberToken = (mdMemberRef) methodSpec.Method;

                ref var memberRefBuilder = ref builder.MstatHeap.MemberTable.GetRowBuilder(memberToken, ref builder);

                memberRefBuilder.PropagateSize(memberToken, size, ref builder);
            }
        }

        public struct MethodSpecIterator : MstatHeap.IMstatIterator<MstatMethodSpecTable, MstatMethodSpecRow>
        {
            public MstatMethodSpecRow GetCurrent(MstatMethodSpecTable table, int currentRowId) =>
                new MstatMethodSpecRow(table, currentRowId);

            public int MoveNext(MstatMethodSpecTable table, int currentRowId)
            {
                if (currentRowId < table._rowBuilders.Length)
                    return currentRowId + 1;

                //We're the last RID, so there's no further token
                return default;
            }
        }

        public MstatHeap.Enumerator<MstatMethodSpecTable, MstatMethodSpecRow, MethodSpecIterator> GetEnumerator() => new(1, this);

        IEnumerator<MstatMethodSpecRow> IEnumerable<MstatMethodSpecRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
