using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageDebugDirectory
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_DEBUG_DIRECTORY
    {
        public int Characteristics;
        public int TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public ImageDebugType Type;
        public int SizeOfData;
        public int AddressOfRawData;
        public int PointerToRawData;
    }
}
