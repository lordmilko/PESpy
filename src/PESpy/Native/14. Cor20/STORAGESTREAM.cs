namespace PESpy.Native
{
    internal unsafe struct STORAGESTREAM
    {
        internal const int MAXSTREAMNAME = 32;

        /// <summary>
        /// Offset in file for this stream.
        /// </summary>
        public int iOffset;

        /// <summary>
        /// Size of the file.
        /// </summary>
        public int iSize;

        /// <summary>
        /// Start of name, null terminated.
        /// </summary>
        public fixed byte rcName[1]; //The max length is 32, but the actual size is variable
    }
}
