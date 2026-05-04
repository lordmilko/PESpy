using System;

namespace PESpy.LE
{
    [Flags]
    public enum E32BundleEntryFlags
    {
        /// <summary>
        /// Exported entry
        /// </summary>
        E32EXPORT = 0x01,

        /// <summary>
        /// Uses shared data
        /// </summary>
        E32SHARED = 0x02,

        /// <summary>
        /// Parameter word count mask
        /// </summary>
        E32PARAMS = 0xf8
    }
}
