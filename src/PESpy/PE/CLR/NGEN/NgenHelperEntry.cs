using System.Diagnostics;

namespace PESpy
{
    //Type is made up

    [DebuggerDisplay("iHelper = {iHelper}, IsPointer = {IsPointer}")]
    public struct NgenHelperEntry
    {
        //On ARM64 it's 16
        internal const int HELPER_TABLE_ENTRY_LEN = 8;

        internal const uint CORCOMPILE_HELPER_PTR = 0x80000000; // The entry is pointer to the helper (jump thunk otherwise)

        public uint dwHelper { get; }

        //The top bit may have the CORCOMPILE_HELPER_PTR bit set, so you need to cast to ushort
        //to get the actual index of the helper
        public ushort iHelper => (ushort) dwHelper;

        public bool IsPointer => (dwHelper & CORCOMPILE_HELPER_PTR) != 0;

        //Using this helperIndex you index into a list of helper names.
        //It seems like these match up both with the enum CorInfoHelpFunc
        //and the list of helpers in jithelpers.h, which is a bit of a problem
        //because the JIT interface is versioned and items are added/removed all the time
    }
}
