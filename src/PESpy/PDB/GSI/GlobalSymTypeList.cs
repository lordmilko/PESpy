using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    internal class GlobalSymTypeListDebugView
    {
        private readonly GlobalSymTypeList list;

        public GlobalSymTypeListDebugView(GlobalSymTypeList list)
        {
            this.list = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SymType[] Items => list.ToArray();
    }

    //PERF: don't allocate a massive array of SymType
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(GlobalSymTypeListDebugView))]
    public unsafe class GlobalSymTypeList : IEnumerable<SymType>
    {
        private readonly NativeSpan<HRFile> hashRecords;
        private readonly byte* symbolsStart;

        public int Count => hashRecords.Length;

        public GlobalSymTypeList(NativeSpan<HRFile> hashRecords, byte* symbolsStart)
        {
            this.hashRecords = hashRecords;
            this.symbolsStart = symbolsStart;
        }

        public SymType this[int index]
        {
            get
            {
                ref var record = ref hashRecords[index];

                return (SYMTYPE*) (symbolsStart + record.off - 1);
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(hashRecords, symbolsStart);

        IEnumerator<SymType> IEnumerable<SymType>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<SymType>
        {
            private readonly NativeSpan<HRFile> hashRecords;
            private readonly byte* symbolsStart;
            private int index;

            internal Enumerator(NativeSpan<HRFile> hashRecords, byte* symbolsStart)
            {
                this.hashRecords = hashRecords;
                this.symbolsStart = symbolsStart;
                index = 0;
                Current = default;
            }

            public bool MoveNext()
            {
                if (index >= hashRecords.Length)
                {
                    Current = default;
                    return false;
                }

                ref var record = ref hashRecords[index];
                index++;

                Current = (SYMTYPE*) (symbolsStart + record.off - 1);
                return true;
            }

            public SymType Current { get; private set; }

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
