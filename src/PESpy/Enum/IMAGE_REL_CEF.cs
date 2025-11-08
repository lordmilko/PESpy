namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_CEF_* enumeration that describes CEF relocation types.
    /// </summary>
    public enum IMAGE_REL_CEF : short
    {
        /// <summary>
        /// Reference is absolute, no relocation is necessary
        /// </summary>
        IMAGE_REL_CEF_ABSOLUTE = 0x0000,

        /// <summary>
        /// 32-bit address (VA).
        /// </summary>
        IMAGE_REL_CEF_ADDR32 = 0x0001,

        /// <summary>
        /// 64-bit address (VA).
        /// </summary>
        IMAGE_REL_CEF_ADDR64 = 0x0002,

        /// <summary>
        /// 32-bit address w/o image base (RVA).
        /// </summary>
        IMAGE_REL_CEF_ADDR32NB = 0x0003,

        /// <summary>
        /// Section index
        /// </summary>
        IMAGE_REL_CEF_SECTION = 0x0004,

        /// <summary>
        /// 32 bit offset from base of section containing target
        /// </summary>
        IMAGE_REL_CEF_SECREL = 0x0005,

        /// <summary>
        /// 32 bit metadata token
        /// </summary>
        IMAGE_REL_CEF_TOKEN = 0x0006
    }
}
