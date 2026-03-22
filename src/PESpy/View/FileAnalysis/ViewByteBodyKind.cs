namespace PESpy.View
{
    //Describes the type of a body pointed to by a ViewByte
    public enum ViewByteBodyKind
    {
        None = 0,

        //The value is split across multiple pages; this byte is the head of a detached body
        //region what is separated from the head of the actual data structure
        SplitHead = 0x10,

        //The value is split across multiple pages; this byte is the tail of a value region, that may
        //be headed by the actual value head, or a SplitHead
        SplitTail = 0x20,

        //0x30, 0x40, 0x50, 0x60, 0x70 unused
    }
}
