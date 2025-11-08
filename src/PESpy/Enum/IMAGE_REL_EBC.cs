namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_EBC_* enumeration.
    /// </summary>
    public enum IMAGE_REL_EBC : short
    {
        /// <summary>
        /// No relocation required
        /// </summary>
        IMAGE_REL_EBC_ABSOLUTE = 0x0000,

        /// <summary>
        /// 32 bit address w/o image base
        /// </summary>
        IMAGE_REL_EBC_ADDR32NB = 0x0001,

        /// <summary>
        /// 32-bit relative address from byte following reloc
        /// </summary>
        IMAGE_REL_EBC_REL32 = 0x0002,

        /// <summary>
        /// Section table index
        /// </summary>
        IMAGE_REL_EBC_SECTION = 0x0003,

        /// <summary>
        /// Offset within section
        /// </summary>
        IMAGE_REL_EBC_SECREL = 0x0004
    }
}
