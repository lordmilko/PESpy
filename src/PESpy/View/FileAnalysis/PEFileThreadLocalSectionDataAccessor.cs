namespace PESpy.View
{
    internal class PEFileThreadLocalSectionDataAccessor : ISectionDataAccessor
    {
        private PESectionLookupCache _lookupCache;
        private bool _wantVirtual;

        public PEFileThreadLocalSectionDataAccessor(PEFile peFile, bool wantVirtual)
        {
            _lookupCache = new PESectionLookupCache(peFile, wantVirtual);
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
