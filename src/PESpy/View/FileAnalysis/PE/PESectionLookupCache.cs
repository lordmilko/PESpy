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
        private bool _isLoaded;

        private PEFile _peFile;

        internal long ImageBase => _peFile.OptionalHeader.ImageBase;

        public PESectionLookupCache(PEFile peFile)
        {
            _peFile = peFile;
            _isLoaded = peFile.IsLoadedImage;

            _sectionIndex = default;
            _lastSectionHeaderVirtualStart = default;
            _lastSectionHeaderVirtualEnd = default;
            _lastSectionHeaderPhysicalStart = default;
            _lastSectionHeaderPhysicalEnd = default;
            _lastSectionIsCode = default;
        }

        public bool TryGetSectionInfo(int rva, out int targetAddress, out int sectionIndex, out bool isCode)
        {
            if (rva < _lastSectionHeaderVirtualStart || rva > _lastSectionHeaderVirtualEnd)
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

                _lastSectionHeaderVirtualStart = section.VirtualAddress;
                _lastSectionHeaderVirtualEnd = _lastSectionHeaderVirtualStart + section.VirtualSize;
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

            if (_isLoaded)
                targetAddress = rva;
            else
            {
                //The size of sections on disk can be less than the size of the section when loaded into memory.
                //RVAs can point to addresses that will only exist in the loaded process. If we're trying to represent
                //an unloaded module, we can't allow these to be shown

                targetAddress = (rva - _lastSectionHeaderVirtualStart) + _lastSectionHeaderPhysicalStart;

                //.textbss sections can have an empty pointer to raw data
                if (targetAddress > _lastSectionHeaderPhysicalEnd || _lastSectionHeaderPhysicalStart == 0)
                {
                    targetAddress = default;
                    sectionIndex = -1;
                    isCode = default;
                    return false;
                }
            }

            sectionIndex = _sectionIndex;
            isCode = _lastSectionIsCode;

            return true;
        }

        public unsafe void GetRawSectionDataFromTargetAddress(int targetAddress, out byte* pByte, out int remainingLength)
        {
            if (_peFile.IsLoadedImage)
                _peFile.GetRawSectionDataFromRVA(targetAddress, out pByte, out remainingLength);
            else
                _peFile.GetRawSectionDataFromOffset(targetAddress, out pByte, out remainingLength);
        }

        public unsafe void GetRawSectionDataFromTargetAddress(int targetAddress, int sectionIndex, out byte* pByte, out int remainingLength)
        {
            if (_peFile.IsLoadedImage)
                _peFile.GetRawSectionDataFromRVA(targetAddress, sectionIndex, out pByte, out remainingLength);
            else
                _peFile.GetRawSectionDataFromOffset(targetAddress, sectionIndex, out pByte, out remainingLength);
        }
    }
}
