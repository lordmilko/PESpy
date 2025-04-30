using System.Runtime.InteropServices;

namespace PESpy.Native
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct IMAGE_AUX_SYMBOL
    {
        [FieldOffset(0)]
        public AuxSym Sym;

        [FieldOffset(0)]
        public AuxFile File;

        [FieldOffset(0)]
        public AuxSection Section;

        [FieldOffset(0)]
        public IMAGE_AUX_SYMBOL_TOKEN_DEF TokenDef;

        [FieldOffset(0)]
        public AuxCrc CRC;

        [StructLayout(LayoutKind.Explicit)]
        internal struct AuxSym
        {
            [FieldOffset(0)]
            public int TagIndex;

            #region Union 4-7

            [FieldOffset(4)]
            public short Linenumber;

            [FieldOffset(6)]
            public short Size;

            [FieldOffset(4)]
            public int TotalSize;

            #endregion
            #region Union 8-15

            [FieldOffset(8)]
            public int PointerToLinenumber;

            [FieldOffset(12)]
            public int PointerToNextFunction;

            [FieldOffset(8)]
            public fixed short Dimension[4];

            #endregion

            [FieldOffset(16)]
            public short TvIndex;
        }

        internal struct AuxFile
        {
            public fixed byte Name[18];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        internal struct AuxSection
        {
            public int Length;                         // section length
            public short NumberOfRelocations;            // number of relocation entries
            public short NumberOfLinenumbers;            // number of line numbers
            public uint CheckSum;                       // checksum for communal
            public short Number;                         // section number to associate with
            public byte Selection;                      // communal selection type
            public byte bReserved;
            public short HighNumber;                     // high bits of the section number
        }

        internal struct AuxCrc
        {
            public int crc;
            public fixed byte rgbReserved[14];
        }
    }
}
