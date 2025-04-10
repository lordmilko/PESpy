namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_ARM_* enumeration that describes ARM relocation types.
    /// </summary>
    public enum ImageRelArm : short
    {
        /// <summary>
        /// No relocation required<para/>
        /// IMAGE_REL_ARM_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// 32 bit address<para/>
        /// IMAGE_REL_ARM_ADDR32
        /// </summary>
        Addr32 = 0x0001,

        /// <summary>
        /// 32 bit address w/o image base<para/>
        /// IMAGE_REL_ARM_ADDR32NB
        /// </summary>
        Addr32NB = 0x0002,

        /// <summary>
        /// 24 bit offset &lt;&lt; 2 &amp; sign ext.<para/>
        /// IMAGE_REL_ARM_BRANCH24
        /// </summary>
        Branch24 = 0x0003,

        /// <summary>
        /// Thumb: 2 11 bit offsets<para/>
        /// IMAGE_REL_ARM_BRANCH11
        /// </summary>
        Branch11 = 0x0004,

        /// <summary>
        /// clr token<para/>
        /// IMAGE_REL_ARM_TOKEN
        /// </summary>
        Token = 0x0005,

        /// <summary>
        /// GP-relative addressing (ARM)<para/>
        /// IMAGE_REL_ARM_GPREL12
        /// </summary>
        GPRel12 = 0x0006,

        /// <summary>
        /// GP-relative addressing (Thumb)<para/>
        /// IMAGE_REL_ARM_GPREL7
        /// </summary>
        GPRel7 = 0x0007,

        /// <summary>
        /// IMAGE_REL_ARM_BLX24
        /// </summary>
        Blx24 = 0x0008,

        /// <summary>
        /// IMAGE_REL_ARM_BLX11
        /// </summary>
        Blx11 = 0x0009,

        /// <summary>
        /// Section table index<para/>
        /// IMAGE_REL_ARM_SECTION
        /// </summary>
        Section = 0x000E,

        /// <summary>
        /// Offset within section<para/>
        /// IMAGE_REL_ARM_SECREL
        /// </summary>
        SecRel = 0x000F,

        /// <summary>
        /// ARM: MOVW/MOVT<para/>
        /// IMAGE_REL_ARM_MOV32A
        /// </summary>
        Mov32A = 0x0010,

        /// <summary>
        /// ARM: MOVW/MOVT (deprecated)<para/>
        /// IMAGE_REL_ARM_MOV32
        /// </summary>
        Mov32 = 0x0010,

        /// <summary>
        /// Thumb: MOVW/MOVT<para/>
        /// IMAGE_REL_ARM_MOV32T
        /// </summary>
        Mov32T = 0x0011,

        /// <summary>
        /// Thumb: MOVW/MOVT (deprecated)<para/>
        /// IMAGE_REL_THUMB_MOV32
        /// </summary>
        ThumbMov32 = 0x0011,

        /// <summary>
        /// Thumb: 32-bit conditional B<para/>
        /// IMAGE_REL_ARM_BRANCH20T
        /// </summary>
        Branch20T = 0x0012,

        /// <summary>
        /// Thumb: 32-bit conditional B (deprecated)<para/>
        /// IMAGE_REL_THUMB_BRANCH20
        /// </summary>
        ThumbBranch20 = 0x0012,

        /// <summary>
        /// Thumb: 32-bit B or BL<para/>
        /// IMAGE_REL_ARM_BRANCH24T
        /// </summary>
        Branch24T = 0x0014,

        /// <summary>
        /// Thumb: 32-bit B or BL (deprecated)<para/>
        /// IMAGE_REL_THUMB_BRANCH24
        /// </summary>
        ThumbBranch24 = 0x0014,

        /// <summary>
        /// Thumb: BLX immediate<para/>
        /// IMAGE_REL_ARM_BLX23T
        /// </summary>
        Blx23T = 0x0015,

        /// <summary>
        /// Thumb: BLX immediate (deprecated)<para/>
        /// IMAGE_REL_THUMB_BLX23
        /// </summary>
        ThumbBlx23 = 0x0015
    }
}
