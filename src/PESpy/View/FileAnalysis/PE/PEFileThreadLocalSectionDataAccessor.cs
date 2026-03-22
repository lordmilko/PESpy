namespace PESpy.View
{
    internal class PEFileThreadLocalSectionDataAccessor : ISectionDataAccessor
    {
        private PESectionLookupCache _lookupCache;

        public PEFileThreadLocalSectionDataAccessor(PEFile peFile)
        {
            _lookupCache = new PESectionLookupCache(peFile);
        }

        public bool TryGetTargetAddress(int rva, out int targetAddress, out int sectionIndex) =>
            _lookupCache.TryGetSectionInfo(rva, out targetAddress, out sectionIndex, out _);

        public bool TryGetOffSeg(int rva, out int off, out ushort seg)
        {
            throw new System.NotImplementedException();
        }

        public unsafe void GetRawSectionData(int targetAddress, out byte* pByte, out int remainingLength) =>
            _lookupCache.GetRawSectionDataFromTargetAddress(targetAddress, out pByte, out remainingLength);

        public unsafe void GetRawSectionData(int targetAddress, int sectionIndex, out byte* pByte, out int remainingLength) =>
            _lookupCache.GetRawSectionDataFromTargetAddress(targetAddress, sectionIndex, out pByte, out remainingLength);
    }
}
