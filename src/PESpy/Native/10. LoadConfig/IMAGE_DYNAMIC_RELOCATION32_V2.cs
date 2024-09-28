namespace PESpy.Native
{
    //ImageDynamicRelocationV2
    internal struct IMAGE_DYNAMIC_RELOCATION32_V2
    {
        public int HeaderSize;
        public int FixupInfoSize;
        public int Symbol;
        public int SymbolGroup;
        public int Flags;
        // ...     variable length header fields
        // BYTE    FixupInfo[FixupInfoSize]
    }
}