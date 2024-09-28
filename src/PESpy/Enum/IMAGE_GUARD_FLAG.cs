using System;

namespace PESpy
{
    //https://learn.microsoft.com/en-us/windows/win32/secbp/pe-metadata

    /// <summary>
    /// Represents additional metadata that has been attached to a Control Flow Guard record in the GuardCFFunctionTable (GFIDS)
    /// </summary>
    [Flags]
    public enum IMAGE_GUARD_FLAG : byte
    {
        /// <summary>
        /// Call target is explicitly suppressed (do not treat it as valid for purposes of CFG)
        /// </summary>
        FID_SUPPRESSED = 1,

        /// <summary>
        /// Call target is export suppressed.
        /// </summary>
        EXPORT_SUPPRESSED = 2,

        FID_LANGEXCPTHANDLER = 4,

        FID_XFG = 8 //Specifies that XFG is present
    }
}
