using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageDebugMisc
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct IMAGE_DEBUG_MISC
    {
        public ImageDebugMiscType DataType;
        public int Length;

        public byte Unicode;
        public fixed byte Reserved[3];

        public fixed byte Data[1];
    }
}
