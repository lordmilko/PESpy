using System.Runtime.InteropServices;

namespace PESpy //Public and in PESpy namespace as this may be needed by debuggers
{
    //RuntimeFunction
    [StructLayout(LayoutKind.Sequential)]
    public struct RUNTIME_FUNCTION
    {
        public int BeginAddress;
        public int EndAddress;
        public int UnwindData;
    }
}
