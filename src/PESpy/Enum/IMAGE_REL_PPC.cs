namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_PPC_* enumeration that describes IBM PowerPC relocation types.
    /// </summary>
    public enum IMAGE_REL_PPC : short
    {
        /// <summary>
        /// NOP
        /// </summary>
        IMAGE_REL_PPC_ABSOLUTE = 0x0000,

        /// <summary>
        /// 64-bit address
        /// </summary>
        IMAGE_REL_PPC_ADDR64 = 0x0001,

        /// <summary>
        /// 32-bit address
        /// </summary>
        IMAGE_REL_PPC_ADDR32 = 0x0002,

        /// <summary>
        /// 26-bit address, shifted left 2 (branch absolute)
        /// </summary>
        IMAGE_REL_PPC_ADDR24 = 0x0003,

        /// <summary>
        /// 16-bit address
        /// </summary>
        IMAGE_REL_PPC_ADDR16 = 0x0004,

        /// <summary>
        /// 16-bit address, shifted left 2 (load doubleword)
        /// </summary>
        IMAGE_REL_PPC_ADDR14 = 0x0005,

        /// <summary>
        /// 26-bit PC-relative offset, shifted left 2 (branch relative)
        /// </summary>
        IMAGE_REL_PPC_REL24 = 0x0006,

        /// <summary>
        /// 16-bit PC-relative offset, shifted left 2 (br cond relative)
        /// </summary>
        IMAGE_REL_PPC_REL14 = 0x0007,

        /// <summary>
        /// 16-bit offset from TOC base
        /// </summary>
        IMAGE_REL_PPC_TOCREL16 = 0x0008,

        /// <summary>
        /// 16-bit offset from TOC base, shifted left 2 (load doubleword)
        /// </summary>
        IMAGE_REL_PPC_TOCREL14 = 0x0009,

        /// <summary>
        /// 32-bit addr w/o image base
        /// </summary>
        IMAGE_REL_PPC_ADDR32NB = 0x000A,

        /// <summary>
        /// va of containing section (as in an image sectionhdr)
        /// </summary>
        IMAGE_REL_PPC_SECREL = 0x000B,

        /// <summary>
        /// sectionheader number
        /// </summary>
        IMAGE_REL_PPC_SECTION = 0x000C,

        /// <summary>
        /// substitute TOC restore instruction iff symbol is glue code
        /// </summary>
        IMAGE_REL_PPC_IFGLUE = 0x000D,

        /// <summary>
        /// symbol is glue code; virtual address is TOC restore instruction
        /// </summary>
        IMAGE_REL_PPC_IMGLUE = 0x000E,

        /// <summary>
        /// va of containing section (limited to 16 bits)
        /// </summary>
        IMAGE_REL_PPC_SECREL16 = 0x000F,

        IMAGE_REL_PPC_REFHI = 0x0010,
        IMAGE_REL_PPC_REFLO = 0x0011,
        IMAGE_REL_PPC_PAIR = 0x0012,

        /// <summary>
        /// Low 16-bit section relative reference (used for >32k TLS)
        /// </summary>
        IMAGE_REL_PPC_SECRELLO = 0x0013,

        /// <summary>
        /// High 16-bit section relative reference (used for >32k TLS)
        /// </summary>
        IMAGE_REL_PPC_SECRELHI = 0x0014,

        IMAGE_REL_PPC_GPREL = 0x0015,

        /// <summary>
        /// clr token
        /// </summary>
        IMAGE_REL_PPC_TOKEN = 0x0016,

        /// <summary>
        /// mask to isolate above values in IMAGE_RELOCATION.Type
        /// </summary>
        IMAGE_REL_PPC_TYPEMASK = 0x00FF,

        /// <summary>
        /// subtract reloc value rather than adding it
        /// </summary>
        IMAGE_REL_PPC_NEG = 0x0100,

        /// <summary>
        /// fix branch prediction bit to predict branch taken
        /// </summary>
        IMAGE_REL_PPC_BRTAKEN = 0x0200,

        /// <summary>
        /// fix branch prediction bit to predict branch not taken
        /// </summary>
        IMAGE_REL_PPC_BRNTAKEN = 0x0400,

        /// <summary>
        /// toc slot defined in file (or, data in toc)
        /// </summary>
        IMAGE_REL_PPC_TOCDEFN = 0x0800
    }
}
