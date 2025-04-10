namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_ALPHA_* enumeration that describes Alpha relocation types.
    /// </summary>
    public enum ImageRelAlpha : short
    {
        /// <summary>
        /// IMAGE_REL_ALPHA_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// IMAGE_REL_ALPHA_REFLONG
        /// </summary>
        RefLong = 0x0001,

        /// <summary>
        /// IMAGE_REL_ALPHA_REFQUAD
        /// </summary>
        RefQuad = 0x0002,

        /// <summary>
        /// IMAGE_REL_ALPHA_GPREL32
        /// </summary>
        GPRel32 = 0x0003,

        /// <summary>
        /// IMAGE_REL_ALPHA_LITERAL
        /// </summary>
        Literal = 0x0004,

        /// <summary>
        /// IMAGE_REL_ALPHA_LITUSE
        /// </summary>
        LitUse = 0x0005,

        /// <summary>
        /// IMAGE_REL_ALPHA_GPDISP
        /// </summary>
        GPDisp = 0x0006,

        /// <summary>
        /// IMAGE_REL_ALPHA_BRADDR
        /// </summary>
        BRAddr = 0x0007,

        /// <summary>
        /// IMAGE_REL_ALPHA_HINT
        /// </summary>
        Hint = 0x0008,

        /// <summary>
        /// IMAGE_REL_ALPHA_INLINE_REFLONG
        /// </summary>
        InlineRefLong = 0x0009,

        /// <summary>
        /// IMAGE_REL_ALPHA_REFHI
        /// </summary>
        RefHi = 0x000A,

        /// <summary>
        /// IMAGE_REL_ALPHA_REFLO
        /// </summary>
        RefLo = 0x000B,

        /// <summary>
        /// IMAGE_REL_ALPHA_PAIR
        /// </summary>
        Pair = 0x000C,

        /// <summary>
        /// IMAGE_REL_ALPHA_MATCH
        /// </summary>
        Match = 0x000D,

        /// <summary>
        /// IMAGE_REL_ALPHA_SECTION
        /// </summary>
        Section = 0x000E,

        /// <summary>
        /// IMAGE_REL_ALPHA_SECREL
        /// </summary>
        SecRel = 0x000F,

        /// <summary>
        /// IMAGE_REL_ALPHA_REFLONGNB
        /// </summary>
        RefLongNB = 0x0010,

        /// <summary>
        /// Low 16-bit section relative reference<para/>
        /// IMAGE_REL_ALPHA_SECRELLO
        /// </summary>
        SecRelLo = 0x0011,

        /// <summary>
        /// High 16-bit section relative reference<para/>
        /// IMAGE_REL_ALPHA_SECRELHI
        /// </summary>
        SecRelHi = 0x0012,

        /// <summary>
        /// High 16 bits of 48 bit reference<para/>
        /// IMAGE_REL_ALPHA_REFQ3
        /// </summary>
        RefQ3 = 0x0013,

        /// <summary>
        /// Middle 16 bits of 48 bit reference<para/>
        /// IMAGE_REL_ALPHA_REFQ2
        /// </summary>
        RefQ2 = 0x0014,

        /// <summary>
        /// Low 16 bits of 48 bit reference<para/>
        /// IMAGE_REL_ALPHA_REFQ1
        /// </summary>
        RefQ1 = 0x0015,

        /// <summary>
        /// Low 16-bit GP relative reference<para/>
        /// IMAGE_REL_ALPHA_GPRELLO
        /// </summary>
        GPRelLo = 0x0016,

        /// <summary>
        /// High 16-bit GP relative reference<para/>
        /// IMAGE_REL_ALPHA_GPRELHI
        /// </summary>
        GPRelHi = 0x0017
    }
}
