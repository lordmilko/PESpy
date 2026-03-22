using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    internal class DeclSecurityAttributeListDebugView
    {
        private DeclSecurityAttributeList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DeclSecurityRow[] Items => list.ToArray();

        internal DeclSecurityAttributeListDebugView(DeclSecurityAttributeList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(DeclSecurityAttributeListDebugView))]
    public readonly struct DeclSecurityAttributeList : IEnumerable<DeclSecurityRow>
    {
        private readonly CompressedModelHeap compressedModelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal DeclSecurityAttributeList(CompressedModelHeap compressedModelHeap, CodedIndex index)
        {
            this.compressedModelHeap = compressedModelHeap;
            var declSecurityTable = compressedModelHeap.DeclSecurityTable;

            if (declSecurityTable != null)
                compressedModelHeap.DeclSecurityTable.GetRange(index, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            }
        }

        //0-based index
        public DeclSecurityRow this[int index] => compressedModelHeap.DeclSecurityTable[(DeclSecurityIndex) (firstRowId + index)];

        public Enumerator GetEnumerator() => new Enumerator(compressedModelHeap, firstRowId, lastRowId);

        IEnumerator<DeclSecurityRow> IEnumerable<DeclSecurityRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<DeclSecurityRow>
        {
            private readonly int lastRowId;
            private DeclSecurityTable table;
            private int currentRowId;

            internal Enumerator(CompressedModelHeap compressedModelHeap, int firstRowId, int lastRowId)
            {
                table = compressedModelHeap?.DeclSecurityTable;
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

            public DeclSecurityRow Current
            {
                get
                {
                    return table[(DeclSecurityIndex) currentRowId];
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
