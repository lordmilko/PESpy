namespace PESpy.View
{
    //Flags for a ViewByte when its Kind is Code
    public enum ViewByteCodeFlags
    {
        None = 0,

        /// <summary>
        /// Whether this byte represents the start of a function
        /// </summary>
        Function = 0x10,

        NoReturn = 0x20,

        //0x30, 0x40, 0x50, 0x60 and 0x70 unused
    }
}
