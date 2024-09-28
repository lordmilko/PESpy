using System;

namespace PESpy
{
    [Flags]
    public enum STGHDR : byte
    {
        /// <summary>
        /// Normal default flags.
        /// </summary>
        STGHDR_NORMAL = 0x00,

        /// <summary>
        /// Additional data exists after header.
        /// </summary>
        STGHDR_EXTRADATA = 0x01
    }
}
