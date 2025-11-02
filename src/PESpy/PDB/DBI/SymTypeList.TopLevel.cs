using System.Collections;
using System.Collections.Generic;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    public partial class SymTypeList
    {
        public readonly unsafe struct TopLevel : IEnumerable<SymType>
        {
            private readonly byte* start; //Start may be less than ptr when there's a CV_SIGNATURE value at the front. BlockSym ends are relative to the literal start, before the CV_SIGNATURE begins
            private readonly byte* ptr;
            private readonly byte* end;
            private readonly ICodeViewAccessor? codeViewAccessor;

            public ICodeViewAccessor GetCodeViewAccessor() => codeViewAccessor;

            internal TopLevel(byte* start, byte* ptr, byte* end, ICodeViewAccessor? codeViewAccessor)
            {
                this.start = start;
                this.ptr = ptr;
                this.end = end;
                this.codeViewAccessor = codeViewAccessor;
            }

            public Enumerator GetEnumerator() => new Enumerator(start, ptr, end, codeViewAccessor);

            IEnumerator<SymType> IEnumerable<SymType>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<SymType>
            {
                private readonly byte* start;
                private byte* ptr;
                private readonly byte* end;
                private readonly ICodeViewAccessor? codeViewAccessor;

                internal Enumerator(byte* start, byte* ptr, byte* end, ICodeViewAccessor? codeViewAccessor)
                {
                    this.start = start;
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

                        if (SymType.IsBlockSym(Current.rectyp))
                        {
                            //If this is a block symbol, we don't want to traverse within it.
                            //Skip over all of its descendants, as well as the S_END symbol
                            //after it
                            var block = (BLOCKSYM*) ptr;

                            ptr = start + block->pEnd; //If there's no children, myOff will be the same as pEnd
                            ptr += SymType.GetSymbolLength((SYMTYPE*) ptr, codeViewAccessor);
                        }
                        else
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
}
