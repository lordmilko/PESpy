namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_M32R_* enumeration.
    /// </summary>
    public enum ImageRelM32R : short
    {
        /// <summary>
        /// No relocation required<para/>
        /// IMAGE_REL_M32R_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// 32 bit address<para/>
        /// IMAGE_REL_M32R_ADDR32
        /// </summary>
        Addr32 = 0x0001,

        /// <summary>
        /// 32 bit address w/o image base<para/>
        /// IMAGE_REL_M32R_ADDR32NB
        /// </summary>
        Addr32NB = 0x0002,

        /// <summary>
        /// 24 bit address<para/>
        /// IMAGE_REL_M32R_ADDR24
        /// </summary>
        Addr24 = 0x0003,

        /// <summary>
        /// GP relative addressing<para/>
        /// IMAGE_REL_M32R_GPREL16
        /// </summary>
        GPRel16 = 0x0004,

        /// <summary>
        /// 24 bit offset &lt;&lt; 2 &amp; sign ext.<para/>
        /// IMAGE_REL_M32R_PCREL24
        /// </summary>
        PCRel24 = 0x0005,

        /// <summary>
        /// 16 bit offset &lt;&lt; 2 &amp; sign ext.<para/>
        /// IMAGE_REL_M32R_PCREL16
        /// </summary>
        PCRel16 = 0x0006,

        /// <summary>
        /// 8 bit offset &lt;&lt; 2 &amp; sign ext.<para/>
        /// IMAGE_REL_M32R_PCREL8
        /// </summary>
        PCRel8 = 0x0007,

        /// <summary>
        /// 16 MSBs<para/>
        /// IMAGE_REL_M32R_REFHALF
        /// </summary>
        RefHalf = 0x0008,

        /// <summary>
        /// 16 MSBs; adj for LSB sign ext.<para/>
        /// IMAGE_REL_M32R_REFHI
        /// </summary>
        RefHi = 0x0009,

        /// <summary>
        /// 16 LSBs<para/>
        /// IMAGE_REL_M32R_REFLO
        /// </summary>
        RefLo = 0x000A,

        /// <summary>
        /// Link HI and LO<para/>
        /// IMAGE_REL_M32R_PAIR
        /// </summary>
        Pair = 0x000B,

        /// <summary>
        /// Section table index<para/>
        /// IMAGE_REL_M32R_SECTION
        /// </summary>
        Section = 0x000C,

        /// <summary>
        /// 32 bit section relative reference<para/>
        /// IMAGE_REL_M32R_SECREL32
        /// </summary>
        SecRel32 = 0x000D,

        /// <summary>
        /// clr token<para/>
        /// IMAGE_REL_M32R_TOKEN
        /// </summary>
        Token = 0x000E
    }
}
