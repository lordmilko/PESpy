using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug;

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

        internal readonly MemoryChunk tableChunk;
        internal int RowSize;

        internal Table(in MemoryChunk tableChunk, int numRows)
        {
            this.tableChunk = tableChunk;

            Count = numRows;
        }

        //I wanted to allow indexing by CodedIndex, mdToken, element index and specific token type,
        //but it seems the compiler gets confused due to the implicit conversions to int we have. So unfortunately
        //we need to use methods instead

        public T this[CodedIndex index] => GetRow(index.RowId);

        public T FromToken(mdToken token) => GetRow(token.Rid);

        //0-based. I tried making this the 1-based row index but it just causes chaos
        public T this[int elementIndex]
        {
            get
            {
                //Per ECMA-335 II.22, indexes are 1 based. If an index of 0 is specified, it means
                //that the value is essentially a "null reference". However, all of our row types
                //will explode if you try and access their members when they don't have an underlying table.
                //Thus, we allow passing various index types in to safely get the rows associated with them
                if (elementIndex >= Count)
                    throw new ArgumentOutOfRangeException(nameof(elementIndex));

                return GetRow(elementIndex + 1);
            }
        }

        protected abstract T GetRow(int index);

        internal T FromOffset(int offset)
        {
            var diff = offset - tableChunk.AbsoluteOffset;

            var index = diff / RowSize;

            return GetRow(index + 1);
        }

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

                Current = table.GetRow(index);
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
