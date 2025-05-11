namespace PESpy.NE
{
    //Name is made up
    public enum NewOperatingSystem : byte
    {
        /// <summary>
        /// Unknown (any "new-format" OS)
        /// </summary>
        NE_UNKNOWN = 0,

        /// <summary>
        /// Microsoft/IBM OS/2 (default)
        /// </summary>
        NE_OS2 = 1,

        /// <summary>
        /// Microsoft Windows
        /// </summary>
        NE_WINDOWS = 2,

        /// <summary>
        /// Microsoft MS-DOS 4.x
        /// </summary>
        NE_DOS4 = 3,

        /// <summary>
        /// Microsoft Windows 386
        /// </summary>
        NE_DEV386 = 4
    }
}
