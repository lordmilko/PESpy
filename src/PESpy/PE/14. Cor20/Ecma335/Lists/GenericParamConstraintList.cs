using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    internal class GenericParamConstraintListDebugView
    {
        private GenericParamConstraintList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public GenericParamConstraintRow[] Items => list.ToArray();

        internal GenericParamConstraintListDebugView(GenericParamConstraintList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(GenericParamConstraintListDebugView))]
    public readonly struct GenericParamConstraintList : IEnumerable<GenericParamConstraintRow>
    {
        private readonly CompressedModelHeap compressedModelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal GenericParamConstraintList(int firstRowId, ushort count, CompressedModelHeap compressedModelHeap)
        {
            this.compressedModelHeap = compressedModelHeap;
            this.firstRowId = firstRowId;
            this.lastRowId = firstRowId + count;
        }

        //0-based index
        public GenericParamConstraintRow this[int index] => compressedModelHeap.GenericParamConstraintTable[(GenericParamConstraintIndex) (firstRowId + index)];

        public Enumerator GetEnumerator() => new Enumerator(compressedModelHeap, firstRowId, lastRowId);

        IEnumerator<GenericParamConstraintRow> IEnumerable<GenericParamConstraintRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<GenericParamConstraintRow>
        {
            private readonly int lastRowId;
            private GenericParamConstraintTable table;
            private int currentRowId;

            internal Enumerator(CompressedModelHeap compressedModelHeap, int firstRowId, int lastRowId)
            {
                table = compressedModelHeap?.GenericParamConstraintTable;
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

            public GenericParamConstraintRow Current
            {
                get
                {
                    return table[(GenericParamConstraintIndex) currentRowId];
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
