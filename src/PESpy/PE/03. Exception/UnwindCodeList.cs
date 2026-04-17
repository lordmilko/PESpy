using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

namespace PESpy
{
    internal class UnwindCodeListDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public UnwindCode[] Items => list.ToArray();

        private UnwindCodeList list;

        //There's some sort of issue with the Visual Studio debugger sometimes. If i do ToArray
        //in the ctor here, the debugger doesn't show anything. But if I defer it to the property then we at least
        //get a debugger error
        internal UnwindCodeListDebugView(UnwindCodeList list)
        {
            this.list = list;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(UnwindCodeListDebugView))]
    public unsafe struct UnwindCodeList : IEnumerable<UnwindCode>, ILightweightList<UnwindCodeList.Enumerator, UnwindCode>
    {
        //todo: make unwindcodes a struct that store a pointer and have "derived structs" that include additional members
        //like we do in our symtype. have a debuggerdisplay that dispatches based on the uwop kind, and
        //have a Count on this list type that calculates the actual count with a comment it may be different from
        //countofcodes and also may include the trailing "null" unwind code which is just used for alignment.

        //then we need an xmldoc on both exceptiondata and exceptionhandlerkind to say these will only use symbols
        //if theyre locally available, you need to call getsymbolaccessor with the desired httpmode beforehand
        //to force locate the symbol file if you want to make sure you have it

        public int Count
        {
            get
            {
                if (_count == 0)
                {
                    var i = 0;

                    var enumerator = GetEnumerator();

                    while (enumerator.MoveNext())
                        i++;

                    _count = i;
                }

                return _count;
            }
        }

        private int _count; //If 0, the count hasn't been computed yet
        private readonly byte* _pUnwindInfo;
        private readonly bool _includeAlignment;

        internal UnwindCodeList(byte* pUnwindInfo, bool includeAlignment)
        {
            _pUnwindInfo = pUnwindInfo;
            _includeAlignment = includeAlignment;
        }

        public UnwindCode this[int index]
        {
            get
            {
                var i = 0;

                var enumerator = GetEnumerator();

                while (enumerator.MoveNext())
                {
                    if (i == index)
                        return enumerator.Current;

                    i++;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(_pUnwindInfo, _includeAlignment);

        IEnumerator<UnwindCode> IEnumerable<UnwindCode>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<UnwindCode>
        {
            private readonly byte* _pEnd;
            private readonly byte* _pAlignedEnd;
            private readonly byte* _pUnwindInfo;
            private readonly bool _includeAlignment;
            private byte* _pCurrent;
            private UnwindCode _current;

            internal Enumerator(byte* pUnwindInfo, bool includeAlignment)
            {
                //CountOfCodes represents a count of "slots" that follow. A "slot" is a 16 bit value
                //that either contains an UNWIND_CODE, or some additional data relating to the previous
                //UNWIND_CODE. Thus, we don't know how many top level "codes" we'll actually have
                var countOfCodes = *(pUnwindInfo + UnwindInfo.CountOfCodesOffset);

                _pCurrent = pUnwindInfo + UnwindInfo.UnwindCodeOffset;
                _pUnwindInfo = pUnwindInfo;
                _pEnd = _pCurrent + (countOfCodes * sizeof(short));
                _includeAlignment = includeAlignment;

                var alignedCount = (countOfCodes + 1) & ~1;

                //For alignment purposes, this array always has an even number of entries, and the final entry is
                //potentially unused. In that case, the array is one longer than indicated by the count of unwind
                //codes field
                _pAlignedEnd = countOfCodes == alignedCount
                    ? _pEnd
                    : _pEnd + sizeof(short);
            }

            public UnwindCode Current => _current;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                //https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64?view=msvc-170#struct-unwind_code
                if (_pCurrent < _pEnd)
                {
                    var current = new UnwindCode(_pCurrent, _pUnwindInfo);
                    _current = current;

                    _pCurrent += current.StructSize;

                    return true;
                }

                if (_pCurrent < _pAlignedEnd && _includeAlignment)
                {
                    _current = new UnwindCode(_pCurrent, default);
                    _pCurrent += sizeof(short);
                    return true;
                }

                return false;
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
