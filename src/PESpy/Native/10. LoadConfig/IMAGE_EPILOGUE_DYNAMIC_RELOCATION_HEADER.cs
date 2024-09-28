namespace PESpy.Native
{
    //ImageEpilogueDynamicRelocationtable
    internal struct IMAGE_EPILOGUE_DYNAMIC_RELOCATION_HEADER
    {
        public int EpilogueCount;
        public byte EpilogueByteCount;
        public byte BranchDescriptorElementSize;
        public short BranchDescriptorCount;
        // BYTE    BranchDescriptors[...];
        // BYTE    BranchDescriptorBitMap[...];
    }
}