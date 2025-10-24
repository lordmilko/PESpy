namespace PESpy.View
{
    //Describes the type of data pointed to by a ViewByte
    public enum ViewByteDataKind
    {
        Byte = 0,
        Int16 = 0x10,
        Int32 = 0x20,
        Int64 = 0x30,
        Float = 0x40,
        String = 0x50,
        Struct = 0x60,
        Padding = 0x70
    }
}
