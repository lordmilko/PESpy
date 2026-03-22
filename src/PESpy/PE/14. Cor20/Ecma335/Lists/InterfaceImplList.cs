using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    internal class InterfaceImplListDebugView
    {
        private InterfaceImplList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public InterfaceImplRow[] Items => list.ToArray();

        internal InterfaceImplListDebugView(InterfaceImplList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(InterfaceImplListDebugView))]
    public readonly struct InterfaceImplList : IEnumerable<InterfaceImplRow>
    {
        private readonly CompressedModelHeap compressedModelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal InterfaceImplList(TypeDefIndex implementingType, CompressedModelHeap compressedModelHeap)
        {
            this.compressedModelHeap = compressedModelHeap;
            var interfaceImplTable = compressedModelHeap.InterfaceImplTable;

            if (interfaceImplTable != null)
                interfaceImplTable.GetRange(implementingType, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            }
        }

        //0-based index
        public InterfaceImplRow this[int index] => compressedModelHeap.InterfaceImplTable[(InterfaceImplIndex) (firstRowId + index)];

        public Enumerator GetEnumerator() => new Enumerator(compressedModelHeap, firstRowId, lastRowId);

        IEnumerator<InterfaceImplRow> IEnumerable<InterfaceImplRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<InterfaceImplRow>
        {
            private readonly int lastRowId;
            private InterfaceImplTable table;
            private int currentRowId;

            internal Enumerator(CompressedModelHeap compressedModelHeap, int firstRowId, int lastRowId)
            {
                table = compressedModelHeap?.InterfaceImplTable;
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

            public InterfaceImplRow Current
            {
                get
                {
                    return table[(InterfaceImplIndex) currentRowId];
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
