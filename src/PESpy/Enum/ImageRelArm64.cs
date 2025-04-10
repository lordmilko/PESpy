namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_ARM64_* enumeration that describes ARM64 relocation types.
    /// </summary>
    public enum ImageRelArm64 : short
    {
        /// <summary>
        /// No relocation required<para/>
        /// IMAGE_REL_ARM64_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// 32 bit address. Review! do we need it?<para/>
        /// IMAGE_REL_ARM64_ADDR32
        /// </summary>
        Addr32 = 0x0001,

        /// <summary>
        /// 32 bit address w/o image base (RVA: for Data/PData/XData)<para/>
        /// IMAGE_REL_ARM64_ADDR32NB
        /// </summary>
        Addr32NB = 0x0002,

        /// <summary>
        /// 26 bit offset &lt;&lt; 2 &amp; sign ext. for B &amp; BL<para/>
        /// IMAGE_REL_ARM64_BRANCH26
        /// </summary>
        Branch26 = 0x0003,

        /// <summary>
        /// ADRP<para/>
        /// IMAGE_REL_ARM64_PAGEBASE_REL21
        /// </summary>
        PageBaseRel21 = 0x0004,

        /// <summary>
        /// ADR<para/>
        /// IMAGE_REL_ARM64_REL21
        /// </summary>
        Rel21 = 0x0005,

        /// <summary>
        /// ADD/ADDS (immediate) with zero shift, for page offset<para/>
        /// IMAGE_REL_ARM64_PAGEOFFSET_12A
        /// </summary>
        PageOffset12A = 0x0006,

        /// <summary>
        /// LDR (indexed, unsigned immediate), for page offset<para/>
        /// IMAGE_REL_ARM64_PAGEOFFSET_12L
        /// </summary>
        PageOffset12L = 0x0007,

        /// <summary>
        /// Offset within section<para/>
        /// IMAGE_REL_ARM64_SECREL
        /// </summary>
        SecRel = 0x0008,

        /// <summary>
        /// ADD/ADDS (immediate) with zero shift, for bit 0:11 of section offset<para/>
        /// IMAGE_REL_ARM64_SECREL_LOW12A
        /// </summary>
        SecRelLow12A = 0x0009,

        /// <summary>
        /// ADD/ADDS (immediate) with zero shift, for bit 12:23 of section offset<para/>
        /// IMAGE_REL_ARM64_SECREL_HIGH12A
        /// </summary>
        SecRelHigh12A = 0x000A,

        /// <summary>
        /// LDR (indexed, unsigned immediate), for bit 0:11 of section offset<para/>
        /// IMAGE_REL_ARM64_SECREL_LOW12L
        /// </summary>
        SecRelLow12L = 0x000B,

        /// <summary>
        /// IMAGE_REL_ARM64_TOKEN
        /// </summary>
        Token = 0x000C,

        /// <summary>
        /// Section table index<para/>
        /// IMAGE_REL_ARM64_SECTION
        /// </summary>
        Section = 0x000D,

        /// <summary>
        /// 64 bit address<para/>
        /// IMAGE_REL_ARM64_ADDR64
        /// </summary>
        Addr64 = 0x000E,

        /// <summary>
        /// 19 bit offset &lt;&lt; 2 &amp; sign ext. for conditional B<para/>
        /// IMAGE_REL_ARM64_BRANCH19
        /// </summary>
        Branch19 = 0x000F
    }
}
