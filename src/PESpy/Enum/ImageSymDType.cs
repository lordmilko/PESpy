namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_SYM_DTYPE_* enumeration that describes derived types.
    /// </summary>
    public enum ImageSymDType : ushort
    {
        /// <summary>
        /// no derived type.<para/>
        /// IMAGE_SYM_DTYPE_NULL
        /// </summary>
        Null = 0,

        /// <summary>
        /// pointer.<para/>
        /// IMAGE_SYM_DTYPE_POINTER
        /// </summary>
        Pointer = 1,

        /// <summary>
        /// function.<para/>
        /// IMAGE_SYM_DTYPE_FUNCTION
        /// </summary>
        Function = 2,

        /// <summary>
        /// array.<para/>
        /// IMAGE_SYM_DTYPE_ARRAY
        /// </summary>
        Array = 3
    }
}
