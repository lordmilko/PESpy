namespace PESpy.Native
{
    //See also: ECMA-335 II.24.2.1

    internal unsafe struct STORAGESIGNATURE
    {
        /// <summary>
        /// "Magic" signature.
        /// </summary>
        public uint lSignature;

        /// <summary>
        /// Major file version.
        /// </summary>
        public short iMajorVer;

        /// <summary>
        /// Minor file version.
        /// </summary>
        public short iMinorVer;

        /// <summary>
        /// Offset to next structure of information
        /// </summary>
        public uint iExtraData;

        /// <summary>
        /// Length of version string
        /// </summary>
        public int iVersionString;

        /// <summary>
        /// Version string
        /// </summary>
        public fixed byte pVersion[1];

        //Version is a variable length field. Following this are the following members:

        //Padding after version to next 4 byte boundary
        //Flags (always 0). STGHDR flags are a byte but this field is a short
        //Streams (number of streams)
        //StreamHeaders
    }
}
