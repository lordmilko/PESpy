using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#nullable disable

namespace PESpy.Ecma335
{
    internal class GenericParamListDebugView
    {
        private GenericParamList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public GenericParamRow[] Items => list.ToArray();

        internal GenericParamListDebugView(GenericParamList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(GenericParamListDebugView))]
    public readonly struct GenericParamList : IEnumerable<GenericParamRow>
    {
        private readonly ModelHeap modelHeap;
        private readonly int firstRowId;
        private readonly int lastRowId;

        public int Count => lastRowId - firstRowId;

        internal GenericParamList(int firstRowId, ushort count, ModelHeap modelHeap)
        {
            this.modelHeap = modelHeap;
            this.firstRowId = firstRowId;
            this.lastRowId = firstRowId + count;
        }

        //0-based index
        public GenericParamRow this[int index] => modelHeap.GenericParamTable[(GenericParamIndex) (firstRowId + index)];

        public GenericParamRow this[string name]
        {
            get
            {
                foreach (var item in this)
                {
                    if (item.Name.GetString() == name)
                        return item;
                }

                throw new InvalidOperationException($"Failed to find a generic param named '{name}'");
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(modelHeap, firstRowId, lastRowId);

        IEnumerator<GenericParamRow> IEnumerable<GenericParamRow>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<GenericParamRow>
        {
            private readonly int lastRowId;
            private GenericParamTable table;
            private int currentRowId;

            internal Enumerator(ModelHeap modelHeap, int firstRowId, int lastRowId)
            {
                table = modelHeap?.GenericParamTable;
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

            public GenericParamRow Current
            {
                get
                {
                    return table[(GenericParamIndex) currentRowId];
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
