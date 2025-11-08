using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageDebugDirectory
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_DEBUG_DIRECTORY
    {
        public int Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public IMAGE_DEBUG_TYPE Type;
        public int SizeOfData;
        public int AddressOfRawData;
        public int PointerToRawData;
    }
}
