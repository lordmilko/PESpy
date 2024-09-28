namespace PESpy.Native
{
    //ImageDynamicRelocationV2
    internal struct IMAGE_DYNAMIC_RELOCATION64_V2
    {
        public int HeaderSize;
        public int FixupInfoSize;
        public long Symbol;
        public int SymbolGroup;
        public int Flags;
        // ...     variable length header fields
        // BYTE    FixupInfo[FixupInfoSize]
    }
}