using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageNTHeaders
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_NT_HEADERS64
    {
        public int Signature;
        public IMAGE_FILE_HEADER FileHeader;
        public IMAGE_OPTIONAL_HEADER64 OptionalHeader;
    }
}
