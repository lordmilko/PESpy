namespace PESpy
{
    public enum ImageRelAm
    {
        /// <summary>
        /// IMAGE_REL_AM_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// IMAGE_REL_AM_ADDR32
        /// </summary>
        Addr32 = 0x0001,

        /// <summary>
        /// IMAGE_REL_AM_ADDR32NB
        /// </summary>
        Addr32NB = 0x0002,

        /// <summary>
        /// IMAGE_REL_AM_CALL32
        /// </summary>
        Call32 = 0x0003,

        /// <summary>
        /// IMAGE_REL_AM_FUNCINFO
        /// </summary>
        FuncInfo = 0x0004,

        /// <summary>
        /// IMAGE_REL_AM_REL32_1
        /// </summary>
        Rel32_1 = 0x0005,

        /// <summary>
        /// IMAGE_REL_AM_REL32_2
        /// </summary>
        Rel32_2 = 0x0006,

        /// <summary>
        /// IMAGE_REL_AM_SECREL
        /// </summary>
        SecRel = 0x0007,

        /// <summary>
        /// IMAGE_REL_AM_SECTION
        /// </summary>
        Section = 0x0008,

        /// <summary>
        /// IMAGE_REL_AM_TOKEN
        /// </summary>
        Token = 0x0009
    }
}
