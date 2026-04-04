using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    internal class SymTypeChildListDebugView
    {
        private readonly SymTypeChildList list;

        public SymTypeChildListDebugView(SymTypeChildList list)
        {
            this.list = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SymType[] Items => list.ToArray();
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(SymTypeChildListDebugView))]
    public unsafe struct SymTypeChildList : IEnumerable<SymType>
    {
        private readonly byte* bufferStart;
        private readonly BLOCKSYM* parentStart;
        private readonly byte* parentEnd;
        private readonly ICodeViewAccessor codeViewAccessor;

        private int? count;

        public int Count
        {
            get
            {
                if (count == null)
                {
                    //We store a pointer to the first parent, so we need to skip over that
                    var p = ((byte*) parentStart) + SymType.GetSymbolLength((SYMTYPE*) parentStart, codeViewAccessor);
                    var e = parentEnd;

                    var c = 0;

                    while (p < e)
                    {
                        var s = (SYMTYPE*) p;

                        c++;

                        if (SymType.IsBlockSym(s->rectyp))
                            p = bufferStart + ((BLOCKSYM*) s)->pEnd;

                        p += SymType.GetSymbolLength((SYMTYPE*) p, codeViewAccessor);
                    }

                    count = c;
                }

                return count.Value;
            }
        }

        //Faster than counting all of the elements
        public bool IsEmpty => ((byte*) parentStart) + SymType.GetSymbolLength((SYMTYPE*) parentStart, codeViewAccessor) == parentEnd;

        internal SymTypeChildList(
            BLOCKSYM* parent,
            ICodeViewModuleAccessor? codeViewModuleAccessor)
        {
            parentStart = parent;

            //The first few fields of BLOCKSYM / BLOCKSYM16 / BLOCKSYM32 that describe the parent and
            //end of the block sym are the same in both 16-bit and 32-bit

            long bufferStart;

            if (codeViewModuleAccessor != null)
            {
                var symbols = codeViewModuleAccessor.Symbols;
                this.codeViewAccessor = symbols.codeViewAccessor;
                bufferStart = (long) symbols.start;

                if (codeViewAccessor == null)
                    this.codeViewAccessor = SymbolMemoryTracker.GetAccessor((long) parent);
            }
            else
            {
                var range = SymbolMemoryTracker.FindRange((long) parent);

                if (range == null)
                    bufferStart = 0;
                else
                {
                    bufferStart = range.Value.Start;
                    this.codeViewAccessor = range.Value.CodeViewAccessor;
                }
            }

            if (bufferStart == 0)
                parentEnd = (byte*) parent;
            else
                parentEnd = (byte*) (bufferStart + parent->pEnd);

            this.bufferStart = (byte*) bufferStart;
            count = default;
        }

        //Performs a linear search
        public SymType this[int index]
        {
            get
            {
                var i = -1;

                var enumerator = GetEnumerator();

                while (enumerator.MoveNext())
                {
                    i++;

                    if (i == index)
                        return enumerator.Current;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(bufferStart, (byte*) parentStart, parentEnd, codeViewAccessor);

        IEnumerator<SymType> IEnumerable<SymType>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<SymType>
        {
            private byte* bufferStart;
            private byte* ptr;
            private readonly byte* end;
            private readonly ICodeViewAccessor codeViewAccessor;

            internal Enumerator(byte* bufferStart, byte* ptr, byte* end, ICodeViewAccessor codeViewAccessor)
            {
                this.bufferStart = bufferStart;
                this.ptr = ptr;
                this.end = end;
                this.codeViewAccessor = codeViewAccessor;
                Current = default;
            }

            public bool MoveNext()
            {
                var next = (SYMTYPE*) ptr; //Previous

                next = (SYMTYPE*) (((byte*) next) + SymType.GetSymbolLength(next, codeViewAccessor));

                if (next < end)
                {
                    Current = next;

                    if (SymType.IsBlockSym(next->rectyp))
                    {
                        next = (SYMTYPE*) (bufferStart + ((BLOCKSYM*) next)->pEnd);
                    }

                    ptr = (byte*) next;
                    return true;
                }
                else
                {
                    Current = default;
                    return false;
                }
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
