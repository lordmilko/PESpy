using System.Runtime.InteropServices;

namespace PESpy
{
    //ImageRelocation
    [StructLayout(LayoutKind.Explicit)]
    internal struct IMAGE_RELOCATION
    {
        [FieldOffset(0)]
        public int VirtualAddress;

        [FieldOffset(0)]
        public int RelocCount; // Set to the real count when IMAGE_SCN_LNK_NRELOC_OVFL is set

        [FieldOffset(4)]
        public int SymbolTableIndex;

        [FieldOffset(8)]
        public short Type;
    }
}
