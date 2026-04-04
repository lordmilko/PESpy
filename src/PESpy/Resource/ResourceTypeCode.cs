namespace PESpy
{
    public enum ResourceTypeCode
    {
        // Primitives
        Null = 0,
        String = 1,
        Boolean = 2,
        Char = 3,
        Byte = 4,
        SByte = 5,
        Int16 = 6,
        UInt16 = 7,
        Int32 = 8,
        UInt32 = 9,
        Int64 = 0xa,
        UInt64 = 0xb,
        Single = 0xc,
        Double = 0xd,
        Decimal = 0xe,
        DateTime = 0xf,
        TimeSpan = 0x10,

        // A meta-value - change this if you add new primitives
        LastPrimitive = TimeSpan,

        // Types with a special representation, like byte[] and Stream
        ByteArray = 0x20,
        Stream = 0x21,

        // User types - serialized using the binary formatter.
        StartOfUserTypes = 0x40
    }
}
