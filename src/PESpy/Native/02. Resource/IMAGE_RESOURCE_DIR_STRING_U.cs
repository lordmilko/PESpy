using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageResourceDirStringU
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IMAGE_RESOURCE_DIR_STRING_U
    {
        public ushort Length;
        public fixed ushort NameString[1];
    }
}