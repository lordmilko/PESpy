namespace PESpy.SYM
{
    public enum MSF : byte
    {
        /* values for md_abstype, gd_type */

        /// <summary>
        /// 32-bit symbols
        /// </summary>
        MSF_32BITSYMS = 0x01,

        /// <summary>
        /// symbols sorted alphabetically, too
        /// </summary>
        MSF_ALPHASYMS = 0x02,

        /* values for gd_type only */

        /// <summary>
        /// bigger than 64K of symdefs
        /// </summary>
        MSF_BIGSYMDEF = 0x04,

        /* values for md_abstype only */

        /// <summary>
        /// 2MEG max symbol file, 32 byte alignment
        /// </summary>
        MSF_ALIGN32 = 0x10,

        /// <summary>
        /// 4MEG max symbol file, 64 byte alignment
        /// </summary>
        MSF_ALIGN64 = 0x20,

        /// <summary>
        /// 8MEG max symbol file, 128 byte alignment
        /// </summary>
        MSF_ALIGN128 = 0x30,

        MSF_ALIGN_MASK = 0x30
    }
}
