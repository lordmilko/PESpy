using System;

namespace PESpy
{
    //https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#the-load-configuration-structure-image-only
    [Flags]
    public enum IMAGE_GUARD : uint
    {
        /// <summary>
        /// Module performs control flow integrity checks using system-supplied support.
        /// </summary>
        IMAGE_GUARD_CF_INSTRUMENTED = 0x00000100,

        /// <summary>
        /// Module performs control flow and write integrity checks.
        /// </summary>
        IMAGE_GUARD_CFW_INSTRUMENTED = 0x00000200,

        /// <summary>
        /// Module contains valid control flow target metadata.
        /// </summary>
        IMAGE_GUARD_CF_FUNCTION_TABLE_PRESENT = 0x00000400,

        /// <summary>
        /// Module does not make use of the /GS security cookie.
        /// </summary>
        IMAGE_GUARD_SECURITY_COOKIE_UNUSED = 0x00000800,

        /// <summary>
        /// Module supports read only delay load IAT.
        /// </summary>
        IMAGE_GUARD_PROTECT_DELAYLOAD_IAT = 0x00001000,

        /// <summary>
        /// Delayload import table in its own .didat section (with nothing else in it) that can be freely reprotected.
        /// </summary>
        IMAGE_GUARD_DELAYLOAD_IAT_IN_ITS_OWN_SECTION = 0x00002000,

        /// <summary>
        /// Module contains suppressed export information. This also infers that the address taken IAT table is also present in the load config.
        /// </summary>
        IMAGE_GUARD_CF_EXPORT_SUPPRESSION_INFO_PRESENT = 0x00004000,

        /// <summary>
        /// Module enables suppression of exports.
        /// </summary>
        IMAGE_GUARD_CF_ENABLE_EXPORT_SUPPRESSION = 0x00008000,

        //Module contains longjmp target information.
        IMAGE_GUARD_CF_LONGJUMP_TABLE_PRESENT = 0x00010000,

        IMAGE_GUARD_RF_INSTRUMENTED = 0x00020000,
        IMAGE_GUARD_RF_ENABLE = 0x00040000,
        IMAGE_GUARD_RF_STRICT = 0x00080000,
        IMAGE_GUARD_RETPOLINE_PRESENT = 0x00100000,
        IMAGE_GUARD_EH_CONTINUATION_TABLE_PRESENT = 0x00400000,
        IMAGE_GUARD_XFG_ENABLED = 0x00800000,

        /// <summary>
        /// Mask for the subfield that contains the stride of Control Flow Guard function table entries (that is, the additional count of bytes per table entry).
        /// </summary>
        IMAGE_GUARD_CF_FUNCTION_TABLE_SIZE_MASK = 0xF0000000 //Use with ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT

        //CF_FUNCTION_TABLE_SIZE_SHIFT is a constant value, and is defined in ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT rather than in this enum
    }
}
