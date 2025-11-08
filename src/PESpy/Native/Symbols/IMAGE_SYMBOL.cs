using System.Runtime.InteropServices;

namespace PESpy.Native
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct IMAGE_SYMBOL
    {
        [FieldOffset(0)]
        public fixed byte ShortName[8];

        [FieldOffset(0)]
        public int Short; // if 0, use LongName

        [FieldOffset(4)]
        public int Long; // offset into string table

        [FieldOffset(2)]
        public fixed int LongName[2];

        [FieldOffset(8)]
        public int Value;

        [FieldOffset(12)]
        public short SectionNumber;

        [FieldOffset(14)]
        public IMAGE_SYM_TYPE Type;

        [FieldOffset(16)]
        public IMAGE_SYM_CLASS StorageClass;

        [FieldOffset(17)]
        public byte NumberOfAuxSymbols;
    }
}
