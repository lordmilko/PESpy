namespace PESpy.Native
{
    internal unsafe struct IMAGE_AUX_SYMBOL_TOKEN_DEF
    {
        public ImageAuxSymbolType bAuxType;
        public byte bReserved;
        public int SymbolTableIndex;
        public fixed byte rgbReserved[12];
    }
}
