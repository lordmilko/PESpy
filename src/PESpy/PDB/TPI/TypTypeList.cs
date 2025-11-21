using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    internal class TypTypeListDebugView
    {
        private readonly TypTypeList list;

        public TypTypeListDebugView(TypTypeList list)
        {
            this.list = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TypType[] Items => list.ToArray();
    }

    //PERF: don't allocate a massive array of TypType
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(TypTypeListDebugView))]
    public unsafe class TypTypeList : IEnumerable<TypType>
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
                        var t = (TYPTYPE*) p;

                        c++;
                        p += t->len + 2;
                    }

                    count = c;
                }

                return count.Value;
            }
        }

        internal TypTypeList(byte* ptr, int length)
        {
            this.ptr = ptr;
            this.end = ptr + length;
        }

        public void CopyTo(Span<byte> destination)
        {
            new Span<byte>(ptr, (int) (end - ptr)).CopyTo(destination);
        }

        public TypType GetTypeFromOffset(int offset)
        {
            var p = ptr + offset;

            if (p > end)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return (TYPTYPE*) p;
        }

        //Performs a linear search
        public TypType this[int index]
        {
            get
            {
                var p = ptr;
                var e = end;

                var i = 0;

                while (p < e)
                {
                    var t = (TYPTYPE*) p;

                    if (i == index)
                        return t;

                    i++;
                    p += t->len + 2;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(ptr, end);

        //Enumerate starting from the specified offset
        public Enumerator GetEnumerator(int offset) => new Enumerator(ptr + offset, end);

        IEnumerator<TypType> IEnumerable<TypType>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<TypType>
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
                    Current = (TYPTYPE*) ptr;

#if DEBUG
                    //Force resolve the symbol to its actual type so that we can trigger any asserts for un-implemented properties
                    //TypTypeProxy.GetValue(Current);
#endif

                    ptr += Current.len + 2;

                    return true;
                }

                Current = default;
                return false;
            }

            public TypType Current { get; private set; }

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
