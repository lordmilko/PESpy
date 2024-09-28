using System;
using System.ComponentModel;
using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_DLLCHARACTERISTICS_* enumeration which specifies the DLL characteristics of an <see cref="IMAGE_OPTIONAL_HEADER32"/> / <see cref="IMAGE_OPTIONAL_HEADER64"/>.
    /// </summary>
    [Flags]
    public enum ImageDllCharacteristics : ushort
    {
        /// <summary>
        /// Reserved.
        /// </summary>
        [Description("IMAGE_LIBRARY_PROCESS_INIT")]
        ProcessInit = 1,

        /// <summary>
        /// Reserved.
        /// </summary>
        [Description("IMAGE_LIBRARY_PROCESS_TERM")]
        ProcessTerm = 2,

        /// <summary>
        /// Reserved.
        /// </summary>
        [Description("IMAGE_LIBRARY_THREAD_INIT")]
        ThreadInit = 4,

        /// <summary>
        /// Reserved.
        /// </summary>
        [Description("IMAGE_LIBRARY_THREAD_TERM")]
        ThreadTerm = 8,

        /// <summary>
        /// Image can handle a high entropy 64-bit virtual address space.
        /// </summary>
        [Description("IMAGE_DLLCHARACTERISTICS_HIGH_ENTROPY_VA")]
        HighEntropyVirtualAddressSpace = 32,

        /// <summary>
        /// DLL can be relocated at load time.
        /// </summary>
        [Description("IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE")]
        DynamicBase = 64,

        /// <summary>
        /// Code Integrity checks are enforced.
        /// </summary>
        [Description("IMAGE_DLLCHARACTERISTICS_FORCE_INTEGRITY")]
        ForceIntegrity = 128,

        /// <summary>
        /// Image is NX compatible.
        /// </summary>
        [Description("IMAGE_DLLCHARACTERISTICS_NX_COMPAT")]
        NxCompatible = 256,

        /// <summary>
        /// Isolation aware, but do not isolate the image.
        /// </summary>
        [Description("IMAGE_DLLCHARACTERISTICS_NO_ISOLATION")]
        NoIsolation = 512,

        /// <summary>
        /// Does not use structured exception (SE) handling. No SE handler may be called in this image.
        /// </summary>
        [Description("IMAGE_DLLCHARACTERISTICS_NO_SEH")]
        NoSeh = 1024,

        /// <summary>
        /// Do not bind the image.
        /// </summary>
        [Description("IMAGE_DLLCHARACTERISTICS_NO_BIND")]
        NoBind = 2048,

        /// <summary>
        /// Image must execute in an AppContainer.
        /// </summary>
        [Description("IMAGE_DLLCHARACTERISTICS_APPCONTAINER")]
        AppContainer = 4096,

        /// <summary>
        /// A WDM driver.
        /// </summary>
        [Description("IMAGE_DLLCHARACTERISTICS_WDM_DRIVER")]
        WdmDriver = 8192,

        /// <summary>
        /// Image supports Control Flow Guard.
        /// </summary>
        [Description("IMAGE_DLLCHARACTERISTICS_GUARD_CF")]
        GuardCF = 16384,

        /// <summary>
        /// Terminal Server aware.
        /// </summary>
        [Description("IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE")]
        TerminalServerAware = 32768,
    }
}
