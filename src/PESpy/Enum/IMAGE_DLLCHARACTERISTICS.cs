using System;
using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_DLLCHARACTERISTICS_* enumeration which specifies the DLL characteristics of an <see cref="IMAGE_OPTIONAL_HEADER32"/> / <see cref="IMAGE_OPTIONAL_HEADER64"/>.
    /// </summary>
    [Flags]
    public enum IMAGE_DLLCHARACTERISTICS : ushort
    {
        /// <summary>
        /// Reserved.
        /// </summary>
        IMAGE_LIBRARY_PROCESS_INIT = 1,

        /// <summary>
        /// Reserved.
        /// </summary>
        IMAGE_LIBRARY_PROCESS_TERM = 2,

        /// <summary>
        /// Reserved.
        /// </summary>
        IMAGE_LIBRARY_THREAD_INIT = 4,

        /// <summary>
        /// Reserved.
        /// </summary>
        IMAGE_LIBRARY_THREAD_TERM = 8,

        /// <summary>
        /// Image can handle a high entropy 64-bit virtual address space.
        /// </summary>
        IMAGE_DLLCHARACTERISTICS_HIGH_ENTROPY_VA = 32,

        /// <summary>
        /// DLL can be relocated at load time.
        /// </summary>
        IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE = 64,

        /// <summary>
        /// Code Integrity checks are enforced.
        /// </summary>
        IMAGE_DLLCHARACTERISTICS_FORCE_INTEGRITY = 128,

        /// <summary>
        /// Image is NX compatible.
        /// </summary>
        IMAGE_DLLCHARACTERISTICS_NX_COMPAT = 256,

        /// <summary>
        /// Isolation aware, but do not isolate the image.
        /// </summary>
        IMAGE_DLLCHARACTERISTICS_NO_ISOLATION = 512,

        /// <summary>
        /// Does not use structured exception (SE) handling. No SE handler may be called in this image.
        /// </summary>
        IMAGE_DLLCHARACTERISTICS_NO_SEH = 1024,

        /// <summary>
        /// Do not bind the image.
        /// </summary>
        IMAGE_DLLCHARACTERISTICS_NO_BIND = 2048,

        /// <summary>
        /// Image must execute in an AppContainer.
        /// </summary>
        IMAGE_DLLCHARACTERISTICS_APPCONTAINER = 4096,

        /// <summary>
        /// A WDM driver.
        /// </summary>
        IMAGE_DLLCHARACTERISTICS_WDM_DRIVER = 8192,

        /// <summary>
        /// Image supports Control Flow Guard.
        /// </summary>
        IMAGE_DLLCHARACTERISTICS_GUARD_CF = 16384,

        /// <summary>
        /// Terminal Server aware.
        /// </summary>
        IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE = 32768
    }
}
