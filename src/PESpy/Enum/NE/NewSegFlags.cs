using System;

namespace PESpy.NE
{
    //Name is made up
    [Flags]
    public enum NewSegFlags : ushort
    {
        /// <summary>
        /// Code segment
        /// </summary>
        NSCODE = 0x0000,

        /// <summary>
        /// Data segment
        /// </summary>
        NSDATA = 0x0001,

        /// <summary>
        /// Iterated segment flag
        /// </summary>
        NSITER = 0x0008,

        /// <summary>
        /// Movable segment flag
        /// </summary>
        NSMOVE = 0x0010,

        /// <summary>
        /// Pure segment flag
        /// </summary>
        NSPURE = 0x0020,

        /// <summary>
        /// Preload segment flag
        /// </summary>
        NSPRELOAD = 0x0040,

        /// <summary>
        /// Execute-only (code segment), or read-only (data segment)
        /// </summary>
        NSEXRD = 0x0080,

        /// <summary>
        /// Segment has relocations
        /// </summary>
        NSRELOC = 0x0100,

        /// <summary>
        /// Segment has debug info
        /// </summary>
        NSDEBUG = 0x0200,

        /// <summary>
        /// 286 DPL bits
        /// </summary>
        NSDPL = 0x0C00,

        /// <summary>
        /// Discard bit for segment
        /// </summary>
        NSDISCARD = 0x1000
    }
}
