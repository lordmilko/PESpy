namespace PESpy.Native
{
    internal unsafe struct IMAGE_AUX_SYMBOL_TOKEN_DEF
    {
        public IMAGE_AUX_SYMBOL_TYPE bAuxType;
        public byte bReserved;
        public int SymbolTableIndex;
        public fixed byte rgbReserved[12];
    }
}
