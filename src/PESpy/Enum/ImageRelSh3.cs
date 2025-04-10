namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_SH3_* enumeration that describes SH3 relocation types.
    /// </summary>
    public enum ImageRelSh3 : ushort
    {
        /// <summary>
        /// No relocation<para/>
        /// IMAGE_REL_SH3_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// 16 bit direct<para/>
        /// IMAGE_REL_SH3_DIRECT16
        /// </summary>
        Direct16 = 0x0001,

        /// <summary>
        /// 32 bit direct<para/>
        /// IMAGE_REL_SH3_DIRECT32
        /// </summary>
        Direct32 = 0x0002,

        /// <summary>
        /// 8 bit direct, -128..255<para/>
        /// IMAGE_REL_SH3_DIRECT8
        /// </summary>
        Direct8 = 0x0003,

        /// <summary>
        /// 8 bit direct .W (0 ext.)<para/>
        /// IMAGE_REL_SH3_DIRECT8_WORD
        /// </summary>
        Direct8Word = 0x0004,

        /// <summary>
        /// 8 bit direct .L (0 ext.)<para/>
        /// IMAGE_REL_SH3_DIRECT8_LONG
        /// </summary>
        Direct8Long = 0x0005,

        /// <summary>
        /// 4 bit direct (0 ext.)<para/>
        /// IMAGE_REL_SH3_DIRECT4
        /// </summary>
        Direct4 = 0x0006,

        /// <summary>
        /// 4 bit direct .W (0 ext.)<para/>
        /// IMAGE_REL_SH3_DIRECT4_WORD
        /// </summary>
        Direct4Word = 0x0007,

        /// <summary>
        /// 4 bit direct .L (0 ext.)<para/>
        /// IMAGE_REL_SH3_DIRECT4_LONG
        /// </summary>
        Direct4Long = 0x0008,

        /// <summary>
        /// 8 bit PC relative .W<para/>
        /// IMAGE_REL_SH3_PCREL8_WORD
        /// </summary>
        PCRel8Word = 0x0009,

        /// <summary>
        /// 8 bit PC relative .L<para/>
        /// IMAGE_REL_SH3_PCREL8_LONG
        /// </summary>
        PCRel8Long = 0x000A,

        /// <summary>
        /// 12 LSB PC relative .W<para/>
        /// IMAGE_REL_SH3_PCREL12_WORD
        /// </summary>
        PCRel12Word = 0x000B,

        /// <summary>
        /// Start of EXE section<para/>
        /// IMAGE_REL_SH3_STARTOF_SECTION
        /// </summary>
        StartOfSection = 0x000C,

        /// <summary>
        /// Size of EXE section<para/>
        /// IMAGE_REL_SH3_SIZEOF_SECTION
        /// </summary>
        SizeOfSection = 0x000D,

        /// <summary>
        /// Section table index<para/>
        /// IMAGE_REL_SH3_SECTION
        /// </summary>
        Section = 0x000E,

        /// <summary>
        /// Offset within section<para/>
        /// IMAGE_REL_SH3_SECREL
        /// </summary>
        SecRel = 0x000F,

        /// <summary>
        /// 32 bit direct not based<para/>
        /// IMAGE_REL_SH3_DIRECT32_NB
        /// </summary>
        Direct32NB = 0x0010,

        /// <summary>
        /// GP-relative addressing<para/>
        /// IMAGE_REL_SH3_GPREL4_LONG
        /// </summary>
        GPRel4Long = 0x0011,

        /// <summary>
        /// clr token<para/>
        /// IMAGE_REL_SH3_TOKEN
        /// </summary>
        Token = 0x0012,

        /// <summary>
        /// Offset from current instruction in longwords.<para/>
        /// If not NOMODE, insert the inverse of the low bit at bit 32 to select PTA/PTB<para/>
        /// IMAGE_REL_SHM_PCRELPT
        /// </summary>
        PCRelPT = 0x0013,

        /// <summary>
        /// Low bits of 32-bit address<para/>
        /// IMAGE_REL_SHM_REFLO
        /// </summary>
        RefLo = 0x0014,

        /// <summary>
        /// High bits of 32-bit address<para/>
        /// IMAGE_REL_SHM_REFHALF
        /// </summary>
        RefHalf = 0x0015,

        /// <summary>
        /// Low bits of relative reference<para/>
        /// IMAGE_REL_SHM_RELLO
        /// </summary>
        RelLo = 0x0016,

        /// <summary>
        /// High bits of relative reference<para/>
        /// IMAGE_REL_SHM_RELHALF
        /// </summary>
        RelHalf = 0x0017,

        /// <summary>
        /// offset operand for relocation<para/>
        /// IMAGE_REL_SHM_PAIR
        /// </summary>
        Pair = 0x0018,

        /// <summary>
        /// relocation ignores section mode<para/>
        /// IMAGE_REL_SH_NOMODE
        /// </summary>
        NoMode = 0x8000
    }
}
