using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //UnwindInfo
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UNWIND_INFO
    {
        public byte VersionAndFlags;
        public byte SizeOfProlog;
        public byte CountOfCodes;
        public byte FrameRegisterAndOffset;

        public byte Version => (byte) (VersionAndFlags & 0x7); //bottom 3 bits

        public UNW_FLAG Flags => (UNW_FLAG) ((VersionAndFlags >> 3) & 0x1f); //top 5 bits

        public byte FrameRegister => (byte) (FrameRegisterAndOffset & 0x0F);

        //The actual frame offset is this 16 * FrameOffset
        public byte FrameOffset => (byte) ((FrameRegisterAndOffset & 0xF0) >> 4);

        public UNWIND_CODE* UnwindCode;

        /* An array of CountOfCode UNWIND_CODE items follows. if CountOfCode is not even,
         * an additional empty UNWIND_CODE also follows for alignment
         *
         *     UNWIND_CODE UnwindCode[1];
         *
         * If Flags contains ExceptionHandler, an ExceptionHandler follows
         *
         *     int ExceptionHandler
         *
         * Otherwise, if Flags contains UNW_FLAG_CHAININFO, a FunctionEntry follows. This member is therefore unioned with ExceptionHandler above
         *
         *     int FunctionEntry
         *
         * After the above union, if Flags contains UNW_FLAG_EHANDLER, ExceptionData follows.
         *
         *     int[] ExceptionData
         *
         * The size and meaning of this data is dependent upon the type of exception handler,
         * as determined by the symbol that the ExceptionHandler member above points to
         */
    }
}
