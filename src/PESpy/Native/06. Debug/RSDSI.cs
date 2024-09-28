using System;
using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //RSDSI
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct RSDSI
    {
        public int dwSig; //RSDS
        public Guid guidSig;
        public int age;
        public fixed byte szPdb[1]; //Max length: MAX_PATH * 3
    }
}
