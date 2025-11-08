namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_LOADER_FLAGS_* enum.
    /// </summary>
    public enum IMAGE_LOADER_FLAGS
    {
        /// <summary>
        /// COM+ image.
        /// </summary>
        IMAGE_LOADER_FLAGS_COMPLUS = 0x00000001,

        /// <summary>
        /// Global subsections apply across TS sessions.
        /// </summary>
        IMAGE_LOADER_FLAGS_SYSTEM_GLOBAL = 0x01000000
    }
}
