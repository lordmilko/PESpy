using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
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

        private Func<MetadataReader, T> createRow;
        private MetadataReader metadataReader;
        private int rowSize;

        internal Table(RawOffset offset, MetadataReader metadataReader, int count, int rowSize, Func<MetadataReader, T> createRow)
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

        public T this[int index]
        {
            get
            {
                if (index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                metadataReader.Enter();

                try
                {
                    metadataReader.Seek(Offset + (index * rowSize));

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
                index = 0;
                Current = default;
            }

            public bool MoveNext()
            {
                if (index >= table.Count)
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
}
