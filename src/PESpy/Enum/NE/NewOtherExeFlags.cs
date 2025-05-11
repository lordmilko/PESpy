using System;

namespace PESpy.NE
{
    //Name is made up
    [Flags]
    public enum NewOtherExeFlags : byte
    {
        /// <summary>
        /// preload segments
        /// </summary>
        NEPRELOAD = 0x08,

        /// <summary>
        /// protect mode
        /// </summary>
        NEINPROT = 0x04,

        /// <summary>
        /// prop. system font
        /// </summary>
        NEINFONT = 0x02,

        /// <summary>
        /// long file names
        /// </summary>
        NELONGNAMES = 0x01
    }
}
