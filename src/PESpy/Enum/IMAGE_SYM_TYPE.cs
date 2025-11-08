namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_SYM_TYPE_* enumeration that describes fundamental types.
    /// </summary>
    public enum IMAGE_SYM_TYPE : ushort
    {
        /// <summary>
        /// no type.
        /// </summary>
        IMAGE_SYM_TYPE_NULL = 0x0000,

        IMAGE_SYM_TYPE_VOID = 0x0001,

        /// <summary>
        /// type character.
        /// </summary>
        IMAGE_SYM_TYPE_CHAR = 0x0002,

        /// <summary>
        /// type short integer.
        /// </summary>
        IMAGE_SYM_TYPE_SHORT = 0x0003,

        IMAGE_SYM_TYPE_INT = 0x0004,
        IMAGE_SYM_TYPE_LONG = 0x0005,
        IMAGE_SYM_TYPE_FLOAT = 0x0006,
        IMAGE_SYM_TYPE_DOUBLE = 0x0007,
        IMAGE_SYM_TYPE_STRUCT = 0x0008,
        IMAGE_SYM_TYPE_UNION = 0x0009,

        /// <summary>
        /// enumeration.
        /// </summary>
        IMAGE_SYM_TYPE_ENUM = 0x000A,

        /// <summary>
        /// member of enumeration.
        /// </summary>
        IMAGE_SYM_TYPE_MOE = 0x000B,

        IMAGE_SYM_TYPE_BYTE = 0x000C,
        IMAGE_SYM_TYPE_WORD = 0x000D,
        IMAGE_SYM_TYPE_UINT = 0x000E,
        IMAGE_SYM_TYPE_DWORD = 0x000F,
        IMAGE_SYM_TYPE_PCODE = 0x8000
    }
}
