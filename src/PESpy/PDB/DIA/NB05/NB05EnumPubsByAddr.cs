using System;

namespace PESpy.PDB.DIA
{
    internal class NB05EnumPubsByAddr
    {
        private OMFHashedSymbols _omfHashedSymbols;
        private IAddrHashInternal32 _addrHash32;
        private ISECT _seg;
        private int _index;

        internal NB05EnumPubsByAddr(OMFHashedSymbols omfHashedSymbols, IAddrHashInternal32 addrHash32)
        {
            _omfHashedSymbols = omfHashedSymbols;
            _addrHash32 = addrHash32;
        }

        public unsafe bool Locate(ISECT seg, int off)
        {
            _addrHash32.BinarySearchAddressMap(off, seg, out var resultSeg, out var resultOffsetIndex);

            _seg = resultSeg;
            var resultOff = _addrHash32[resultSeg, resultOffsetIndex].sectionRelativeOffset;

            var cmp = AddressMapSymTypeList.CompareSectionAndOffset(resultSeg, resultOff, seg, off);

            if (cmp < 0)
                _index = resultOffsetIndex - 2;
            else
                _index = resultOffsetIndex - 1;

            if (_index < 0)
                _index = -1;

            //Rewind to find the first symbol that has the given off/seg
            while (_index != -1)
            {
                var previousOffset = _addrHash32[_seg, resultOffsetIndex - 1].sectionRelativeOffset;

                if (previousOffset != resultOff)
                    break;

                _index--;
                resultOffsetIndex--; //"low" from the binary search
            }

            return true;
        }

        public SymType Current
        {
            get
            {
                var symbolOffset = _addrHash32[_seg, _index].symbolOffset;
                var symType = _omfHashedSymbols.GetSymbolFromOffset(symbolOffset);
                return symType;
            }
        }

        public bool Next()
        {
            retry:
            var offsetsCount = _addrHash32.GetOffsetCount(_seg);

            if (++_index < offsetsCount)
                return true;

            //We've reached the end of the current segment; if there's
            //another segment after us, set the index to the start
            //of that segment
            if (++_seg <= _addrHash32.cSeg)
            {
                _index = -1;
                goto retry;
            }

            return false;
        }

        public bool Previous()
        {
            if (_index > 0)
            {
                _index--;
                return true;
            }

            //We've reached the beginning of the current segment; if
            //there's another segment before us, set the index to the
            //end of that segment
            if (_seg > 1)
            {
                _seg--;
                _index = _addrHash32.GetOffsetCount(_seg) - 1;
                return true;
            }

            return false;
        }
    }
}
