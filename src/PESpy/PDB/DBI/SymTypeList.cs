using System;
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
    [DebuggerTypeProxy(typeof(SymTypeListDebugView))]
    public unsafe partial class SymTypeList : IEnumerable<SymType>
    {
        private readonly byte* start; //Start may be less than ptr when there's a CV_SIGNATURE value at the front. BlockSym ends are relative to the literal start, before the CV_SIGNATURE begins
        private readonly byte* ptr;
        private readonly byte* end;
        internal readonly ICodeViewAccessor? codeViewAccessor;

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
                        var s = (SYMTYPE*) p;

                        c++;
                        p += SymType.GetSymbolLength(s, codeViewAccessor);
                    }

                    count = c;
                }

                return count.Value;
            }
        }

        internal SymTypeList(byte* start, int dataOffset, int length, ICodeViewAccessor? codeViewAccessor)
        {
            this.start = start;
            this.ptr = start + dataOffset;
            this.end = ptr + length;
            this.codeViewAccessor = codeViewAccessor;
        }

        public TopLevel GetTopLevel() => new TopLevel(start, ptr, end, codeViewAccessor);

        //Copies all symbols (does not include any CV_SIGNATURE) to the destination buffer
        public void CopyTo(Span<byte> destination)
        {
            new Span<byte>(ptr, (int) (end - ptr)).CopyTo(destination);
        }

        //Performs a linear search
        public SymType this[int index]
        {
            get
            {
                var i = 0;

                var p = ptr;
                var e = end;

                while (p < e)
                {
                    var s = (SYMTYPE*) p;

                    if (i == index)
                        return s;

                    i++;
                    p += SymType.GetSymbolLength(s, codeViewAccessor);
                }

                throw new IndexOutOfRangeException();
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(ptr, end, codeViewAccessor);

        IEnumerator<SymType> IEnumerable<SymType>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<SymType>
        {
            private byte* ptr;
            private readonly byte* end;
            private readonly ICodeViewAccessor codeViewAccessor;

            internal Enumerator(byte* ptr, byte* end, ICodeViewAccessor codeViewAccessor)
            {
                this.ptr = ptr;
                this.end = end;
                Current = default;
                this.codeViewAccessor = codeViewAccessor;
            }

            public bool MoveNext()
            {
                if (ptr < end)
                {
                    Current = (SYMTYPE*) ptr;

#if DEBUG
                    //Force resolve the symbol to its actual type so that we can trigger any asserts for un-implemented properties
                    //SymTypeProxy.GetValue(Current);
#endif

                    ptr += SymType.GetSymbolLength(Current, codeViewAccessor);

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
