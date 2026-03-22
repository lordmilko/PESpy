using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    internal class MethodImplListDebugView
    {
        private MethodImplList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public MethodImplRow[] Items => list.ToArray();

        internal MethodImplListDebugView(MethodImplList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(MethodImplListDebugView))]
    public readonly struct MethodImplList : IEnumerable<MethodImplRow>
    {
        private readonly CompressedModelHeap compressedModelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal MethodImplList(TypeDefIndex containingType, CompressedModelHeap compressedModelHeap)
        {
            this.compressedModelHeap = compressedModelHeap;
            var methodImplTable = compressedModelHeap.MethodImplTable;

            if (methodImplTable != null)
                methodImplTable.GetRange(containingType, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            }
        }

        //0-based index
        public MethodImplRow this[int index] => compressedModelHeap.MethodImplTable[(MethodImplIndex) (firstRowId + index)];

        public Enumerator GetEnumerator() => new Enumerator(compressedModelHeap, firstRowId, lastRowId);

        IEnumerator<MethodImplRow> IEnumerable<MethodImplRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<MethodImplRow>
        {
            private readonly int lastRowId;
            private MethodImplTable table;
            private int currentRowId;

            internal Enumerator(CompressedModelHeap compressedModelHeap, int firstRowId, int lastRowId)
            {
                table = compressedModelHeap?.MethodImplTable;
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

            public MethodImplRow Current
            {
                get
                {
                    return table[(MethodImplIndex) currentRowId];
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
