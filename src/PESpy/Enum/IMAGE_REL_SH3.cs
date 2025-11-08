namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_SH3_* enumeration that describes SH3 relocation types.
    /// </summary>
    public enum IMAGE_REL_SH3 : ushort
    {
        /// <summary>
        /// No relocation
        /// </summary>
        IMAGE_REL_SH3_ABSOLUTE = 0x0000,

        /// <summary>
        /// 16 bit direct
        /// </summary>
        IMAGE_REL_SH3_DIRECT16 = 0x0001,

        /// <summary>
        /// 32 bit direct
        /// </summary>
        IMAGE_REL_SH3_DIRECT32 = 0x0002,

        /// <summary>
        /// 8 bit direct, -128..255
        /// </summary>
        IMAGE_REL_SH3_DIRECT8 = 0x0003,

        /// <summary>
        /// 8 bit direct .W (0 ext.)
        /// </summary>
        IMAGE_REL_SH3_DIRECT8_WORD = 0x0004,

        /// <summary>
        /// 8 bit direct .L (0 ext.)
        /// </summary>
        IMAGE_REL_SH3_DIRECT8_LONG = 0x0005,

        /// <summary>
        /// 4 bit direct (0 ext.)
        /// </summary>
        IMAGE_REL_SH3_DIRECT4 = 0x0006,

        /// <summary>
        /// 4 bit direct .W (0 ext.)
        /// </summary>
        IMAGE_REL_SH3_DIRECT4_WORD = 0x0007,

        /// <summary>
        /// 4 bit direct .L (0 ext.)
        /// </summary>
        Direct4Long = 0x0008,

        /// <summary>
        /// 8 bit PC relative .W
        /// </summary>
        PCRel8Word = 0x0009,

        /// <summary>
        /// 8 bit PC relative .L
        /// </summary>
        IMAGE_REL_SH3_PCREL8_LONG = 0x000A,

        /// <summary>
        /// 12 LSB PC relative .W
        /// </summary>
        IMAGE_REL_SH3_PCREL12_WORD = 0x000B,

        /// <summary>
        /// Start of EXE section
        /// </summary>
        IMAGE_REL_SH3_STARTOF_SECTION = 0x000C,

        /// <summary>
        /// Size of EXE section
        /// </summary>
        IMAGE_REL_SH3_SIZEOF_SECTION = 0x000D,

        /// <summary>
        /// Section table index
        /// </summary>
        IMAGE_REL_SH3_SECTION = 0x000E,

        /// <summary>
        /// Offset within section
        /// </summary>
        IMAGE_REL_SH3_SECREL = 0x000F,

        /// <summary>
        /// 32 bit direct not based
        /// </summary>
        IMAGE_REL_SH3_DIRECT32_NB = 0x0010,

        /// <summary>
        /// GP-relative addressing
        /// </summary>
        IMAGE_REL_SH3_GPREL4_LONG = 0x0011,

        /// <summary>
        /// clr token
        /// </summary>
        IMAGE_REL_SH3_TOKEN = 0x0012,

        /// <summary>
        /// Offset from current instruction in longwords.<para/>
        /// If not NOMODE, insert the inverse of the low bit at bit 32 to select PTA/PTB
        /// </summary>
        IMAGE_REL_SHM_PCRELPT = 0x0013,

        /// <summary>
        /// Low bits of 32-bit address
        /// </summary>
        IMAGE_REL_SHM_REFLO = 0x0014,

        /// <summary>
        /// High bits of 32-bit address
        /// </summary>
        IMAGE_REL_SHM_REFHALF = 0x0015,

        /// <summary>
        /// Low bits of relative reference
        /// </summary>
        IMAGE_REL_SHM_RELLO = 0x0016,

        /// <summary>
        /// High bits of relative reference
        /// </summary>
        IMAGE_REL_SHM_RELHALF = 0x0017,

        /// <summary>
        /// offset operand for relocation
        /// </summary>
        IMAGE_REL_SHM_PAIR = 0x0018,

        /// <summary>
        /// relocation ignores section mode
        /// </summary>
        IMAGE_REL_SH_NOMODE = 0x8000
    }
}
