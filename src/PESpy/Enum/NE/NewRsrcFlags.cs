using System;

namespace PESpy.NE
{
    //Name is made up
    [Flags]
    public enum NewRsrcFlags : ushort
    {
        /// <summary>
        /// Moveable resource
        /// </summary>
        RNMOVE = 0x0010,

        /// <summary>
        /// Pure (read-only) resource
        /// </summary>
        RNPURE = 0x0020,

        /// <summary>
        /// Preloaded resource
        /// </summary>
        RNPRELOAD = 0x0040,

        /// <summary>
        /// Discard bit for resource
        /// </summary>
        RNDISCARD = 0x1000,

        /// <summary>
        /// True if handler proc return handle
        /// </summary>
        RNLOADED = 0x0004
    }
}
