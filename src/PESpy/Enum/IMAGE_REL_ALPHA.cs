namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_ALPHA_* enumeration that describes Alpha relocation types.
    /// </summary>
    public enum IMAGE_REL_ALPHA : short
    {
        IMAGE_REL_ALPHA_ABSOLUTE = 0x0000,
        IMAGE_REL_ALPHA_REFLONG = 0x0001,
        IMAGE_REL_ALPHA_REFQUAD = 0x0002,
        IMAGE_REL_ALPHA_GPREL32 = 0x0003,
        IMAGE_REL_ALPHA_LITERAL = 0x0004,
        IMAGE_REL_ALPHA_LITUSE = 0x0005,
        IMAGE_REL_ALPHA_GPDISP = 0x0006,
        IMAGE_REL_ALPHA_BRADDR = 0x0007,
        IMAGE_REL_ALPHA_HINT = 0x0008,
        IMAGE_REL_ALPHA_INLINE_REFLONG = 0x0009,
        IMAGE_REL_ALPHA_REFHI = 0x000A,
        IMAGE_REL_ALPHA_REFLO = 0x000B,
        IMAGE_REL_ALPHA_PAIR = 0x000C,
        IMAGE_REL_ALPHA_MATCH = 0x000D,
        IMAGE_REL_ALPHA_SECTION = 0x000E,
        IMAGE_REL_ALPHA_SECREL = 0x000F,
        IMAGE_REL_ALPHA_REFLONGNB = 0x0010,

        /// <summary>
        /// Low 16-bit section relative reference
        /// </summary>
        IMAGE_REL_ALPHA_SECRELLO = 0x0011,

        /// <summary>
        /// High 16-bit section relative reference
        /// </summary>
        IMAGE_REL_ALPHA_SECRELHI = 0x0012,

        /// <summary>
        /// High 16 bits of 48 bit reference
        /// </summary>
        IMAGE_REL_ALPHA_REFQ3 = 0x0013,

        /// <summary>
        /// Middle 16 bits of 48 bit reference
        /// </summary>
        IMAGE_REL_ALPHA_REFQ2 = 0x0014,

        /// <summary>
        /// Low 16 bits of 48 bit reference
        /// </summary>
        IMAGE_REL_ALPHA_REFQ1 = 0x0015,

        /// <summary>
        /// Low 16-bit GP relative reference
        /// </summary>
        IMAGE_REL_ALPHA_GPRELLO = 0x0016,

        /// <summary>
        /// High 16-bit GP relative reference
        /// </summary>
        IMAGE_REL_ALPHA_GPRELHI = 0x0017
    }
}
