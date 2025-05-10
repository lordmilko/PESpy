using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    internal class SymTypeListDebugView
    {
        private readonly SymTypeList list;

        public SymTypeListDebugView(SymTypeList list)
        {
            this.list = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SymType[] Items => list.ToArray();
    }

    //PERF: don't allocate a massive array of SymType
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(TypTypeListDebugView))]
    public unsafe class SymTypeList : IEnumerable<SymType>
    {
        private readonly byte* ptr;
        private readonly byte* end;

        private int? count;

        public int Count
        {
            get
            {
                if (count == null)
                {
                    var p = ptr;
                    var e = end;

                    var c = 0;

                    while (p < e)
                    {
                        var s = (SYMTYPE*) ptr;

                        c++;
                        p += s->reclen + 2;
                    }

                    count = c;
                }

                return count.Value;
            }
        }

        internal SymTypeList(byte* ptr, int length)
        {
            this.ptr = ptr;
            this.end = ptr + length;
        }

        public IEnumerator<SymType> GetEnumerator() => new Enumerator(ptr, end);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator : IEnumerator<SymType>
        {
            private byte* ptr;
            private readonly byte* end;

            internal Enumerator(byte* ptr, byte* end)
            {
                this.ptr = ptr;
                this.end = end;
                Current = default;
            }

            public bool MoveNext()
            {
                if (ptr < end)
                {
                    Current = (SYMTYPE*) ptr;

#if DEBUG
                    //Force resolve the symbol to its actual type so that we can trigger any asserts for un-implemented properties
                    SymTypeProxy.GetValue(Current);
#endif

                    ptr += Current.reclen + 2;

                    return true;
                }

                Current = default;
                return false;
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
