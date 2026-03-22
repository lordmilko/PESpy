using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    internal class FieldDefListDebugView
    {
        private FieldDefList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public FieldRow[] Items => list.ToArray();

        internal FieldDefListDebugView(FieldDefList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(FieldDefListDebugView))]
    public readonly struct FieldDefList : IEnumerable<FieldRow>
    {
        private readonly CompressedModelHeap compressedModelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal FieldDefList(TypeDefIndex containingType, CompressedModelHeap compressedModelHeap)
        {
            this.compressedModelHeap = compressedModelHeap;
            var fieldTable = compressedModelHeap.FieldTable;

            if (fieldTable != null)
                fieldTable.GetRange(containingType, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            }
        }

        //0-based index
        public FieldRow this[int index] => compressedModelHeap.FieldTable[(FieldIndex) (firstRowId + index)];

        public FieldRow this[string name]
        {
            get
            {
                foreach (var item in this)
                {
                    if (item.Name.GetString() == name)
                        return item;
                }

                throw new InvalidOperationException($"Failed to find a field named '{name}'");
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(compressedModelHeap, firstRowId, lastRowId);

        IEnumerator<FieldRow> IEnumerable<FieldRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<FieldRow>
        {
            private readonly int lastRowId;
            private FieldTable table;
            private int currentRowId;

            internal Enumerator(CompressedModelHeap compressedModelHeap, int firstRowId, int lastRowId)
            {
                table = compressedModelHeap?.FieldTable;
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

            public FieldRow Current
            {
                get
                {
                    return table[(FieldIndex) currentRowId];
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
