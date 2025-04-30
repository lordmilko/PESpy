using System.Runtime.InteropServices;

namespace PESpy.Native
{
    [StructLayout(LayoutKind.Explicit)]
    public struct IMAGE_LINENUMBER
    {
        /// <summary>
        /// Symbol table index of function name if Linenumber is 0.
        /// </summary>
        [FieldOffset(0)]
        public int SymbolTableIndex;

        /// <summary>
        /// Virtual address of line number.
        /// </summary>
        [FieldOffset(0)]
        public int VirtualAddress;

        /// <summary>
        /// Line number.
        /// </summary>
        [FieldOffset(4)]
        public short Linenumber;
    }
}
