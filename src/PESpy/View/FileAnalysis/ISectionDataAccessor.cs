namespace PESpy.View
{
    public unsafe interface ISectionDataAccessor
    {
        bool TryGetTargetAddress(int rva, out int targetAddress, out int sectionIndex);

        void GetRawSectionData(int targetAddress, out byte* pByte, out int remainingLength);

        void GetRawSectionData(int targetAddress, int sectionIndex, out byte* pByte, out int remainingLength);
    }
}
