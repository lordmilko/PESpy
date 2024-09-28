using System.Runtime.InteropServices;

namespace PESpy.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SCOPE_TABLE
    {
        public int Count;

        //An array of ScopeRecord entries follows, containing the following anonymous struct definition:

        /*struct
        {
            ULONG BeginAddress;
            ULONG EndAddress;
            ULONG HandlerAddress;
            ULONG JumpTarget;
        } ScopeRecord[1]; */
    }
}
