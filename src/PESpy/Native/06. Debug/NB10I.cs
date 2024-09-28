using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //NB10I
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NB10I
    {
        public int dwSig;
        public int dwOffset;
        public int sig;
        public int age;
        public fixed byte szPdb[260]; //MAX_PATH
    }
}
