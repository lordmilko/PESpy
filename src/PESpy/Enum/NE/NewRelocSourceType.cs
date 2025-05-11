namespace PESpy.NE
{
    //Name is made up
    public enum NewRelocSourceType : byte
    {
        /// <summary>
        /// Source type mask
        /// </summary>
        NRSTYP = 0x07,

        /// <summary>
        /// 16-bit segment
        /// </summary>
        NRSSEG = 0x02,

        /// <summary>
        /// 32-bit pointer
        /// </summary>
        NRSPTR = 0x03,

        /// <summary>
        /// 16-bit offset
        /// </summary>
        NRSOFF = 0x05
    }
}
