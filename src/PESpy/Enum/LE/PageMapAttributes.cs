using System;

namespace PESpy.LE
{
    public enum PageMapAttributes : byte
    {
        /// <summary>
        /// Valid Physical Page in .EXE
        /// </summary>
        VALID = 0x00,

        /// <summary>
        /// Iterated Data Page
        /// </summary>
        ITERDATA = 0x01,

        /// <summary>
        /// Invalid Page
        /// </summary>
        INVALID = 0x02,

        /// <summary>
        /// Zero Filled Page
        /// </summary>
        ZEROED = 0x03,

        /// <summary>
        /// Range of pages
        /// </summary>
        RANGE = 0x04
    }
}
