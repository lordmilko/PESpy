using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.Ecma335
{
    public class TableDebugView<T>
    {
        private Table<T> table;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => table.ToArray();

        public TableDebugView(Table<T> table)
        {
            this.table = table;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(TableDebugView<>))]
    public abstract class Table<T> : IEnumerable<T>
    {
        public int Count { get; }

        protected Table(int numRows)
        {
            Count = numRows;
        }

        public T this[int index]
        {
            get
            {
                //Per ECMA-335 II.22, indexes are 1 based. If an index of 0 is specified, it means
                //that the value is essentially a "null reference"

                if (index == 0)
                    return default!;

                if (index > Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return GetRow(index);
            }
        }

        protected abstract T GetRow(int index);

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            public T Current { get; private set; }

            object? IEnumerator.Current => Current;

            private readonly Table<T> table;
            private int index;

            internal Enumerator(Table<T> table)
            {
                this.table = table;
                index = 1;
                Current = default!;
            }

            public bool MoveNext()
            {
                if (index > table.Count)
                {
                    Current = default!;
                    return false;
                }

                Current = table[index];
                index++;
                return true;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
