namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_ARM_* enumeration that describes ARM relocation types.
    /// </summary>
    public enum IMAGE_REL_ARM : short
    {
        /// <summary>
        /// No relocation required
        /// </summary>
        IMAGE_REL_ARM_ABSOLUTE = 0x0000,

        /// <summary>
        /// 32 bit address
        /// </summary>
        IMAGE_REL_ARM_ADDR32 = 0x0001,

        /// <summary>
        /// 32 bit address w/o image base
        /// </summary>
        IMAGE_REL_ARM_ADDR32NB = 0x0002,

        /// <summary>
        /// 24 bit offset &lt;&lt; 2 &amp; sign ext.
        /// </summary>
        IMAGE_REL_ARM_BRANCH24 = 0x0003,

        /// <summary>
        /// Thumb: 2 11 bit offsets
        /// </summary>
        IMAGE_REL_ARM_BRANCH11 = 0x0004,

        /// <summary>
        /// clr token
        /// </summary>
        IMAGE_REL_ARM_TOKEN = 0x0005,

        /// <summary>
        /// GP-relative addressing (ARM)
        /// </summary>
        IMAGE_REL_ARM_GPREL12 = 0x0006,

        /// <summary>
        /// GP-relative addressing (Thumb)
        /// </summary>
        IMAGE_REL_ARM_GPREL7 = 0x0007,

        IMAGE_REL_ARM_BLX24 = 0x0008,
        IMAGE_REL_ARM_BLX11 = 0x0009,

        /// <summary>
        /// Section table index
        /// </summary>
        IMAGE_REL_ARM_SECTION = 0x000E,

        /// <summary>
        /// Offset within section
        /// </summary>
        IMAGE_REL_ARM_SECREL = 0x000F,

        /// <summary>
        /// ARM: MOVW/MOVT
        /// </summary>
        IMAGE_REL_ARM_MOV32A = 0x0010,

        /// <summary>
        /// ARM: MOVW/MOVT (deprecated)
        /// </summary>
        IMAGE_REL_ARM_MOV32 = 0x0010,

        /// <summary>
        /// Thumb: MOVW/MOVT
        /// </summary>
        IMAGE_REL_ARM_MOV32T = 0x0011,

        /// <summary>
        /// Thumb: MOVW/MOVT (deprecated)
        /// </summary>
        IMAGE_REL_THUMB_MOV32 = 0x0011,

        /// <summary>
        /// Thumb: 32-bit conditional B
        /// </summary>
        IMAGE_REL_ARM_BRANCH20T = 0x0012,

        /// <summary>
        /// Thumb: 32-bit conditional B (deprecated)
        /// </summary>
        IMAGE_REL_THUMB_BRANCH20 = 0x0012,

        /// <summary>
        /// Thumb: 32-bit B or BL
        /// </summary>
        IMAGE_REL_ARM_BRANCH24T = 0x0014,

        /// <summary>
        /// Thumb: 32-bit B or BL (deprecated)
        /// </summary>
        IMAGE_REL_THUMB_BRANCH24 = 0x0014,

        /// <summary>
        /// Thumb: BLX immediate
        /// </summary>
        IMAGE_REL_ARM_BLX23T = 0x0015,

        /// <summary>
        /// Thumb: BLX immediate (deprecated)
        /// </summary>
        IMAGE_REL_THUMB_BLX23 = 0x0015
    }
}
