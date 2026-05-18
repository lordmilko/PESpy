using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    internal class EventListDebugView
    {
        private EventList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public EventRow[] Items => list.ToArray();

        internal EventListDebugView(EventList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(EventListDebugView))]
    public readonly struct EventList : IEnumerable<EventRow>
    {
        private readonly ModelHeap modelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal EventList(TypeDefIndex containingType, ModelHeap modelHeap)
        {
            this.modelHeap = modelHeap;
            var eventMapTable = modelHeap.EventMapTable;

            if (eventMapTable != null)
                eventMapTable.GetRange(containingType, out firstRowId, out lastRowId);
            else
            {
                firstRowId = 0;
                lastRowId = 0;
            }
        }

        //0-based index
        public EventRow this[int index] => modelHeap.EventTable[(EventIndex) (firstRowId + index)];

        public Enumerator GetEnumerator() => new Enumerator(modelHeap, firstRowId, lastRowId);

        IEnumerator<EventRow> IEnumerable<EventRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<EventRow>
        {
            private readonly int lastRowId;
            private EventTable table;
            private int currentRowId;

            internal Enumerator(ModelHeap modelHeap, int firstRowId, int lastRowId)
            {
                table = modelHeap?.EventTable;
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

            public EventRow Current
            {
                get
                {
                    return table[(EventIndex) currentRowId];
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
