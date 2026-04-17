using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    internal class AddressMapSymTypeListDebugView
    {
        private readonly AddressMapSymTypeList list;

        public AddressMapSymTypeListDebugView(AddressMapSymTypeList list)
        {
            this.list = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SymType[] Items => list.ToArray();
    }

    /// <summary>
    /// Gets the address map, which allows resolving addresses to the symbols that are closest to them.<para/>
    /// The entry point for resolving the nearest symbol to a given address is <see cref="MsfStream.PSGSI.TryGetNearestSymbol(int, int, out SymType, out int)"/>
    /// which takes into consideration whether the specified address may be in the thunk table.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(AddressMapSymTypeListDebugView))]
    public unsafe class AddressMapSymTypeList : IEnumerable<SymType> //PERF: don't allocate a massive array of SymType
    {
        //The address map as it resides on disk
        private readonly NativeSpan<int> _addressMap;

        //The address map as it resides in memory; if we have any thunks, this will include a virtual ".Base" symbol that encapsualtes the region in which thunk symbols reside. This enables binary searches
        //against the address map to detect when the closest symbol to a given off/seg is actually a thunk
        private readonly NativeSpan<int> _virtualAddressMap;
        private readonly byte* _symbolsStart;

        private readonly int _baseThunkIndex = -1;
        private readonly SymType _baseThunkSym;

        public int Count => _addressMap.Length;

        internal int VirtualCount => _virtualAddressMap.Length;

        internal AddressMapSymTypeList(
            NativeSpan<int> addressMap,
            byte* symbolsStart,
            in PSGSIHDR psGsiHdr,
            out IntPtr virtualAddressMap,
            out IntPtr baseThunkSym)
        {
            _addressMap = addressMap;
            _symbolsStart = symbolsStart;
            baseThunkSym = default;

            if (psGsiHdr.nThunks == 0)
            {
                virtualAddressMap = default;
                _virtualAddressMap = addressMap;
            }
            else
            {
                //Find the insertion point, then create a new list of address map offsets.
                //PDB1 seems to store the pointer position of a PSYM within the bounds of the sym recs
                //which is then lazily read on demand; we don't do that, we just store an offset into the symrecs,
                //and we'll say if we see the index of our .Base symbol being used, we'll return the base symbol
                //instead of something in sym recs. That way we don't need to worry about modifying the symrecs buffer
                virtualAddressMap = Marshal.AllocHGlobal((addressMap.Length * sizeof(int)) + sizeof(int));

                //The insertion point should be close to the front
                _virtualAddressMap = new NativeSpan<int>((void*) virtualAddressMap, addressMap.Length + 1);

                var source = addressMap.AsSpan();
                var dest = _virtualAddressMap.AsSpan();

                for (var i = 0; i < addressMap.Length; i++)
                {
                    SymType symType = (SYMTYPE*) (symbolsStart + source[i]);

                    if (CompareSectionAndOffset(symType, psGsiHdr.offThunkTable, psGsiHdr.isectThunkTable) < 0)
                    {
                        //Copy everything up to here
                        source.Slice(0, i).CopyTo(dest.Slice(0, i));

                        //Insert ourselves. We don't need a real value because when we see this index is being used,
                        //we need to special case it. That way we don't need a duplicate symrecs
                        dest[i] = -1;
                        _baseThunkIndex = i;

                        if (i < addressMap.Length - 1)
                        {
                            //Copy everything after here
                            source.Slice(i).CopyTo(dest.Slice(i + 1));
                        }

                        baseThunkSym = CreateBaseThunkSymbol(symType, psGsiHdr);
                        _baseThunkSym = (SYMTYPE*) baseThunkSym;

                        break;
                    }
                }
            }
        }

        internal bool IsBaseThunkIndex(int index) => index == _baseThunkIndex;

        private unsafe IntPtr CreateBaseThunkSymbol(SymType symType, in PSGSIHDR psGsiHdr)
        {
            bool utf8;
            int baseSymLength;
            int totalLength;
            IntPtr baseSym;
            const string baseName = ".Base";

            //Just copy whatever type the current symbol is
            switch (symType.rectyp)
            {
                case SYM_ENUM_e.S_PUB16:
                    utf8 = false;
                    baseSymLength = DataSym16.FixedStructSize;
                    totalLength = baseSymLength + baseName.Length + 1;
                    baseSym = Marshal.AllocHGlobal(totalLength);
                    var pubSym16 = (DATASYM16*) baseSym;
                    pubSym16->rectyp = SYM_ENUM_e.S_PUB16;
                    pubSym16->reclen = (ushort) (totalLength - sizeof(short));
                    pubSym16->typind = 0;
                    pubSym16->seg = psGsiHdr.isectThunkTable;
                    pubSym16->off = psGsiHdr.offThunkTable;
                    break;

                case SYM_ENUM_e.S_PUB32_16t:
                    utf8 = false;
                    baseSymLength = DataSym3216t.FixedStructSize;
                    totalLength = baseSymLength + baseName.Length + 1;
                    baseSym = Marshal.AllocHGlobal(totalLength);
                    var pubSym3216t = (DATASYM32_16t*) baseSym;
                    pubSym3216t->rectyp = SYM_ENUM_e.S_PUB32_16t;
                    pubSym3216t->reclen = (ushort) (totalLength - sizeof(short));
                    pubSym3216t->typind = 0;
                    pubSym3216t->seg = psGsiHdr.isectThunkTable;
                    pubSym3216t->off = psGsiHdr.offThunkTable;
                    break;

                case SYM_ENUM_e.S_PUB32_ST:
                    utf8 = false;
                    baseSymLength = PubSym32.FixedStructSize;
                    totalLength = baseSymLength + baseName.Length + 1;
                    baseSym = Marshal.AllocHGlobal(totalLength);
                    var pubSym32ST = (PUBSYM32*) baseSym;
                    pubSym32ST->rectyp = SYM_ENUM_e.S_PUB32_ST;
                    pubSym32ST->reclen = (ushort) (totalLength - sizeof(short));
                    pubSym32ST->pubsymflags = 0;
                    pubSym32ST->pubsymflags.fCode = true;
                    pubSym32ST->seg = psGsiHdr.isectThunkTable;
                    pubSym32ST->off = psGsiHdr.offThunkTable;
                    break;

                case SYM_ENUM_e.S_PUB32:
                    utf8 = true;
                    baseSymLength = PubSym32.FixedStructSize;
                    totalLength = baseSymLength + baseName.Length + 1;
                    baseSym = Marshal.AllocHGlobal(totalLength);
                    var pubSym32 = (PUBSYM32*) baseSym;
                    pubSym32->rectyp = SYM_ENUM_e.S_PUB32;
                    pubSym32->reclen = (ushort) (totalLength - sizeof(short));
                    pubSym32->pubsymflags = 0;
                    pubSym32->pubsymflags.fCode = true;
                    pubSym32->seg = psGsiHdr.isectThunkTable;
                    pubSym32->off = psGsiHdr.offThunkTable;
                    break;

                default:
                    throw new NotImplementedException();
            }

            var nameSpan = new Span<byte>((byte*) baseSym + baseSymLength, baseName.Length + 1);

            if (utf8)
            {
                nameSpan[0] = (byte) '.';
                nameSpan[1] = (byte) 'B';
                nameSpan[2] = (byte) 'a';
                nameSpan[3] = (byte) 's';
                nameSpan[4] = (byte) 'e';
                nameSpan[5] = (byte) '\0';
            }
            else
            {
                nameSpan[0] = (byte) baseName.Length;
                nameSpan[1] = (byte) '.';
                nameSpan[2] = (byte) 'B';
                nameSpan[3] = (byte) 'a';
                nameSpan[4] = (byte) 's';
                nameSpan[5] = (byte) 'e';
            }

            return baseSym;
        }

        public SymType this[int index] => (SYMTYPE*) (_symbolsStart + _addressMap[index]);

        internal SymType GetVirtualSymbol(int index) => index == _baseThunkIndex ? _baseThunkSym : (SymType) (SYMTYPE*) (_symbolsStart + _virtualAddressMap[index]);

        //Internal: caller should be asking PSGSI about the nearest symbol so it can check
        //if we have an address map, and do checks against thunks
        //Note that it's possible to leak our virtual ".Base" thunk region by asking for an address past the end
        //of the thunk region. PDB1 has this issue as well
        internal bool GetNearestSymbol(int relativeOffset, int sectionNumber, out SymType symType, out int displacement)
        {
            //There's two ways of looking up addresses using PSGSI: PSGSI1::NearestSym and EnumPubsByAddr::locate
            //which has slightly different logic

            BinarySearchAddressMap(relativeOffset, sectionNumber, out var item, out var low, out var isThunkBase);

            //EnumPubsByAddr::locate then does some funny business with m_iPubs and negative numbers, but we're following
            //NearestSym so we don't need to worry about that

            item.TryGetRawOffSeg(out var itemOff, out var itemSeg);

            if (itemSeg == sectionNumber)
            {
                //Identical Code Folding (ICF) can apparently cause multiple symbols to exist with a given offset.
                //(this seems wrong to me, my experience is COMDAT folding deletes symbols entirely). In any case,
                //we want to take the first symbol that has a given address, so rewind symbols until we find the first one
                //that could be considered valid

                var currentItemIndex = low;

                var currentSymbol = item;

                var baseThunkIndex = _baseThunkIndex;
                var virtualAddressMap = _virtualAddressMap;

                while (currentItemIndex > 0) //Note: _don't_ need to check if it's the base thunk symbol here
                {
                    var previousItemIndex = currentItemIndex - 1;

                    var previousSymbol = GetVirtualSymbol(previousItemIndex);

                    /* An example of what causes this to occur from a debug build:
                     *
                     * we have the following symbols (from first to last)
                     *   __acrt_initialize
                     *   __scrt_stub_for_acrt_initialize
                     *   __vcrt_initialize
                     *
                     * Our binary search will have matched __vcrt_initialize. Apparently, we're supposed to walk backwards as long as there's a match. However,
                     * this seems wrong, because WinDbg shows this function as __scrt_stub_for_acrt_initialize, when you would expect it would actually show
                     * __acrt_initialize. IDA Pro also shows __scrt_stub_for_acrt_initialize
                     */

                    //Any symbols where ICF is present should be all in a row, so if the previous symbol has a different offset,
                    //we're done
                    if (CompareSectionAndOffset(currentSymbol, previousSymbol) != 0)
                        break;

                    currentSymbol = previousSymbol;
                    currentItemIndex = previousItemIndex;
                }

                low = currentItemIndex;
            }
            else
            {
                //If the symbol we matched against was the last symbol in the given section before the section we're actually after,
                //we need to advance to the first symbol in the next section. Note that we don't accept the .Base symbol as a match,
                //so if we've hit that we need to skip over it

                while (itemSeg < sectionNumber || IsBaseThunkIndex(low))
                {
                    low++;

                    if (low >= _virtualAddressMap.Length)
                    {
                        symType = default;
                        displacement = default;
                        return false;
                    }

                    item = GetVirtualSymbol(low);

                    item.TryGetRawOffSeg(out _, out itemSeg);

                    if (itemSeg > sectionNumber)
                    {
                        symType = default;
                        displacement = default;
                        return false;
                    }
                }
            }

            symType = GetVirtualSymbol(low);

            //The above logic does not allow landing in a section other than the one we're after, so we don't need to worry about
            //the section being different in calculating the displacement

            symType.TryGetRawOffSeg(out var resultOff, out var resultSeg);

            //PSGSI::NearestSym doesn't seem to do -1 stuff; when we wanted off/seg 0/0 and got 0/1 the displacement
            //was still 0. And when we had 1/0 and got 1/1 the displacement was 1, so it seems like the section is ignored
            displacement = relativeOffset - resultOff; //The symbol we match against will always be <= our requested symbol
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void BinarySearchAddressMap(
            int relativeOffset,
            ISECT sectionNumber,
            out SymType item,
            out int virtualLow,
            out bool isThunkBase)
        {
            var virtualAddressMap = _virtualAddressMap;

            virtualLow = 0;
            var high = virtualAddressMap.Length - 1;

            //Index of the ".Base" virtual symbol that represents the thunk area
            var baseThunkIndex = _baseThunkIndex;

            int result;

            while (virtualLow < high)
            {
                //This is a right biased mid binary search
                var mid = virtualLow + ((high - virtualLow + 1) / 2);

                item = mid == baseThunkIndex ? _baseThunkSym : (SYMTYPE*) (_symbolsStart + virtualAddressMap[mid]);

                result = CompareSectionAndOffset(item, relativeOffset, sectionNumber);

                if (result < 0)
                    high = mid - 1;
                else if (result > 0)
                    virtualLow = mid;
                else
                {
                    virtualLow = mid;
                    high = mid;
                }
            }

            if (virtualLow == baseThunkIndex)
            {
                item = _baseThunkSym;
                isThunkBase = true;
            }
            else
            {
                item = (SYMTYPE*) (_symbolsStart + virtualAddressMap[virtualLow]);
                isThunkBase = false;
            }
        }

        internal static int CompareSectionAndOffset(SymType symType, int relativeOffset, int sectionNumber)
        {
            var result = symType.TryGetRawOffSeg(out var pubOff, out var pubSeg);
            Debug.Assert(result, "Expected the symbol to be a public with an offset and segment");

            if (pubSeg == sectionNumber)
                return relativeOffset - pubOff;

            return sectionNumber - pubSeg;
        }

        internal static int CompareSectionAndOffset(
            ISECT seg1,
            int off1,
            ISECT seg2,
            int off2)
        {
            if (seg1 == seg2)
                return off2 - off1;

            return off2 - off1;
        }

        internal static int CompareSectionAndOffset(SymType first, SymType second)
        {
            var result = first.TryGetRawOffSeg(out var off, out var seg);
            Debug.Assert(result);

            return CompareSectionAndOffset(second, off, seg);
        }

        public Enumerator GetEnumerator() => new Enumerator(_addressMap, _symbolsStart);

        IEnumerator<SymType> IEnumerable<SymType>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<SymType>
        {
            private readonly NativeSpan<int> addressMap;
            private readonly byte* symbolsStart;
            private int index;

            internal Enumerator(NativeSpan<int> addressMap, byte* symbolsStart)
            {
                this.addressMap = addressMap;
                this.symbolsStart = symbolsStart;
                index = 0;
                Current = default;
            }

            public bool MoveNext()
            {
                if (index >= addressMap.Length)
                {
                    Current = default;
                    return false;
                }

                Current = (SYMTYPE*) (symbolsStart + addressMap[index]);
                index++;
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
