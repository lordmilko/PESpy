namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_SYM_DTYPE_* enumeration that describes derived types.
    /// </summary>
    public enum IMAGE_SYM_DTYPE : ushort
    {
        /// <summary>
        /// no derived type.
        /// </summary>
        IMAGE_SYM_DTYPE_NULL = 0,

        /// <summary>
        /// pointer.
        /// </summary>
        IMAGE_SYM_DTYPE_POINTER = 1,

        /// <summary>
        /// function.
        /// </summary>
        IMAGE_SYM_DTYPE_FUNCTION = 2,

        /// <summary>
        /// array.
        /// </summary>
        IMAGE_SYM_DTYPE_ARRAY = 3
    }
}
