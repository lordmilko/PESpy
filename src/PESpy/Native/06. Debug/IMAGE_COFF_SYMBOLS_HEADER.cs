namespace PESpy.Native
{
    internal struct IMAGE_COFF_SYMBOLS_HEADER
    {
        public int NumberOfSymbols;
        public int LvaToFirstSymbol;
        public int NumberOfLinenumbers;
        public int LvaToFirstLinenumber;
        public int RvaToFirstByteOfCode;
        public int RvaToLastByteOfCode;
        public int RvaToFirstByteOfData;
        public int RvaToLastByteOfData;
    }
}
