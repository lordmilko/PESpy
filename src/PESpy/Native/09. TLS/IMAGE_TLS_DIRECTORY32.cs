using System;
using System.Runtime.InteropServices;

namespace PESpy.Native
{
    internal delegate IntPtr PIMAGE_TLS_CALLBACK(
        [In] IntPtr DllHandle,
        [In] int Reason,
        [In] IntPtr Reserved);

    //ImageTlsDirectory
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_TLS_DIRECTORY32
    {
        public int StartAddressOfRawData;
        public int EndAddressOfRawData;
        public int AddressOfIndex;             // PULONG
        public int AddressOfCallBacks;         // PIMAGE_TLS_CALLBACK *
        public int SizeOfZeroFill;
        public IMAGE_SCN_ALIGN Characteristics; //Flags
    }
}