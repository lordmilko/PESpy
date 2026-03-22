using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    internal class ParamListDebugView
    {
        private ParamList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ParamRow[] Items => list.ToArray();

        internal ParamListDebugView(ParamList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ParamListDebugView))]
    public readonly struct ParamList : IEnumerable<ParamRow>
    {
        private readonly CompressedModelHeap compressedModelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal ParamList(MethodDefIndex containingMethod, CompressedModelHeap compressedModelHeap)
        {
            this.compressedModelHeap = compressedModelHeap;
            var ParamTable = compressedModelHeap.ParamTable;

            if (ParamTable != null)
                ParamTable.GetRange(containingMethod, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            }
        }

        //0-based index
        public ParamRow this[int index] => compressedModelHeap.ParamTable[(ParamIndex) (firstRowId + index)];

        public Enumerator GetEnumerator() => new Enumerator(compressedModelHeap, firstRowId, lastRowId);

        IEnumerator<ParamRow> IEnumerable<ParamRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<ParamRow>
        {
            private readonly int lastRowId;
            private ParamTable table;
            private int currentRowId;

            internal Enumerator(CompressedModelHeap compressedModelHeap, int firstRowId, int lastRowId)
            {
                table = compressedModelHeap?.ParamTable;
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

            public ParamRow Current
            {
                get
                {
                    return table[(ParamIndex) currentRowId];
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
