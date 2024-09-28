using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //UnwindCode
    [StructLayout(LayoutKind.Explicit)]
    internal struct UNWIND_CODE
    {
        [FieldOffset(0)]
        public byte CodeOffset;

        [FieldOffset(1)]
        public byte UnwindOpAndOpInfo;

        [FieldOffset(0)]
        public short FrameOffset;
    }
}
