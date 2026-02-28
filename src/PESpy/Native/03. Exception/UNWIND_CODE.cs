using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //UnwindCode
    [StructLayout(LayoutKind.Explicit)]
    public struct UNWIND_CODE
    {
        [FieldOffset(0)]
        public byte CodeOffset;

        [FieldOffset(1)]
        public byte UnwindOpAndOpInfo;

        [FieldOffset(0)]
        public short FrameOffset;

        public UWOP UnwindOp => (UWOP) (UnwindOpAndOpInfo & 0x0F);
        public byte OpInfo => (byte) ((UnwindOpAndOpInfo & 0xF0) >> 4);
    }
}
