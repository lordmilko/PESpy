namespace PESpy.Native
{
    public unsafe struct IMAGE_SEPARATE_DEBUG_HEADER
    {
        public ushort Signature;
        public short Flags;
        public short Machine;
        public short Characteristics;
        public uint TimeDateStamp;
        public int CheckSum;
        public int ImageBase;
        public int SizeOfImage;
        public int NumberOfSections;
        public int ExportedNamesSize;
        public int DebugDirectorySize;
        public int SectionAlignment;
        public fixed int Reserved[2];
    }
}
