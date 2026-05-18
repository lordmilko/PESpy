using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    internal class LocalScopeListDebugView
    {
        private LocalScopeList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public LocalScopeRow[] Items => list.ToArray();

        internal LocalScopeListDebugView(LocalScopeList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(LocalScopeListDebugView))]
    public readonly struct LocalScopeList : IEnumerable<LocalScopeRow>
    {
        private readonly ModelHeap modelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal LocalScopeList(MethodDefIndex methodDef, ModelHeap modelHeap)
        {
            this.modelHeap = modelHeap;
            var localScopeTable = modelHeap.LocalScopeTable;

            if (localScopeTable != null)
                localScopeTable.GetRange(methodDef, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            }
        }

        //0-based index
        public LocalScopeRow this[int index] => modelHeap.LocalScopeTable[(LocalScopeIndex) (firstRowId + index)];

        public Enumerator GetEnumerator() => new Enumerator(modelHeap, firstRowId, lastRowId);

        IEnumerator<LocalScopeRow> IEnumerable<LocalScopeRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<LocalScopeRow>
        {
            private readonly int lastRowId;
            private LocalScopeTable table;
            private int currentRowId;

            internal Enumerator(ModelHeap modelHeap, int firstRowId, int lastRowId)
            {
                table = modelHeap?.LocalScopeTable;
                currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId - 1;
            }

            public bool MoveNext()
            {
                if (currentRowId >= lastRowId)
                {
                    currentRowId = ModelHeap.EnumEnded;
                    return false;
                }
                else
                {
                    currentRowId++;
                    return true;
                }
            }

            public LocalScopeRow Current
            {
                get
                {
                    return table[(LocalScopeIndex) currentRowId];
                }
            }

            object IEnumerator.Current => Current;

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
