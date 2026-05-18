using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    internal class PropertyListDebugView
    {
        private PropertyList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public PropertyRow[] Items => list.ToArray();

        internal PropertyListDebugView(PropertyList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(PropertyListDebugView))]
    public readonly struct PropertyList : IEnumerable<PropertyRow>
    {
        private readonly ModelHeap modelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal PropertyList(TypeDefIndex containingType, ModelHeap modelHeap)
        {
            this.modelHeap = modelHeap;
            var propertyTable = modelHeap.PropertyTable;

            if (propertyTable != null)
                propertyTable.GetRange(containingType, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            }
        }

        //0-based index
        public PropertyRow this[int index] => modelHeap.PropertyTable[(PropertyIndex) (firstRowId + index)];

        public Enumerator GetEnumerator() => new Enumerator(modelHeap, firstRowId, lastRowId);

        IEnumerator<PropertyRow> IEnumerable<PropertyRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<PropertyRow>
        {
            private readonly int lastRowId;
            private PropertyTable table;
            private int currentRowId;

            internal Enumerator(ModelHeap modelHeap, int firstRowId, int lastRowId)
            {
                table = modelHeap?.PropertyTable;
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

            public PropertyRow Current
            {
                get
                {
                    return table[(PropertyIndex) currentRowId];
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
