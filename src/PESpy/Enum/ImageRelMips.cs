namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_MIPS_* enumeration that describes MIPS relocation types.
    /// </summary>
    public enum ImageRelMips : short
    {
        /// <summary>
        /// Reference is absolute, no relocation is necessary<para/>
        /// IMAGE_REL_MIPS_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// IMAGE_REL_MIPS_REFHALF
        /// </summary>
        RefHalf = 0x0001,

        /// <summary>
        /// IMAGE_REL_MIPS_REFWORD
        /// </summary>
        RefWord = 0x0002,

        /// <summary>
        /// IMAGE_REL_MIPS_JMPADDR
        /// </summary>
        JumpAddr = 0x0003,

        /// <summary>
        /// IMAGE_REL_MIPS_REFHI
        /// </summary>
        RefHi = 0x0004,

        /// <summary>
        /// IMAGE_REL_MIPS_REFLO
        /// </summary>
        RefLo = 0x0005,

        /// <summary>
        /// IMAGE_REL_MIPS_GPREL
        /// </summary>
        GPRel = 0x0006,

        /// <summary>
        /// IMAGE_REL_MIPS_LITERAL
        /// </summary>
        Literal = 0x0007,

        /// <summary>
        /// IMAGE_REL_MIPS_SECTION
        /// </summary>
        Section = 0x000A,

        /// <summary>
        /// IMAGE_REL_MIPS_SECREL
        /// </summary>
        SecRel = 0x000B,

        /// <summary>
        /// Low 16-bit section relative referemce (used for >32k TLS)<para/>
        /// IMAGE_REL_MIPS_SECRELLO
        /// </summary>
        SecRelLo = 0x000C,

        /// <summary>
        /// High 16-bit section relative reference (used for >32k TLS)<para/>
        /// IMAGE_REL_MIPS_SECRELHI
        /// </summary>
        SecRelHi = 0x000D,

        /// <summary>
        /// clr token<para/>
        /// IMAGE_REL_MIPS_TOKEN
        /// </summary>
        Token = 0x000E,

        /// <summary>
        /// IMAGE_REL_MIPS_JMPADDR16
        /// </summary>
        JmpAddr16 = 0x0010,

        /// <summary>
        /// IMAGE_REL_MIPS_REFWORDNB
        /// </summary>
        RefWordNB = 0x0022,

        /// <summary>
        /// IMAGE_REL_MIPS_PAIR
        /// </summary>
        Pair = 0x0025
    }
}
