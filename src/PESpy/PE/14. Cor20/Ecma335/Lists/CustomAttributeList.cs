using System;
using System.Collections;
using System.Collections.Generic;

namespace PESpy.Ecma335
{
    public readonly struct CustomAttributeList : IEnumerable<CustomAttributeRow>
    {
        private readonly CompressedModelHeap compressedModelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        //todo: what if we're actually a default item, we should have no rows in that case
        public int Count => lastRowId - firstRowId + 1;

        internal CustomAttributeList(CompressedModelHeap compressedModelHeap, CodedIndex index)
        {
            this.compressedModelHeap = compressedModelHeap;
            var customAttributeTable = compressedModelHeap.CustomAttributeTable;

            if (customAttributeTable != null)
                customAttributeTable.GetRange(index, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            };
        }

        //0-based index
        public CustomAttributeRow this[int index] => compressedModelHeap.CustomAttributeTable[firstRowId + index];

        public Enumerator GetEnumerator() => new Enumerator(compressedModelHeap, firstRowId, lastRowId);

        IEnumerator<CustomAttributeRow> IEnumerable<CustomAttributeRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<CustomAttributeRow>
        {
            private readonly int lastRowId;
            private CustomAttributeTable table;
            private int currentRowId;

            internal Enumerator(CompressedModelHeap compressedModelHeap, int firstRowId, int lastRowId)
            {
                table = compressedModelHeap.CustomAttributeTable;
                currentRowId = firstRowId - 1;
                this.lastRowId = lastRowId;
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

            public CustomAttributeRow Current
            {
                get
                {
                    if (table.SortedTable != null)
                        throw new NotImplementedException("Getting a custom attribute where we had to sort parents is not implemented");
                    
                    return table[currentRowId];
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
