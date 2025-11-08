using System;

namespace PESpy
{
    //https://learn.microsoft.com/en-us/windows/win32/api/verrsrc/ns-verrsrc-vs_fixedfileinfo

    /// <summary>
    /// Represents the VOS enumeration which specifies the operation for which this file was designed.
    /// </summary>
    /// <remarks>
    /// An application can combine these values to indicate that the file was designed for one operating system running on another.
    /// e.g. 0x00010001: The file was designed for 16-bit Windows running on MS-DOS.
    /// https://learn.microsoft.com/en-us/windows/win32/api/verrsrc/ns-verrsrc-vs_fixedfileinfo
    /// </remarks>
    [Flags] //Can be flags, see remarks
    public enum VOS : uint
    {
        /// <summary>
        /// The file was designed for MS-DOS.<para/>
        /// VOS_DOS
        /// </summary>
        DOS = 0x00010000,

        /// <summary>
        /// The file was designed for Windows NT.<para/>
        /// VOS_NT
        /// </summary>
        NT = 0x00040000,

        /// <summary>
        /// The file was designed for 16-bit Windows.<para/>
        /// VOS__WINDOWS16
        /// </summary>
        WINDOWS16 = 0x00000001, //Note: name has double underscore

        /// <summary>
        /// The file was designed for 32-bit Windows.<para/>
        /// VOS__WINDOWS32
        /// </summary>
        WINDOWS32 = 0x00000004, //Note: name has double underscore

        /// <summary>
        /// The file was designed for 16-bit OS/2.<para/>
        /// VOS_OS216
        /// </summary>
        OS216 = 0x00020000,

        /// <summary>
        /// The file was designed for 32-bit OS/2.<para/>
        /// VOS_OS232
        /// </summary>
        OS232 = 0x00030000,

        /// <summary>
        /// The file was designed for 16-bit Presentation Manager.<para/>
        /// VOS__PM16
        /// </summary>
        PM16 = 0x00000002, //Note: name has double underscore

        /// <summary>
        /// The file was designed for 32-bit Presentation Manager.<para/>
        /// VOS__PM32
        /// </summary>
        PM32 = 0x00000003, //Note: name has double underscore

        /// <summary>
        /// The operating system for which the file was designed is unknown to the system.<para/>
        /// VOS_UNKNOWN
        /// </summary>
        UNKNOWN = 0
    }
}
