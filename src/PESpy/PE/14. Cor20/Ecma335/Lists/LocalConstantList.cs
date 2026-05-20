using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#nullable disable

namespace PESpy.Ecma335
{
    internal class LocalConstantListDebugView
    {
        private LocalConstantList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public LocalConstantRow[] Items => list.ToArray();

        internal LocalConstantListDebugView(LocalConstantList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(LocalConstantListDebugView))]
    public readonly struct LocalConstantList : IEnumerable<LocalConstantRow>
    {
        private readonly ModelHeap modelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal LocalConstantList(LocalScopeIndex scope, ModelHeap modelHeap)
        {
            this.modelHeap = modelHeap;
            var LocalConstantTable = modelHeap.LocalConstantTable;

            if (LocalConstantTable != null)
                LocalConstantTable.GetRange(scope, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            }
        }

        //0-based index
        public LocalConstantRow this[int index] => modelHeap.LocalConstantTable[(LocalConstantIndex) (firstRowId + index)];

        public Enumerator GetEnumerator() => new Enumerator(modelHeap, firstRowId, lastRowId);

        IEnumerator<LocalConstantRow> IEnumerable<LocalConstantRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<LocalConstantRow>
        {
            private readonly int lastRowId;
            private LocalConstantTable table;
            private int currentRowId;

            internal Enumerator(ModelHeap modelHeap, int firstRowId, int lastRowId)
            {
                table = modelHeap?.LocalConstantTable;
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

            public LocalConstantRow Current
            {
                get
                {
                    return table[(LocalConstantIndex) currentRowId];
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
