namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_I386_* enumeration that describes I386 relocation types.
    /// </summary>
    public enum ImageRelI386 : short
    {
        /// <summary>
        /// Reference is absolute, no relocation is necessary<para/>
        /// IMAGE_REL_I386_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// Direct 16-bit reference to the symbols virtual address<para/>
        /// IMAGE_REL_I386_DIR16
        /// </summary>
        Dir16 = 0x0001,

        /// <summary>
        /// PC-relative 16-bit reference to the symbols virtual address<para/>
        /// IMAGE_REL_I386_REL16
        /// </summary>
        Rel16 = 0x0002,

        /// <summary>
        /// Direct 32-bit reference to the symbols virtual address<para/>
        /// IMAGE_REL_I386_DIR32
        /// </summary>
        Dir32 = 0x0006,

        /// <summary>
        /// Direct 32-bit reference to the symbols virtual address, base not included<para/>
        /// IMAGE_REL_I386_DIR32NB
        /// </summary>
        Dir32NB = 0x0007,

        /// <summary>
        /// Direct 16-bit reference to the segment-selector bits of a 32-bit virtual address<para/>
        /// IMAGE_REL_I386_SEG12
        /// </summary>
        Seg12 = 0x0009,

        /// <summary>
        /// IMAGE_REL_I386_SECTION
        /// </summary>
        Section = 0x000A,

        /// <summary>
        /// IMAGE_REL_I386_SECREL
        /// </summary>
        SecRel = 0x000B,

        /// <summary>
        /// clr token<para/>
        /// IMAGE_REL_I386_TOKEN
        /// </summary>
        Token = 0x000C,

        /// <summary>
        /// 7 bit offset from base of section containing target<para/>
        /// IMAGE_REL_I386_SECREL7
        /// </summary>
        SecRel7 = 0x000D,

        /// <summary>
        /// PC-relative 32-bit reference to the symbols virtual address<para/>
        /// IMAGE_REL_I386_REL32
        /// </summary>
        Rel32 = 0x0014
    }
}
