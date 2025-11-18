namespace PESpy.View
{
    //Describes the type of data pointed to by a ViewByte
    public enum ViewByteDataKind
    {
        //Unlike ViewByteCodeFlags, this is not flags so we can store 8 values

        Unknown = 0,

        //byte, short, int, long
        //Which one is determined by the length
        Integer = 0x10,

        //float, double
        //Which one is determined by the length
        Decimal = 0x20,

        //It's a random global value that happens to be an enum
        Enum = 0x30,

        Guid = 0x40,

        String = 0x50,
        Struct = 0x60,
        Padding = 0x70,
    }
}
