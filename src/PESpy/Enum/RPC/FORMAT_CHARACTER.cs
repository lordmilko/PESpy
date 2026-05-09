namespace PESpy
{
    public enum FORMAT_CHARACTER : byte
    {
        FC_ZERO,

        //
        // Simple integer and floating point types.
        //

        FC_BYTE,                    // 0x01
        FC_CHAR,                    // 0x02
        FC_SMALL,                   // 0x03
        FC_USMALL,                  // 0x04

        FC_WCHAR,                   // 0x05
        FC_SHORT,                   // 0x06
        FC_USHORT,                  // 0x07

        FC_LONG,                    // 0x08
        FC_ULONG,                   // 0x09

        FC_FLOAT,                   // 0x0a

        FC_HYPER,                   // 0x0b

        FC_DOUBLE,                  // 0x0c

        FC_ENUM16,                  // 0x0d
        FC_ENUM32,                  // 0x0e

        FC_IGNORE,                  // 0x0f
        FC_ERROR_STATUS_T,          // 0x10

        //
        // Pointer types :
        //     RP - reference pointer
        //     UP - unique pointer
        //     OP - OLE unique pointer
        //     FP - full pointer
        //

        FC_RP,                      // 0x11
        FC_UP,                      // 0x12
        FC_OP,                      // 0x13
        FC_FP,                      // 0x14

        //
        // Structures
        //

        //
        // Structure containing only simple types and fixed arrays.
        //
        FC_STRUCT,                  // 0x15

        //
        // Structure containing only simple types, pointers and fixed arrays.
        //
        FC_PSTRUCT,                 // 0x16

        //
        // Structure containing a conformant array plus all those types
        // allowed by FC_STRUCT.
        //
        FC_CSTRUCT,                 // 0x17

        //
        // Struct containing a conformant array plus all those types allowed by
        // FC_PSTRUCT.
        //
        FC_CPSTRUCT,                // 0x18

        //
        // Struct containing either a conformant varying array or a conformant
        // string, plus all those types allowed by FC_PSTRUCT.
        //
        FC_CVSTRUCT,                // 0x19

        //
        // Complex struct - totally bogus!
        //
        FC_BOGUS_STRUCT,            // 0x1a

        //
        // Arrays.
        //

        //
        // Conformant arrray.
        //
        FC_CARRAY,                  // 0x1b

        //
        // Conformant varying array.
        //
        FC_CVARRAY,                 // 0x1c

        //
        // Fixed array, small and large.
        //
        FC_SMFARRAY,                // 0x1d
        FC_LGFARRAY,                // 0x1e

        //
        // Varying array, small and large.
        //
        FC_SMVARRAY,                // 0x1f
        FC_LGVARRAY,                // 0x20

        //
        // Complex arrays - totally bogus!
        //
        FC_BOGUS_ARRAY,             // 0x21

        //
        // Strings :
        //
        // The order of these should have been moved around, but it's too late
        // now.
        //
        //     CSTRING - character string
        //     BSTRING - byte string (Beta2 compatability only)
        //     SSTRING - structure string
        //     WSTRING - wide charater string
        //

        //
        // Conformant strings.
        //
        FC_C_CSTRING,               // 0x22
        FC_C_BSTRING,               // 0x23
        FC_C_SSTRING,               // 0x24
        FC_C_WSTRING,               // 0x25

        //
        // Non-conformant strings.
        //
        FC_CSTRING,                 // 0x26
        FC_BSTRING,                 // 0x27
        FC_SSTRING,                 // 0x28
        FC_WSTRING,                 // 0x29

        //
        // Unions
        //
        FC_ENCAPSULATED_UNION,      // 0x2a
        FC_NON_ENCAPSULATED_UNION,  // 0x2b

        //
        // Byte count pointer.
        //
        FC_BYTE_COUNT_POINTER,      // 0x2c

        //
        // transmit_as and represent_as
        //
        FC_TRANSMIT_AS,             // 0x2d
        FC_REPRESENT_AS,            // 0x2e

        //
        // Cairo Interface pointer.
        //
        FC_IP,                      // 0x2f

        //
        // Binding handle types
        //
        FC_BIND_CONTEXT,            // 0x30
        FC_BIND_GENERIC,            // 0x31
        FC_BIND_PRIMITIVE,          // 0x32
        FC_AUTO_HANDLE,             // 0x33
        FC_CALLBACK_HANDLE,         // 0x34
        FC_UNUSED1,                 // 0x35

        // Embedded pointer - used in complex structure layouts only.
        FC_POINTER,                 // 0x36

        //
        // Alignment directives, used in structure layouts.
        // No longer generated with post NT5.0 MIDL.
        //

        FC_ALIGNM2,                 // 0x37 
        FC_ALIGNM4,                 // 0x38
        FC_ALIGNM8,                 // 0x39

        FC_UNUSED2,                 // 0x3a
        FC_UNUSED3,                 // 0x3b
        FC_UNUSED4,                 // 0x3c

        //
        // Structure padding directives, used in structure layouts only.
        //
        FC_STRUCTPAD1,              // 0x3d
        FC_STRUCTPAD2,              // 0x3e
        FC_STRUCTPAD3,              // 0x3f
        FC_STRUCTPAD4,              // 0x40
        FC_STRUCTPAD5,              // 0x41
        FC_STRUCTPAD6,              // 0x42
        FC_STRUCTPAD7,              // 0x43

        //
        // Additional string attribute.
        //
        FC_STRING_SIZED,            // 0x44

        FC_UNUSED5,                 // 0x45

        //
        // Pointer layout attributes.
        //
        FC_NO_REPEAT,               // 0x46
        FC_FIXED_REPEAT,            // 0x47
        FC_VARIABLE_REPEAT,         // 0x48
        FC_FIXED_OFFSET,            // 0x49
        FC_VARIABLE_OFFSET,         // 0x4a

        // Pointer section delimiter.
        FC_PP,                      // 0x4b

        // Embedded complex type.
        FC_EMBEDDED_COMPLEX,        // 0x4c

        // Parameter attributes.
        FC_IN_PARAM,                // 0x4d
        FC_IN_PARAM_BASETYPE,       // 0x4e
        FC_IN_PARAM_NO_FREE_INST,   // 0x4d
        FC_IN_OUT_PARAM,            // 0x50
        FC_OUT_PARAM,               // 0x51
        FC_RETURN_PARAM,            // 0x52
        FC_RETURN_PARAM_BASETYPE,   // 0x53

        //
        // Conformance/variance attributes.
        //
        FC_DEREFERENCE,             // 0x54
        FC_DIV_2,                   // 0x55
        FC_MULT_2,                  // 0x56
        FC_ADD_1,                   // 0x57
        FC_SUB_1,                   // 0x58
        FC_CALLBACK,                // 0x59

        // Iid flag.
        FC_CONSTANT_IID,            // 0x5a

        FC_END,                     // 0x5b
        FC_PAD,                     // 0x5c
        FC_EXPR,                    // 0x5d
        FC_PARTIAL_IGNORE_PARAM,    // 0x5e

        //
        // split Conformance/variance attributes.
        //
        FC_SPLIT_DEREFERENCE = 0x74,      // 0x74
        FC_SPLIT_DIV_2,                   // 0x75
        FC_SPLIT_MULT_2,                  // 0x76
        FC_SPLIT_ADD_1,                   // 0x77
        FC_SPLIT_SUB_1,                   // 0x78
        FC_SPLIT_CALLBACK,                // 0x79

        //
        // Attributes, directives, etc.
        //

        //
        // New types.
        //

        FC_HARD_STRUCT = 0xb1,      // 0xb1

        FC_TRANSMIT_AS_PTR,         // 0xb2
        FC_REPRESENT_AS_PTR,        // 0xb3

        FC_USER_MARSHAL,            // 0xb4

        FC_PIPE,                    // 0xb5

        FC_BLKHOLE,                 // 0xb6

        FC_RANGE,                   // 0xb7

        FC_INT3264,                 // 0xb8
        FC_UINT3264,                // 0xb9

        //
        // Arrays of international characters
        //
        FC_CSARRAY,                 // 0xba
        FC_CS_TAG,                  // 0xbb

        // Replacement for alignment in structure layout.
        FC_STRUCTPADN,              // 0xbc

        FC_INT128,                  // 0xbd
        FC_UINT128,                 // 0xbe
        FC_FLOAT80,                 // 0xbf
        FC_FLOAT128,                // 0xc0

        FC_BUFFER_ALIGN,            // 0xc1

        //
        // New Ndr64 codes
        //

        FC_ENCAP_UNION,             // 0xc2

        // new arrays types

        FC_FIX_ARRAY,               // 0xc3
        FC_CONF_ARRAY,              // 0xc4
        FC_VAR_ARRAY,               // 0xc5 
        FC_CONFVAR_ARRAY,           // 0xc6
        FC_FIX_FORCED_BOGUS_ARRAY,  // 0xc7
        FC_FIX_BOGUS_ARRAY,         // 0xc8 
        FC_FORCED_BOGUS_ARRAY,      // 0xc9

        FC_CHAR_STRING,             // 0xca
        FC_WCHAR_STRING,            // 0xcb
        FC_STRUCT_STRING,           // 0xcc

        FC_CONF_CHAR_STRING,        // 0xcd
        FC_CONF_WCHAR_STRING,       // 0xce
        FC_CONF_STRUCT_STRING,      // 0xcf

        // new structures types
        FC_CONF_STRUCT,             // 0xd0
        FC_CONF_PSTRUCT,            // 0xd1
        FC_CONFVAR_STRUCT,          // 0xd2
        FC_CONFVAR_PSTRUCT,         // 0xd3
        FC_FORCED_BOGUS_STRUCT,     // 0xd4
        FC_CONF_BOGUS_STRUCT,       // 0xd5
        FC_FORCED_CONF_BOGUS_STRUCT,// 0xd7

        FC_END_OF_UNIVERSE          // 0xd8

    }
}
