using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //UnwindInfo
    [StructLayout(LayoutKind.Sequential)]
    internal struct UNWIND_INFO
    {
        public byte VersionAndFlags;
        public byte SizeOfProlog;
        public byte CountOfCodes;
        public byte FrameRegisterAndOffset;

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