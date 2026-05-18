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
        private readonly ModelHeap modelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal InterfaceImplList(TypeDefIndex implementingType, ModelHeap modelHeap)
        {
            this.modelHeap = modelHeap;
            var interfaceImplTable = modelHeap.InterfaceImplTable;

            if (interfaceImplTable != null)
                interfaceImplTable.GetRange(implementingType, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            }
        }

        //0-based index
        public InterfaceImplRow this[int index] => modelHeap.InterfaceImplTable[(InterfaceImplIndex) (firstRowId + index)];

        public Enumerator GetEnumerator() => new Enumerator(modelHeap, firstRowId, lastRowId);

        IEnumerator<InterfaceImplRow> IEnumerable<InterfaceImplRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<InterfaceImplRow>
        {
            private readonly int lastRowId;
            private InterfaceImplTable table;
            private int currentRowId;

            internal Enumerator(ModelHeap modelHeap, int firstRowId, int lastRowId)
            {
                table = modelHeap?.InterfaceImplTable;
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
