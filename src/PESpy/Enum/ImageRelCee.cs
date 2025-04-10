namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_CEE_* enumeration that describes CLR relocation types.
    /// </summary>
    public enum ImageRelCee : short
    {
        /// <summary>
        /// Reference is absolute, no relocation is necessary<para/>
        /// IMAGE_REL_CEE_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// 32-bit address (VA).<para/>
        /// IMAGE_REL_CEE_ADDR32
        /// </summary>
        Addr32 = 0x0001,

        /// <summary>
        /// 64-bit address (VA).<para/>
        /// IMAGE_REL_CEE_ADDR64
        /// </summary>
        Addr64 = 0x0002,

        /// <summary>
        /// 32-bit address w/o image base (RVA).<para/>
        /// IMAGE_REL_CEE_ADDR32NB
        /// </summary>
        Addr32NB = 0x0003,

        /// <summary>
        /// Section index<para/>
        /// IMAGE_REL_CEE_SECTION
        /// </summary>
        Section = 0x0004,

        /// <summary>
        /// 32 bit offset from base of section containing target<para/>
        /// IMAGE_REL_CEE_SECREL
        /// </summary>
        SecRel = 0x0005,

        /// <summary>
        /// 32 bit metadata token<para/>
        /// IMAGE_REL_CEE_TOKEN
        /// </summary>
        Token = 0x0006
    }
}
