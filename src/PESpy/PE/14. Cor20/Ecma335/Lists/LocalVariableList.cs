using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    internal class LocalVariableListDebugView
    {
        private LocalVariableList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public LocalVariableRow[] Items => list.ToArray();

        internal LocalVariableListDebugView(LocalVariableList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(LocalVariableListDebugView))]
    public readonly struct LocalVariableList : IEnumerable<LocalVariableRow>
    {
        private readonly CompressedModelHeap compressedModelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal LocalVariableList(LocalScopeIndex scope, CompressedModelHeap compressedModelHeap)
        {
            this.compressedModelHeap = compressedModelHeap;
            var LocalVariableTable = compressedModelHeap.LocalVariableTable;

            if (LocalVariableTable != null)
                LocalVariableTable.GetRange(scope, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            }
        }

        //0-based index
        public LocalVariableRow this[int index] => compressedModelHeap.LocalVariableTable[(LocalVariableIndex) (firstRowId + index)];

        public Enumerator GetEnumerator() => new Enumerator(compressedModelHeap, firstRowId, lastRowId);

        IEnumerator<LocalVariableRow> IEnumerable<LocalVariableRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<LocalVariableRow>
        {
            private readonly int lastRowId;
            private LocalVariableTable table;
            private int currentRowId;

            internal Enumerator(CompressedModelHeap compressedModelHeap, int firstRowId, int lastRowId)
            {
                table = compressedModelHeap?.LocalVariableTable;
                currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId - 1;
            }

            public bool MoveNext()
            {
                if (currentRowId >= lastRowId)
                {
                    currentRowId = CompressedModelHeap.EnumEnded;
                    return false;
                }
                else
                {
                    currentRowId++;
                    return true;
                }
            }

            public LocalVariableRow Current
            {
                get
                {
                    return table[(LocalVariableIndex) currentRowId];
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
