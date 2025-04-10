namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_SYM_TYPE_* enumeration that describes fundamental types.
    /// </summary>
    public enum ImageSymType : ushort
    {
        /// <summary>
        /// no type.<para/>
        /// IMAGE_SYM_TYPE_NULL
        /// </summary>
        Null = 0x0000,

        /// <summary>
        /// IMAGE_SYM_TYPE_VOID
        /// </summary>
        Void = 0x0001,

        /// <summary>
        /// type character.<para/>
        /// IMAGE_SYM_TYPE_CHAR
        /// </summary>
        Char = 0x0002,

        /// <summary>
        /// type short integer.<para/>
        /// IMAGE_SYM_TYPE_SHORT
        /// </summary>
        Short = 0x0003,

        /// <summary>
        /// IMAGE_SYM_TYPE_INT
        /// </summary>
        Int = 0x0004,

        /// <summary>
        /// IMAGE_SYM_TYPE_LONG
        /// </summary>
        Long = 0x0005,

        /// <summary>
        /// IMAGE_SYM_TYPE_FLOAT
        /// </summary>
        Float = 0x0006,

        /// <summary>
        /// IMAGE_SYM_TYPE_DOUBLE
        /// </summary>
        Double = 0x0007,

        /// <summary>
        /// IMAGE_SYM_TYPE_STRUCT
        /// </summary>
        Struct = 0x0008,

        /// <summary>
        /// IMAGE_SYM_TYPE_UNION
        /// </summary>
        Union = 0x0009,

        /// <summary>
        /// enumeration.<para/>
        /// IMAGE_SYM_TYPE_ENUM
        /// </summary>
        Enum = 0x000A,

        /// <summary>
        /// member of enumeration.<para/>
        /// IMAGE_SYM_TYPE_MOE
        /// </summary>
        MOE = 0x000B,

        /// <summary>
        /// IMAGE_SYM_TYPE_BYTE
        /// </summary>
        Byte = 0x000C,

        /// <summary>
        /// IMAGE_SYM_TYPE_WORD
        /// </summary>
        Word = 0x000D,

        /// <summary>
        /// IMAGE_SYM_TYPE_UINT
        /// </summary>
        UInt = 0x000E,

        /// <summary>
        /// IMAGE_SYM_TYPE_DWORD
        /// </summary>
        DWord = 0x000F,

        /// <summary>
        /// IMAGE_SYM_TYPE_PCODE
        /// </summary>
        PCode = 0x8000
    }
}
