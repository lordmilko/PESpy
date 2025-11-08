namespace PESpy
{
    /// <summary>
    /// Symbols have a section number of the section in which they are
    /// defined. Otherwise, section numbers have the meanings defined in this enum
    /// </summary>
    public enum IMAGE_SYM : short
    {
        /// <summary>
        /// Symbol is undefined or is common.
        /// </summary>
        IMAGE_SYM_UNDEFINED,

        /// <summary>
        /// Symbol is an absolute value.
        /// </summary>
        IMAGE_SYM_ABSOLUTE = -1,

        /// <summary>
        ///  Symbol is a special debug item.
        /// </summary>
        IMAGE_SYM_DEBUG = -2,

        /// <summary>
        /// Values 0xFF00-0xFFFF are special
        /// </summary>
        IMAGE_SYM_SECTION_MAX = unchecked((short) 0xFEFF),
    }
}
