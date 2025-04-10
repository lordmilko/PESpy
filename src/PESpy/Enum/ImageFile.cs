using System;
using System.ComponentModel;
using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_FILE_* enumeration which specifies characteristics of an <see cref="IMAGE_FILE_HEADER"/>.
    /// </summary>
    [Flags]
    public enum ImageFile : ushort
    {
        /// <summary>
        /// Image only, Windows CE, and Microsoft Windows NT and later. This indicates that the file does
        /// not contain base relocations and must therefore be loaded at its preferred base address. If the
        /// base address is not available, the loader reports an error. The default behavior of the linker
        /// is to strip base relocations from executable (EXE) files.
        /// </summary>
        [Description("IMAGE_FILE_RELOCS_STRIPPED")]
        RelocsStripped = 1,

        /// <summary>
        /// Image only. This indicates that the image file is valid and can be run. If this flag is not set,
        /// it indicates a linker error.
        /// </summary>
        [Description("IMAGE_FILE_EXECUTABLE_IMAGE")]
        ExecutableImage = 2,

        /// <summary>
        /// COFF line numbers have been removed. This flag is deprecated and should be zero.
        /// </summary>
        [Description("IMAGE_FILE_LINE_NUMS_STRIPPED")]
        LineNumsStripped = 4,

        /// <summary>
        /// COFF symbol table entries for local symbols have been removed. This flag is deprecated and should be zero.
        /// </summary>
        [Description("IMAGE_FILE_LOCAL_SYMS_STRIPPED")]
        LocalSymsStripped = 8,

        /// <summary>
        /// Obsolete. Aggressively trim working set. This flag is deprecated for Windows 2000 and later and must be zero.
        /// </summary>
        [Description("IMAGE_FILE_AGGRESSIVE_WS_TRIM")]
        AggressiveWSTrim = 16,

        /// <summary>
        /// Application can handle > 2-GB addresses.
        /// </summary>
        [Description("IMAGE_FILE_LARGE_ADDRESS_AWARE")]
        LargeAddressAware = 32,

        /// <summary>
        /// Little endian: the least significant bit (LSB) precedes the most significant bit (MSB) in memory. This flag is deprecated and should be zero.
        /// </summary>
        [Description("IMAGE_FILE_BYTES_REVERSED_LO")]
        BytesReversedLo = 128,

        /// <summary>
        /// Machine is based on a 32-bit-word architecture.
        /// </summary>
        [Description("IMAGE_FILE_32BIT_MACHINE")]
        Bit32Machine = 256,

        /// <summary>
        /// Debugging information is removed from the image file.
        /// </summary>
        [Description("IMAGE_FILE_DEBUG_STRIPPED")]
        DebugStripped = 512,

        /// <summary>
        /// If the image is on removable media, fully load it and copy it to the swap file.
        /// </summary>
        [Description("IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP")]
        RemovableRunFromSwap = 1024,

        /// <summary>
        /// If the image is on network media, fully load it and copy it to the swap file.
        /// </summary>
        [Description("IMAGE_FILE_NET_RUN_FROM_SWAP")]
        NetRunFromSwap = 2048,

        /// <summary>
        /// The image file is a system file, not a user program.
        /// </summary>
        [Description("IMAGE_FILE_SYSTEM")]
        System = 4096,

        /// <summary>
        /// The image file is a dynamic-link library (DLL). Such files are considered executable files for almost all purposes, although they cannot be directly run.
        /// </summary>
        [Description("IMAGE_FILE_DLL")]
        Dll = 8192,

        /// <summary>
        /// The file should be run only on a uniprocessor machine.
        /// </summary>
        [Description("IMAGE_FILE_UP_SYSTEM_ONLY")]
        UpSystemOnly = 16384,

        /// <summary>
        /// Big endian: the MSB precedes the LSB in memory. This flag is deprecated and should be zero.
        /// </summary>
        [Description("IMAGE_FILE_BYTES_REVERSED_HI")]
        BytesReversedHi = 32768
    }
}
