using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //Does not have a managed representation as we've only ever seen unicode strings (IMAGE_RESOURCE_DIR_STRING_U)
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IMAGE_RESOURCE_DIRECTORY_STRING
    {
        public ushort Length;
        public fixed byte NameString[1];
    }
}