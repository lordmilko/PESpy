using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    internal class MethodDefListDebugView
    {
        private MethodDefList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public MethodDefRow[] Items => list.ToArray();

        internal MethodDefListDebugView(MethodDefList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(MethodDefListDebugView))]
    public readonly struct MethodDefList : IEnumerable<MethodDefRow>
    {
        private readonly CompressedModelHeap compressedModelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal MethodDefList(TypeDefIndex containingType, CompressedModelHeap compressedModelHeap)
        {
            this.compressedModelHeap = compressedModelHeap;
            var MethodDefTable = compressedModelHeap.MethodDefTable;

            if (MethodDefTable != null)
                MethodDefTable.GetRange(containingType, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            }
        }

        //0-based index
        public MethodDefRow this[int index] => compressedModelHeap.MethodDefTable[(MethodDefIndex) (firstRowId + index)];

        public MethodDefRow this[string name]
        {
            get
            {
                foreach (var item in this)
                {
                    if (item.Name.GetString() == name)
                        return item;
                }

                throw new InvalidOperationException($"Failed to find a method named '{name}'");
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(compressedModelHeap, firstRowId, lastRowId);

        IEnumerator<MethodDefRow> IEnumerable<MethodDefRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<MethodDefRow>
        {
            private readonly int lastRowId;
            private MethodDefTable table;
            private int currentRowId;

            internal Enumerator(CompressedModelHeap compressedModelHeap, int firstRowId, int lastRowId)
            {
                table = compressedModelHeap?.MethodDefTable;
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

            public MethodDefRow Current
            {
                get
                {
                    return table[(MethodDefIndex) currentRowId];
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
