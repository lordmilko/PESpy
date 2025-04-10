namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_PPC_* enumeration that describes IBM PowerPC relocation types.
    /// </summary>
    public enum ImageRelPpc : short
    {
        /// <summary>
        /// NOP<para/>
        /// IMAGE_REL_PPC_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// 64-bit address<para/>
        /// IMAGE_REL_PPC_ADDR64
        /// </summary>
        Addr64 = 0x0001,

        /// <summary>
        /// 32-bit address<para/>
        /// IMAGE_REL_PPC_ADDR32
        /// </summary>
        Addr32 = 0x0002,

        /// <summary>
        /// 26-bit address, shifted left 2 (branch absolute)<para/>
        /// IMAGE_REL_PPC_ADDR24
        /// </summary>
        Addr24 = 0x0003,

        /// <summary>
        /// 16-bit address<para/>
        /// IMAGE_REL_PPC_ADDR16
        /// </summary>
        Addr16 = 0x0004,

        /// <summary>
        /// 16-bit address, shifted left 2 (load doubleword)<para/>
        /// IMAGE_REL_PPC_ADDR14
        /// </summary>
        Addr14 = 0x0005,

        /// <summary>
        /// 26-bit PC-relative offset, shifted left 2 (branch relative)<para/>
        /// IMAGE_REL_PPC_REL24
        /// </summary>
        Rel24 = 0x0006,

        /// <summary>
        /// 16-bit PC-relative offset, shifted left 2 (br cond relative)<para/>
        /// IMAGE_REL_PPC_REL14
        /// </summary>
        Rel14 = 0x0007,

        /// <summary>
        /// 16-bit offset from TOC base<para/>
        /// IMAGE_REL_PPC_TOCREL16
        /// </summary>
        TocRel16 = 0x0008,

        /// <summary>
        /// 16-bit offset from TOC base, shifted left 2 (load doubleword)<para/>
        /// IMAGE_REL_PPC_TOCREL14
        /// </summary>
        TocRel14 = 0x0009,

        /// <summary>
        /// 32-bit addr w/o image base<para/>
        /// IMAGE_REL_PPC_ADDR32NB
        /// </summary>
        Addr32NB = 0x000A,

        /// <summary>
        /// va of containing section (as in an image sectionhdr)<para/>
        /// IMAGE_REL_PPC_SECREL
        /// </summary>
        SecRel = 0x000B,

        /// <summary>
        /// sectionheader number<para/>
        /// IMAGE_REL_PPC_SECTION
        /// </summary>
        Section = 0x000C,

        /// <summary>
        /// substitute TOC restore instruction iff symbol is glue code<para/>
        /// IMAGE_REL_PPC_IFGLUE
        /// </summary>
        IFGlue = 0x000D,

        /// <summary>
        /// symbol is glue code; virtual address is TOC restore instruction<para/>
        /// IMAGE_REL_PPC_IMGLUE
        /// </summary>
        IMGlue = 0x000E,

        /// <summary>
        /// va of containing section (limited to 16 bits)<para/>
        /// IMAGE_REL_PPC_SECREL16
        /// </summary>
        SecRel16 = 0x000F,

        /// <summary>
        /// IMAGE_REL_PPC_REFHI
        /// </summary>
        RefHi = 0x0010,

        /// <summary>
        /// IMAGE_REL_PPC_REFLO
        /// </summary>
        RefLo = 0x0011,

        /// <summary>
        /// IMAGE_REL_PPC_PAIR
        /// </summary>
        Pair = 0x0012,

        /// <summary>
        /// Low 16-bit section relative reference (used for >32k TLS)<para/>
        /// IMAGE_REL_PPC_SECRELLO
        /// </summary>
        SecRelLo = 0x0013,

        /// <summary>
        /// High 16-bit section relative reference (used for >32k TLS)<para/>
        /// IMAGE_REL_PPC_SECRELHI
        /// </summary>
        SecRelHi = 0x0014,

        /// <summary>
        /// IMAGE_REL_PPC_GPREL
        /// </summary>
        GPRel = 0x0015,

        /// <summary>
        /// clr token<para/>
        /// IMAGE_REL_PPC_TOKEN
        /// </summary>
        Token = 0x0016,

        /// <summary>
        /// mask to isolate above values in IMAGE_RELOCATION.Type<para/>
        /// IMAGE_REL_PPC_TYPEMASK
        /// </summary>
        TypeMask = 0x00FF,

        /// <summary>
        /// subtract reloc value rather than adding it<para/>
        /// IMAGE_REL_PPC_NEG
        /// </summary>
        Neg = 0x0100,

        /// <summary>
        /// fix branch prediction bit to predict branch taken<para/>
        /// IMAGE_REL_PPC_BRTAKEN
        /// </summary>
        BRTaken = 0x0200,

        /// <summary>
        /// fix branch prediction bit to predict branch not taken<para/>
        /// IMAGE_REL_PPC_BRNTAKEN
        /// </summary>
        BRNTaken = 0x0400,

        /// <summary>
        /// toc slot defined in file (or, data in toc)<para/>
        /// IMAGE_REL_PPC_TOCDEFN
        /// </summary>
        TocDefn = 0x0800
    }
}
