using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageOptionalHeader
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct IMAGE_OPTIONAL_HEADER32
    {
        public PEMagic Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public int SizeOfCode;
        public int SizeOfInitializedData;
        public int SizeOfUninitializedData;
        public int AddressOfEntryPoint;
        public int BaseOfCode;
        public int BaseOfData;
        public int ImageBase;
        public int SectionAlignment;
        public int FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public int Win32VersionValue;
        public int SizeOfImage;
        public int SizeOfHeaders;
        public uint CheckSum;
        public ImageSubsystem Subsystem;
        public ImageDllCharacteristics DllCharacteristics;
        public int SizeOfStackReserve;
        public int SizeOfStackCommit;
        public int SizeOfHeapReserve;
        public int SizeOfHeapCommit;
        public ImageLoaderFlags LoaderFlags;
        public int NumberOfRvaAndSizes;

        //Array of data directories
        //public IMAGE_DATA_DIRECTORY DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
        public fixed byte DataDirectory[1];
    }
}
