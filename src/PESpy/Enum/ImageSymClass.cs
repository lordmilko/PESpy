using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_SYM_CLASS_* enumeration that defines the storage classes of an <see cref="IMAGE_SYMBOL"/>.
    /// </summary>
    public enum ImageSymClass : byte
    {
        /// <summary>
        /// IMAGE_SYM_CLASS_END_OF_FUNCTION
        /// </summary>
        EndOfFunction = byte.MaxValue,

        /// <summary>
        /// IMAGE_SYM_CLASS_NULL
        /// </summary>
        Null = 0x0000,

        /// <summary>
        /// IMAGE_SYM_CLASS_AUTOMATIC
        /// </summary>
        Automatic = 0x0001,

        /// <summary>
        /// IMAGE_SYM_CLASS_EXTERNAL
        /// </summary>
        External = 0x0002,

        /// <summary>
        /// IMAGE_SYM_CLASS_STATIC
        /// </summary>
        Static = 0x0003,

        /// <summary>
        /// IMAGE_SYM_CLASS_REGISTER
        /// </summary>
        Register = 0x0004,

        /// <summary>
        /// IMAGE_SYM_CLASS_EXTERNAL_DEF
        /// </summary>
        ExternalDef = 0x0005,

        /// <summary>
        /// IMAGE_SYM_CLASS_LABEL
        /// </summary>
        ClassLabel = 0x0006,

        /// <summary>
        /// IMAGE_SYM_CLASS_UNDEFINED_LABEL
        /// </summary>
        UndefinedLabel = 0x0007,

        /// <summary>
        /// IMAGE_SYM_CLASS_MEMBER_OF_STRUCT
        /// </summary>
        MemberOfStruct = 0x0008,

        /// <summary>
        /// IMAGE_SYM_CLASS_ARGUMENT
        /// </summary>
        Argument = 0x0009,

        /// <summary>
        /// IMAGE_SYM_CLASS_STRUCT_TAG
        /// </summary>
        StructTag = 0x000A,

        /// <summary>
        /// IMAGE_SYM_CLASS_MEMBER_OF_UNION
        /// </summary>
        MemberOfUnion = 0x000B,

        /// <summary>
        /// IMAGE_SYM_CLASS_UNION_TAG
        /// </summary>
        UnionTag = 0x000C,

        /// <summary>
        /// IMAGE_SYM_CLASS_TYPE_DEFINITION
        /// </summary>
        Definition = 0x000D,

        /// <summary>
        /// IMAGE_SYM_CLASS_UNDEFINED_STATIC
        /// </summary>
        UndefinedStatic = 0x000E,

        /// <summary>
        /// IMAGE_SYM_CLASS_ENUM_TAG
        /// </summary>
        EnumTag = 0x000F,

        /// <summary>
        /// IMAGE_SYM_CLASS_MEMBER_OF_ENUM
        /// </summary>
        MemberOfEnum = 0x0010,

        /// <summary>
        /// IMAGE_SYM_CLASS_REGISTER_PARAM
        /// </summary>
        RegisterParam = 0x0011,

        /// <summary>
        /// IMAGE_SYM_CLASS_BIT_FIELD
        /// </summary>
        BitField = 0x0012,

        /// <summary>
        /// IMAGE_SYM_CLASS_FAR_EXTERNAL
        /// </summary>
        FarExternal = 0x0044,

        /// <summary>
        /// IMAGE_SYM_CLASS_BLOCK
        /// </summary>
        Block = 0x0064,

        /// <summary>
        /// IMAGE_SYM_CLASS_FUNCTION
        /// </summary>
        Function = 0x0065,

        /// <summary>
        /// IMAGE_SYM_CLASS_END_OF_STRUCT
        /// </summary>
        EndOfStruct = 0x0066,

        /// <summary>
        /// IMAGE_SYM_CLASS_FILE
        /// </summary>
        ClassFile = 0x0067,

        // new
        /// <summary>
        /// IMAGE_SYM_CLASS_SECTION
        /// </summary>
        ClassSection = 0x0068,

        /// <summary>
        /// IMAGE_SYM_CLASS_WEAK_EXTERNAL
        /// </summary>
        WeakExternal = 0x0069,

        /// <summary>
        /// IMAGE_SYM_CLASS_CLR_TOKEN
        /// </summary>
        ClrToken = 0x006B
    }
}
