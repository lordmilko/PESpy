using System;

namespace PESpy
{
    //ehdata_values.h
    [Flags]
    public enum HT : uint
    {
        /// <summary>
        /// // type referenced is 'const' qualified
        /// </summary>
        HT_IsConst = 0x00000001,

        /// <summary>
        /// type referenced is 'volatile' qualified
        /// </summary>
        HT_IsVolatile = 0x00000002,

        /// <summary>
        /// type referenced is 'unaligned' qualified
        /// </summary>
        HT_IsUnaligned = 0x00000004,

        /// <summary>
        /// catch type is by reference
        /// </summary>
        HT_IsReference = 0x00000008,

        /// <summary>
        /// the catch may choose to resume (Reserved)
        /// </summary>
        HT_IsResumable = 0x00000010,

        /// <summary>
        /// the catch is std C++ catch(...) which is supposed to catch only C++ exceptions
        /// </summary>
        HT_IsStdDotDot = 0x00000040,

        /// <summary>
        /// the WinRT type can catch a std::bad_alloc
        /// </summary>
        HT_IsBadAllocCompat = 0x00000080,

        /// <summary>
        /// Is handling within complus EH
        /// </summary>
        HT_IsComplusEh = 0x80000000
    }
}
