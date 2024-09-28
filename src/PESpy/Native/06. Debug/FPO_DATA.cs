using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //FpoData
    [StructLayout(LayoutKind.Sequential)]
    internal struct FPO_DATA
    {
        public int ulOffStart;
        public int cbProcSize;
        public int cdwLocals;
        public short cdwParams;

        /* WORD        cbProlog : 8;           // # bytes in prolog
         * WORD        cbRegs   : 3;           // # regs saved
         * WORD        fHasSEH  : 1;           // TRUE if SEH in func
         * WORD        fUseBP   : 1;           // TRUE if EBP has been allocated
         * WORD        reserved : 1;           // reserved for future use
         * WORD        cbFrame  : 2;           // frame type */
        public short Flags;
    }
}
