namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_EBC_* enumeration.
    /// </summary>
    public enum ImageRelEbc : short
    {
        /// <summary>
        /// No relocation required<para/>
        /// IMAGE_REL_EBC_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// 32 bit address w/o image base<para/>
        /// IMAGE_REL_EBC_ADDR32NB
        /// </summary>
        Addr32NB = 0x0001,

        /// <summary>
        /// 32-bit relative address from byte following reloc<para/>
        /// IMAGE_REL_EBC_REL32
        /// </summary>
        Rel32 = 0x0002,

        /// <summary>
        /// Section table index<para/>
        /// IMAGE_REL_EBC_SECTION
        /// </summary>
        Section = 0x0003,

        /// <summary>
        /// Offset within section<para/>
        /// IMAGE_REL_EBC_SECREL
        /// </summary>
        SecRel = 0x0004
    }
}
