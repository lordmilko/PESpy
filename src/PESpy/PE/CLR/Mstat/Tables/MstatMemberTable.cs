using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug;
using PESpy.Ecma335;

namespace PESpy.Mstat
{
    internal class MstatMemberTableDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public MstatMemberRow[] Items { get; }

        internal MstatMemberTableDebugView(MstatMemberTable table)
        {
            Items = table.ToArray();
        }
    }

    [DebuggerTypeProxy(typeof(MstatMemberTableDebugView))]
    public class MstatMemberTable : IEnumerable<MstatMemberRow>
    {
        public int Size { get; internal set; }

        private readonly MemberRefTable _refTable;
        internal readonly MstatHeap _mstatHeap;

        private RowBuilder[] _rowBuilders;

        internal MstatMemberTable(MemberRefTable refTable, MstatHeap mstatHeap)
        {
            _refTable = refTable;
            _mstatHeap = mstatHeap;
            _rowBuilders = new RowBuilder[refTable.Count];
        }

        public MstatMemberRow this[int index] => new MstatMemberRow(this, index + 1);

        internal ref RowBuilder GetRowBuilder(mdMemberRef token, ref MstatHeapBuilder builder)
        {
            ref var rowBuilder = ref _rowBuilders[token.Rid - 1]; //RID is 1-based

            if (!rowBuilder.IsInitialized)
                rowBuilder.Initialize(token, ref builder);

            return ref rowBuilder;
        }

        internal ref RowBuilder GetMstatRow(int rowId) => ref _rowBuilders[rowId - 1];

        internal MemberRefRow GetRefRow(int rowId) => _refTable[rowId - 1];

        internal struct RowBuilder
        {
            public mdMemberRef NextMemberRef;

            public mdMethodSpec FirstMethodSpec;

            public int Size;
            public int TotalSize;

            internal bool IsInitialized;

            internal void Initialize(mdMemberRef token, ref MstatHeapBuilder builder)
            {
                var memberRef = builder.ModelHeap.MemberRefTable.FromToken(token);

                if (memberRef.Class.TableKind == TableKind.TypeRef)
                {
                    //Update the linked list of members that the parent class owns
                    ref var parentBuilder = ref builder.MstatHeap.TypeDefTable.GetRowBuilder((mdTypeRef) memberRef.Class, ref builder);

                    NextMemberRef = parentBuilder.FirstMemberRef;
                    parentBuilder.FirstMemberRef = token;
                }
                else
                {
                    //Update the linked list of members that the parent class owns
                    ref var parentBuilder = ref builder.MstatHeap.TypeSpecTable.GetRowBuilder((mdTypeSpec) memberRef.Class, ref builder);

                    NextMemberRef = parentBuilder.FirstMemberRef;
                    parentBuilder.FirstMemberRef = token;
                }

                IsInitialized = true;
            }

            internal void PropagateSize(mdMemberRef token, int size, ref MstatHeapBuilder builder)
            {
                TotalSize += size;

                var memberRef = builder.ModelHeap.MemberRefTable.FromToken(token);

                if (memberRef.Class.TableKind == TableKind.TypeRef)
                    builder.MstatHeap.TypeDefTable.GetRowBuilder((mdTypeRef) memberRef.Class, ref builder).PropagateSize((mdTypeRef) memberRef.Class, size, ref builder);
                else
                    builder.MstatHeap.TypeSpecTable.GetRowBuilder((mdTypeSpec) memberRef.Class, ref builder).PropagateSize((mdTypeSpec) memberRef.Class, size, ref builder);
            }
        }

        public struct MemberIterator : MstatHeap.IMstatIterator<MstatMemberTable, MstatMemberRow>
        {
            public MstatMemberRow GetCurrent(MstatMemberTable table, int currentRowId) =>
                new MstatMemberRow(table, currentRowId);

            public int MoveNext(MstatMemberTable table, int currentRowId)
            {
                if (currentRowId < table._rowBuilders.Length)
                    return currentRowId + 1;

                //We're the last RID, so there's no further token
                return default;
            }
        }

        public struct MethodSpecIterator : MstatHeap.IMstatIterator<MstatMethodSpecTable, MstatMethodSpecRow>
        {
            public MstatMethodSpecRow GetCurrent(MstatMethodSpecTable table, int currentRowId) =>
                new MstatMethodSpecRow(table, currentRowId);

            public int MoveNext(MstatMethodSpecTable table, int currentRowId) => table.GetMstatRow(currentRowId).NextMethodSpec.Rid;
        }

        public MstatHeap.Enumerator<MstatMemberTable, MstatMemberRow, MemberIterator> GetEnumerator() => new(1, this);

        IEnumerator<MstatMemberRow> IEnumerable<MstatMemberRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
