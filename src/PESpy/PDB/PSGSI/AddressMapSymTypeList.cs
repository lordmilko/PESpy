using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private readonly NativeSpan<int> addressMap;
        private readonly byte* symbolsStart;

        public int Count => addressMap.Length;

        public AddressMapSymTypeList(NativeSpan<int> addressMap, byte* symbolsStart)
        {
            this.addressMap = addressMap;
            this.symbolsStart = symbolsStart;
        }

        public SymType this[int index] => (SYMTYPE*) (symbolsStart + addressMap[index]);

        //Internal: caller should be asking PSGSI about the nearest symbol so it can check
        //if we have an address map, and do checks against thunks
        internal bool GetNearestSymbol(int relativeOffset, int sectionNumber, out SymType symType, out int displacement)
        {
            //There's two ways of looking up addresses using PSGSI: PSGSI1::NearestSym and EnumPubsByAddr::locate
            //which has slightly different logic

            var low = 0;
            var high = addressMap.Length - 1;

            int result;
            SymType item;

            while (low < high)
            {
                //This is a right biased mid binary search
                var mid = low + ((high - low + 1) / 2);

                item = (SYMTYPE*) (symbolsStart + addressMap[mid]);

                result = CompareSectionAndOffset(item, relativeOffset, sectionNumber);

                if (result < 0)
                    high = mid - 1;
                else if (result > 0)
                    low = mid;
                else
                {
                    low = mid;
                    high = mid;
                }
            }

            item = (SYMTYPE*) (symbolsStart + addressMap[low]);

            //EnumPubsByAddr::locate then does some funny business with m_iPubs and negative numbers, but we're following
            //NearestSym so we don't need to worry about that

            item.TryGetOffSeg(out var itemOff, out var itemSeg);

            if (itemSeg == sectionNumber)
            {
                //Identical Code Folding (ICF) can apparently cause multiple symbols to exist with a given offset.
                //(this seems wrong to me, my experience is COMDAT folding deletes symbols entirely). In any case,
                //we want to take the first symbol that has a given address, so rewind symbols until we find the first one
                //that could be considered valid

                var currentItemIndex = low;

                var currentSymbol = item;

                while (currentItemIndex > 0)
                {
                    var previousItemIndex = currentItemIndex - 1;

                    var previousSymbol = (SYMTYPE*) (symbolsStart + addressMap[previousItemIndex]);

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
                //we need to advance to the first symbol in the next section

                while (true)
                {
                    low++;

                    if (low >= addressMap.Length)
                    {
                        symType = default;
                        displacement = default;
                        return false;
                    }

                    item = (SYMTYPE*) (symbolsStart + addressMap[low]);

                    item.TryGetOffSeg(out _, out itemSeg);

                    if (itemSeg == sectionNumber)
                        break;

                    if (itemSeg > sectionNumber)
                    {
                        symType = default;
                        displacement = default;
                        return false;
                    }
                }

                //Don't understand what "boundary conditions" are.
                //an example rva that seems to hit this code path is 0x00012308 in native.x64
                //throw new System.NotImplementedException();
            }

            symType = (SYMTYPE*) (symbolsStart + addressMap[low]);

            symType.TryGetOffSeg(out var resultOff, out _);
            displacement = relativeOffset - resultOff; //The symbol we match against will always be <= our requested symbol
            return true;
        }

        private static int CompareSectionAndOffset(SymType symType, int relativeOffset, int sectionNumber)
        {
            var result = symType.TryGetOffSeg(out var pubOff, out var pubSeg);
            Debug.Assert(result, "Expected the symbol to be a public with an offset and segment");

            if (pubSeg == sectionNumber)
                return relativeOffset - pubOff;

            return sectionNumber - pubSeg;
        }

        private static int CompareSectionAndOffset(SymType first, SymType second)
        {
            var result = first.TryGetOffSeg(out var off, out var seg);
            Debug.Assert(result);

            return CompareSectionAndOffset(second, off, seg);
        }

        public Enumerator GetEnumerator() => new Enumerator(addressMap, symbolsStart);

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
