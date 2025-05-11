namespace PESpy.NE
{
    //This type is a bit weird. The bottom two bits are the reference type, and then the third bit
    //is the additive fixup. You can use the reference type mask to get the bottom two bits,
    //but then you've got OSFIXUP which conflicts with NRRORD and NRRNAM
    public enum NewRelocFlags : byte
    {
        /// <summary>
        /// Additive fixup
        /// </summary>
        NRADD = 0x04,

        /// <summary>
        /// Reference type mask
        /// </summary>
        NRRTYP = 0x03,

        /// <summary>
        /// Internal reference
        /// </summary>
        NRRINT = 0x00,

        /// <summary>
        /// Import by ordinal
        /// </summary>
        NRRORD = 0x01,

        /// <summary>
        /// Import by name
        /// </summary>
        NRRNAM = 0x02,

        /// <summary>
        /// Floating point fixup
        /// </summary>
        OSFIXUP = 0x03
    }
}
