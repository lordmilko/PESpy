using ClrDebug;

namespace PESpy.View
{
    //Provides fast resolution of RVAs to "target" addresses (based on whether we're representing
    //physical offsets or RVAs in the analysis)
    struct PESectionLookupCache
    {
        private int _sectionIndex;
        private int _lastSectionHeaderVirtualStart;
        private int _lastSectionHeaderVirtualEnd;
        private int _lastSectionHeaderPhysicalStart;
        private int _lastSectionHeaderPhysicalEnd;
        private bool _lastSectionIsCode;

        private PEFile _peFile;
        internal bool _wantVirtual;

        internal long ImageBase => _peFile.OptionalHeader.ImageBase;

        public PESectionLookupCache(PEFile peFile, bool wantVirtual)
        {
            _peFile = peFile;
            _wantVirtual = wantVirtual;

            _sectionIndex = default;
            _lastSectionHeaderVirtualStart = default;
            _lastSectionHeaderVirtualEnd = default;
            _lastSectionHeaderPhysicalStart = default;
            _lastSectionHeaderPhysicalEnd = default;
            _lastSectionIsCode = default;
        }

        public bool TryGetSectionInfo(int rva, out int targetAddress, out int sectionIndex, out bool isCode)
        {
            if (rva < _lastSectionHeaderVirtualStart || rva >= _lastSectionHeaderVirtualEnd)
            {
                _sectionIndex = _peFile.GetSectionContainingRVA(rva);

                if (_sectionIndex == -1)
                {
                    _lastSectionHeaderVirtualStart = 0;
                    _lastSectionHeaderVirtualEnd = 0;
                    _lastSectionHeaderPhysicalStart = 0;
                    _lastSectionHeaderPhysicalEnd = 0;
                    _lastSectionIsCode = false;

                    targetAddress = default;
                    sectionIndex = -1;
                    isCode = default;
                    return false;
                }

                ref var section = ref _peFile.SectionHeaders[_sectionIndex];
                ref var sectionRange = ref _peFile.SectionRanges[_sectionIndex];

                _lastSectionHeaderVirtualStart = sectionRange.Start;
                _lastSectionHeaderVirtualEnd = sectionRange.End;
                _lastSectionHeaderPhysicalStart = section.PointerToRawData;
                _lastSectionHeaderPhysicalEnd = _lastSectionHeaderPhysicalStart + section.SizeOfRawData;
                _lastSectionIsCode = (section.Characteristics & IMAGE_SCN.CNT_CODE) != 0;

                if (rva < _lastSectionHeaderVirtualStart || rva > _lastSectionHeaderVirtualEnd)
                {
                    targetAddress = default;
                    sectionIndex = -1;
                    isCode = default;
                    return false;
                }
            }

            //If we're actually virtual, the address can be used as is. But if we're just faking it, we need to check
            //whether the given address is within the bounds of the physical area, which we do in the else path below
            if (_peFile.IsLoadedImage)
                targetAddress = rva;
            else
            {
                //The size of sections on disk can be less than the size of the section when loaded into memory.
                //RVAs can point to addresses that will only exist in the loaded process. If we're trying to represent
                //an unloaded module, we can't allow these to be shown

                //Even if we want virtual, we still need to check whether the address is in bounds
                targetAddress = (rva - _lastSectionHeaderVirtualStart) + _lastSectionHeaderPhysicalStart;

                //.textbss sections can have an empty pointer to raw data
                if (targetAddress >= _lastSectionHeaderPhysicalEnd || _lastSectionHeaderPhysicalStart == 0)
                {
                    targetAddress = default;
                    sectionIndex = -1;
                    isCode = default;
                    return false;
                }

                if (_wantVirtual)
                    targetAddress = rva;
            }

            sectionIndex = _sectionIndex;
            isCode = _lastSectionIsCode;

            return true;
        }

        public unsafe void GetRawSectionDataFromTargetAddress(int targetAddress, out byte* pByte, out int remainingLength)
        {
            if (_wantVirtual)
                _peFile.GetRawSectionDataFromRVA(targetAddress, out pByte, out remainingLength);
            else
                _peFile.GetRawSectionDataFromOffset(targetAddress, out pByte, out remainingLength);
        }

        public unsafe void GetRawSectionDataFromTargetAddress(int targetAddress, int sectionIndex, out byte* pByte, out int remainingLength)
        {
            if (_wantVirtual)
                _peFile.GetRawSectionDataFromRVA(targetAddress, sectionIndex, out pByte, out remainingLength);
            else
                _peFile.GetRawSectionDataFromPhysicalOffset(targetAddress, sectionIndex, out pByte, out remainingLength);
        }
    }
}
