using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageTlsDirectory
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_TLS_DIRECTORY64
    {
        public long StartAddressOfRawData;
        public long EndAddressOfRawData;
        public long AddressOfIndex;         // PULONG
        public long AddressOfCallBacks;     // PIMAGE_TLS_CALLBACK *;
        public int SizeOfZeroFill;
        public IMAGE_SCN_ALIGN Characteristics; //Flags
    }
}