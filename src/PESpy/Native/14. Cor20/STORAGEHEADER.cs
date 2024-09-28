namespace PESpy.Native
{
    internal struct STORAGEHEADER
    {
        /// <summary>
        /// STGHDR_xxx flags.
        /// </summary>
        public STGHDR fFlags;

        public byte pad;

        /// <summary>
        /// How many streams are there.
        /// </summary>
        public short iStreams;
    }
}
