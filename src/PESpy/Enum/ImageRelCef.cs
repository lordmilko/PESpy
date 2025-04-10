namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_CEF_* enumeration that describes CEF relocation types.
    /// </summary>
    public enum ImageRelCef : short
    {
        /// <summary>
        /// Reference is absolute, no relocation is necessary<para/>
        /// IMAGE_REL_CEF_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// 32-bit address (VA).<para/>
        /// IMAGE_REL_CEF_ADDR32
        /// </summary>
        Addr32 = 0x0001,

        /// <summary>
        /// 64-bit address (VA).<para/>
        /// IMAGE_REL_CEF_ADDR64
        /// </summary>
        Addr64 = 0x0002,

        /// <summary>
        /// 32-bit address w/o image base (RVA).<para/>
        /// IMAGE_REL_CEF_ADDR32NB
        /// </summary>
        Addr32NB = 0x0003,

        /// <summary>
        /// Section index<para/>
        /// IMAGE_REL_CEF_SECTION
        /// </summary>
        Section = 0x0004,

        /// <summary>
        /// 32 bit offset from base of section containing target<para/>
        /// IMAGE_REL_CEF_SECREL
        /// </summary>
        SecRel = 0x0005,

        /// <summary>
        /// 32 bit metadata token<para/>
        /// IMAGE_REL_CEF_TOKEN
        /// </summary>
        Token = 0x0006
    }
}
