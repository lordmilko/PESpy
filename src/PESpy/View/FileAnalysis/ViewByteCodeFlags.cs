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

        IsIL = 0x40,

        //Because this is flags, we can only store 3 values
    }
}
