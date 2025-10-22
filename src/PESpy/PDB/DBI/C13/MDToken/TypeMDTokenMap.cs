namespace PESpy.PDB
{
    //Type is made up
    public readonly struct TypeMDTokenMap
    {
        public int NumEntries { get; }

        public Entry[] Entries { get; }

        //Each entry contains a NativeSpan of the specific area of the type data
        //that pertains to it
        public NativeSpan<byte> TypeData { get; }

        internal TypeMDTokenMap(int numEntries, Entry[] entries, NativeSpan<byte> typeData)
        {
            NumEntries = numEntries;
            Entries = entries;
            TypeData = typeData;
        }

        public readonly struct Entry
        {
            public TypOrEnumType TypeIndex { get; }

            //The top bit is cleared. Interpret this as an array of bytes to parse as an ECMA 335 type signature.
            //Not all 4 bytes may be used
            public int SmallTypeSig { get; }

            public NativeSpan<byte> LargeTypeSig { get; }

            public unsafe bool HasSmallTypeSig => (byte*) LargeTypeSig == default;

            internal Entry(TypOrEnumType typeIndex, int smallTypeSig)
            {
                TypeIndex = typeIndex;
                SmallTypeSig = smallTypeSig;
            }

            internal Entry(TypOrEnumType typeIndex, NativeSpan<byte> largeTypeSig)
            {
                TypeIndex = typeIndex;
                LargeTypeSig = largeTypeSig;
            }

            public override string ToString()
            {
                return TypeIndex.ToString();
            }
        }
    }
}
