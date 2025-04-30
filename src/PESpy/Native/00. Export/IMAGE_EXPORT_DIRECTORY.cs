using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageExportDirectory
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_EXPORT_DIRECTORY
    {
        public int Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public int Name;
        public int Base;
        public int NumberOfFunctions;
        public int NumberOfNames;
        public int AddressOfFunctions;     // RVA from base of image
        public int AddressOfNames;         // RVA from base of image
        public int AddressOfNameOrdinals;  // RVA from base of image
    }
}
