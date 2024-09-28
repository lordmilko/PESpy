using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //See the comments in IMAGE_THUNK_DATA32.cs regarding the nature of this structure

    [StructLayout(LayoutKind.Explicit)]
    internal struct IMAGE_THUNK_DATA64
    {
        [FieldOffset(0)]
        public long ForwarderString;      // PUCHAR

        [FieldOffset(0)]
        public long Function;             // PULONG

        [FieldOffset(0)]
        public long Ordinal;

        [FieldOffset(0)]
        public long AddressOfData;        // PIMAGE_IMPORT_BY_NAME
    }
}