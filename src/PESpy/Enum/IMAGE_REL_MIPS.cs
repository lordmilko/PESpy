namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_MIPS_* enumeration that describes MIPS relocation types.
    /// </summary>
    public enum IMAGE_REL_MIPS : short
    {
        /// <summary>
        /// Reference is absolute, no relocation is necessary
        /// </summary>
        IMAGE_REL_MIPS_ABSOLUTE = 0x0000,

        IMAGE_REL_MIPS_REFHALF = 0x0001,
        IMAGE_REL_MIPS_REFWORD = 0x0002,
        IMAGE_REL_MIPS_JMPADDR = 0x0003,
        IMAGE_REL_MIPS_REFHI = 0x0004,
        IMAGE_REL_MIPS_REFLO = 0x0005,
        IMAGE_REL_MIPS_GPREL = 0x0006,
        IMAGE_REL_MIPS_LITERAL = 0x0007,
        IMAGE_REL_MIPS_SECTION = 0x000A,
        IMAGE_REL_MIPS_SECREL = 0x000B,

        /// <summary>
        /// Low 16-bit section relative referemce (used for >32k TLS)
        /// </summary>
        IMAGE_REL_MIPS_SECRELLO = 0x000C,

        /// <summary>
        /// High 16-bit section relative reference (used for >32k TLS)
        /// </summary>
        IMAGE_REL_MIPS_SECRELHI = 0x000D,

        /// <summary>
        /// clr token
        /// </summary>
        IMAGE_REL_MIPS_TOKEN = 0x000E,

        IMAGE_REL_MIPS_JMPADDR16 = 0x0010,
        IMAGE_REL_MIPS_REFWORDNB = 0x0022,
        IMAGE_REL_MIPS_PAIR = 0x0025
    }
}
