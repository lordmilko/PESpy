namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_I386_* enumeration that describes I386 relocation types.
    /// </summary>
    public enum IMAGE_REL_I386 : short
    {
        /// <summary>
        /// Reference is absolute, no relocation is necessary
        /// </summary>
        IMAGE_REL_I386_ABSOLUTE = 0x0000,

        /// <summary>
        /// Direct 16-bit reference to the symbols virtual address
        /// </summary>
        IMAGE_REL_I386_DIR16 = 0x0001,

        /// <summary>
        /// PC-relative 16-bit reference to the symbols virtual address
        /// </summary>
        IMAGE_REL_I386_REL16 = 0x0002,

        /// <summary>
        /// Direct 32-bit reference to the symbols virtual address
        /// </summary>
        IMAGE_REL_I386_DIR32 = 0x0006,

        /// <summary>
        /// Direct 32-bit reference to the symbols virtual address, base not included
        /// </summary>
        IMAGE_REL_I386_DIR32NB = 0x0007,

        /// <summary>
        /// Direct 16-bit reference to the segment-selector bits of a 32-bit virtual address
        /// </summary>
        IMAGE_REL_I386_SEG12 = 0x0009,

        IMAGE_REL_I386_SECTION = 0x000A,
        IMAGE_REL_I386_SECREL = 0x000B,

        /// <summary>
        /// clr token
        /// </summary>
        IMAGE_REL_I386_TOKEN = 0x000C,

        /// <summary>
        /// 7 bit offset from base of section containing target
        /// </summary>
        IMAGE_REL_I386_SECREL7 = 0x000D,

        /// <summary>
        /// PC-relative 32-bit reference to the symbols virtual address
        /// </summary>
        IMAGE_REL_I386_REL32 = 0x0014
    }
}
