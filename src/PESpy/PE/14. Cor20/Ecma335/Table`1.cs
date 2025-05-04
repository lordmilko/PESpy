using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
#if PEFAST
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
                    return default;

                if (index > Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return GetRow(index);
            }
        }

        protected abstract T GetRow(int index);

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator : IEnumerator<T>
        {
            public T? Current { get; private set; }

            object? IEnumerator.Current => Current;

            private readonly Table<T> table;
            private int index;

            internal Enumerator(Table<T> table)
            {
                this.table = table;
                index = 1;
                Current = default;
            }

            public bool MoveNext()
            {
                if (index > table.Count)
                    return false;

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
#else
    //Allocating an actual table that contains every single metadata row consumes quite a bit of memory, hence why we virtualize it

    /// <summary>
    /// Provides lazy access to the rows in a given metadata table.<para/>
    /// This type does not have a well-known native struct definition.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public class Table<T> : IValue, IEnumerable<T>
    {
        public int Count { get; }

        public RawOffset Offset { get; }

        internal delegate T CreateRowDelegate(MetadataReader metadataReader);

        private CreateRowDelegate createRow;

        private MetadataReader metadataReader;
        private int rowSize;

        internal Table(RawOffset offset, MetadataReader metadataReader, int count, int rowSize, CreateRowDelegate createRow)
        {
            Offset = offset;
            this.metadataReader = metadataReader;
            Count = count;
            this.rowSize = rowSize;
            this.createRow = createRow;

#if STRESS_TEST
            _ = this[0]; //Test deserializing one
#endif
        }

        public T this[int entryNo]
        {
            get
            {
                //Per ECMA-335 II.22, indexes are 1 based. If an index of 0 is specified, it means
                //that the value is essentially a "null reference"

                if (entryNo == 0)
                    return default;

                if (entryNo > Count)
                    throw new ArgumentOutOfRangeException(nameof(entryNo));

                metadataReader.Enter();

                try
                {
                    metadataReader.Seek(Offset + ((entryNo - 1) * rowSize));

                    return createRow(metadataReader);
                }
                finally
                {
                    metadataReader.Exit();
                }
            }
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator : IEnumerator<T>
        {
            public T Current { get; private set; }

            object IEnumerator.Current => Current;

            private Table<T> table;
            private int index;

            internal Enumerator(Table<T> table)
            {
                this.table = table;
                index = 1;
                Current = default;
            }

            public bool MoveNext()
            {
                if (index > table.Count)
                    return false;

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
#endif
}
