namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_ARM64_* enumeration that describes ARM64 relocation types.
    /// </summary>
    public enum IMAGE_REL_ARM64 : short
    {
        /// <summary>
        /// No relocation required
        /// </summary>
        IMAGE_REL_ARM64_ABSOLUTE = 0x0000,

        /// <summary>
        /// 32 bit address. Review! do we need it?
        /// </summary>
        IMAGE_REL_ARM64_ADDR32 = 0x0001,

        /// <summary>
        /// 32 bit address w/o image base (RVA: for Data/PData/XData)
        /// </summary>
        IMAGE_REL_ARM64_ADDR32NB = 0x0002,

        /// <summary>
        /// 26 bit offset &lt;&lt; 2 &amp; sign ext. for B &amp; BL
        /// </summary>
        IMAGE_REL_ARM64_BRANCH26 = 0x0003,

        /// <summary>
        /// ADRP
        /// </summary>
        IMAGE_REL_ARM64_PAGEBASE_REL21 = 0x0004,

        /// <summary>
        /// ADR
        /// </summary>
        IMAGE_REL_ARM64_REL21 = 0x0005,

        /// <summary>
        /// ADD/ADDS (immediate) with zero shift, for page offset
        /// </summary>
        IMAGE_REL_ARM64_PAGEOFFSET_12A = 0x0006,

        /// <summary>
        /// LDR (indexed, unsigned immediate), for page offset
        /// </summary>
        IMAGE_REL_ARM64_PAGEOFFSET_12L = 0x0007,

        /// <summary>
        /// Offset within section
        /// </summary>
        IMAGE_REL_ARM64_SECREL = 0x0008,

        /// <summary>
        /// ADD/ADDS (immediate) with zero shift, for bit 0:11 of section offset
        /// </summary>
        IMAGE_REL_ARM64_SECREL_LOW12A = 0x0009,

        /// <summary>
        /// ADD/ADDS (immediate) with zero shift, for bit 12:23 of section offset
        /// </summary>
        IMAGE_REL_ARM64_SECREL_HIGH12A = 0x000A,

        /// <summary>
        /// LDR (indexed, unsigned immediate), for bit 0:11 of section offset
        /// </summary>
        IMAGE_REL_ARM64_SECREL_LOW12L = 0x000B,

        IMAGE_REL_ARM64_TOKEN = 0x000C,

        /// <summary>
        /// Section table index
        /// </summary>
        IMAGE_REL_ARM64_SECTION = 0x000D,

        /// <summary>
        /// 64 bit address
        /// </summary>
        IMAGE_REL_ARM64_ADDR64 = 0x000E,

        /// <summary>
        /// 19 bit offset &lt;&lt; 2 &amp; sign ext. for conditional B
        /// </summary>
        IMAGE_REL_ARM64_BRANCH19 = 0x000F
    }
}
