namespace PESpy.View
{
    public unsafe interface ISectionDataAccessor
    {
        bool TryGetTargetAddress(int rva, out int targetAddress, out int sectionIndex);

        bool TryGetOffSeg(int rva, out int off, out ushort seg);

        void GetRawSectionData(int targetAddress, out byte* pByte, out int remainingLength);

        void GetRawSectionData(int targetAddress, int sectionIndex, out byte* pByte, out int remainingLength);
    }
}
