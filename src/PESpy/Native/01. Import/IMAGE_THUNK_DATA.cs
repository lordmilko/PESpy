using System;
using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //Architecture agnostic version of IMAGE_THUNK_DATA32/IMAGE_THUNK_DATA64
    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_THUNK_DATA
    {
        [FieldOffset(0)]
        public IntPtr ForwarderString;      // PUCHAR

        [FieldOffset(0)]
        public IntPtr Function;             // PULONG

        [FieldOffset(0)]
        public IntPtr Ordinal;

        [FieldOffset(0)]
        public IntPtr AddressOfData;        // PIMAGE_IMPORT_BY_NAME
    }
}
