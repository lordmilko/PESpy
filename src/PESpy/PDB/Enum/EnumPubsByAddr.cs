using System;
using System.Diagnostics;

namespace PESpy.PDB
{
    public struct EnumPubsByAddr
    {
        private MsfStream.PSGSI _publics;
        private int _virtualIndex; //The index into the virtual list of entries in the address map (which may include the virtual .Base symbol that denotes the thunk region)
        private int _thunksIndex;
        private AddressMapSymTypeList _addressMap;

        private bool IsTraversingThunks => _thunksIndex != -2;

        internal EnumPubsByAddr(MsfStream.PSGSI publics)
        {
            _publics = publics;
            _virtualIndex = -1;
            _thunksIndex = -2;
            _addressMap = publics.AddressMapSymbols;
        }

        //In order to facilitate matching against thunk records when binary searching,
        //PDB1 injects a dummy "base thunk region" (".Base") symbol into its address map.
        //We don't exactly do that; instead, in the event we have thunks we duplicate
        //our address list, and insert a dummy offset in it at the appropriate position.
        //Any time somebody asks for the symbol at that offset, we see that they're asking
        //for the special base thunk region symbol and return that instead

        public unsafe bool Locate(ISECT seg, int off)
        {
            var addressMap = _addressMap;

            //All special handling of the base thunk region symbol occurs inside the binary search
            _addressMap.BinarySearchAddressMap(off, seg, out var item, out var low, out _);

            var cmp = AddressMapSymTypeList.CompareSectionAndOffset(item, off, seg);

            if (cmp < 0)
                _virtualIndex = low - 2;
            else
                _virtualIndex = low - 1;

            if (_virtualIndex < 0)
                _virtualIndex = -1;

            //Rewind to find the first symbol that has the given off/seg. In the latest version of mspdbcore this is a binary search;
            //I feel like linear would be more efficient however (it seems uncommon you would have that many duplicate items at the given
            //address, and even when you do, surely not that many!)
            while (_virtualIndex != -1)
            {
                if (_addressMap.IsBaseThunkIndex(low - 1))
                    break;

                if (AddressMapSymTypeList.CompareSectionAndOffset(_addressMap.GetVirtualSymbol(low - 1), item) != 0)
                    break;

                _virtualIndex--;
                low--;
            }

            if (_publics.IsAddressInThunkTable(seg, off))
            {
                if (!_publics.TryGetThunkSymbol(off, seg, out _, out _))
                    return false;

                _thunksIndex = ((off - _publics.PSGsiHdr.offThunkTable) / _publics.PSGsiHdr.cbSizeOfThunk) - 1;
            }

            return true;
        }

        public SymType Current
        {
            get
            {
                if (_thunksIndex != -2)
                {
                    var seg = _publics.PSGsiHdr.isectThunkTable;
                    var off = _publics.PSGsiHdr.offThunkTable + (_thunksIndex * _publics.PSGsiHdr.cbSizeOfThunk);

                    var result = _publics.TryGetThunkSymbol(off, seg, out var symType, out _);
                    Debug.Assert(result); //We should already know that this is going to succeed from when we called Next in the first place
                    return symType;
                }

                return _addressMap.GetVirtualSymbol(_virtualIndex);
            }
        }

        public bool Next()
        {
            if (IsTraversingThunks)
            {
                if (++_thunksIndex < _publics.PSGsiHdr.nThunks) //NextThunk
                    return true;
                else
                    _thunksIndex = -2; //ResetThunk
            }

            if (++_virtualIndex < _addressMap.VirtualCount)
            {
                if (_addressMap.IsBaseThunkIndex(_virtualIndex))
                    _thunksIndex = 0;

                //We don't need to worry about reading the symbol from disk
                return true;
            }

            return false;
        }

        public bool Previous()
        {
            if (IsTraversingThunks)
            {
                if (--_thunksIndex > -1)
                    return true;
                else
                    _thunksIndex = -2; //ResetThunk
            }

            //microsoft-pdb rewinds unless we're at -1,
            //but this seems wrong to me, and their logic in readSymbol()
            //does not convince me that what they're doing is right either, so I've changed the check
            //to "> 0"
            if (_virtualIndex > 0)
            {
                _virtualIndex--;
                return true;

            }

            return false;
        }
    }
}
