using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    internal class CustomAttributeListDebugView
    {
        private CustomAttributeList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public CustomAttributeRow[] Items => list.ToArray();

        internal CustomAttributeListDebugView(CustomAttributeList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(CustomAttributeListDebugView))]
    public readonly struct CustomAttributeList : IEnumerable<CustomAttributeRow>
    {
        private readonly ModelHeap modelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal CustomAttributeList(ModelHeap modelHeap, CodedIndex index)
        {
            this.modelHeap = modelHeap;
            var customAttributeTable = modelHeap.CustomAttributeTable;

            if (customAttributeTable != null)
                customAttributeTable.GetRange(index, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            };
        }

        //0-based index
        public CustomAttributeRow this[int index] => modelHeap.CustomAttributeTable[(CustomAttributeIndex) (firstRowId + index)];

        public CustomAttributeRow this[string fullName]
        {
            get
            {
                var dot = fullName.LastIndexOf('.');

                if (dot == -1)
                {
                    foreach (var item in this)
                    {
                        if (item.TryGetName(out var namespaceIndex, out var nameIndex) && nameIndex.GetString() == fullName)
                            return item;
                    }
                }
                else
                {
                    var ns = fullName.AsSpan(0, dot);
                    var name = fullName.AsSpan(dot + 1);

                    foreach (var item in this)
                    {
                        if (item.TryGetName(out var namespaceIndex, out var nameIndex) && namespaceIndex.GetString() == ns && nameIndex.GetString() == name)
                            return item;
                    }
                }

                throw new NotImplementedException($"Failed to find a custom attribute named '{fullName}'");
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(modelHeap, firstRowId, lastRowId);

        IEnumerator<CustomAttributeRow> IEnumerable<CustomAttributeRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<CustomAttributeRow>
        {
            private readonly int lastRowId;
            private CustomAttributeTable table;
            private int currentRowId;

            internal Enumerator(ModelHeap modelHeap, int firstRowId, int lastRowId)
            {
                table = modelHeap?.CustomAttributeTable;
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

            public CustomAttributeRow Current
            {
                get
                {
                    if (table.SortedTable != null)
                        throw new NotImplementedException("Getting a custom attribute where we had to sort parents is not implemented");
                    
                    return table[(CustomAttributeIndex) currentRowId];
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
