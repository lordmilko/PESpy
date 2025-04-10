using System;
using PESpy.Native;

namespace PESpy
{
    //https://learn.microsoft.com/en-us/windows/win32/api/verrsrc/ns-verrsrc-vs_fixedfileinfo

    /// <summary>
    /// Represents the VS_FF_* enumeration which specifies file flags of a VS_FIXEDFILEINFO.
    /// </summary>
    [Flags]
    public enum VS_FF : uint
    {
        /// <summary>
        /// The file contains debugging information or is compiled with debugging features enabled.<para/>
        /// VS_FF_DEBUG
        /// </summary>
        Debug = 0x00000001,

        /// <summary>
        /// The file is a development version, not a commercially released product.<para/>
        /// VS_FF_PRERELEASE
        /// </summary>
        PreRelease = 0x00000002,

        /// <summary>
        /// The file has been modified and is not identical to the original shipping file of the same version number.<para/>
        /// VS_FF_PATCHED
        /// </summary>
        Patched = 0x00000004,

        /// <summary>
        /// The file was not built using standard release procedures. If this flag is set, the <see cref="VsVersionInfo.StringFileInfo"/> structure should contain a PrivateBuild entry.
        /// </summary>
        PrivateBuild = 0x00000008,

        /// <summary>
        /// The file's version structure was created dynamically; therefore, some of the members in this structure may be empty or incorrect.
        /// This flag should never be set in a file's <see cref="VS_VERSIONINFO"/> data.<para/>
        /// VS_FF_INFOINFERRED
        /// </summary>
        InfoInferred = 0x00000010,

        /// <summary>
        /// The file was built by the original company using standard release procedures but is a variation of the normal file of the same version number.
        /// If this flag is set, the <see cref="VsVersionInfo.StringFileInfo"/> structure should contain a SpecialBuild entry.<para/>
        /// VS_FF_SPECIALBUILD
        /// </summary>
        SpecialBuild = 0x00000020
    }
}
