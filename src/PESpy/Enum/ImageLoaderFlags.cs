namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_LOADER_FLAGS_* enum.
    /// </summary>
    public enum ImageLoaderFlags
    {
        /// <summary>
        /// COM+ image.
        /// </summary>
        ComPlus = 0x00000001,

        /// <summary>
        /// Global subsections apply across TS sessions.
        /// </summary>
        SystemGlobal = 0x01000000
    }
}
