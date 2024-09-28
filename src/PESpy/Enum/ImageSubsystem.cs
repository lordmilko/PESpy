using System.ComponentModel;

namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_SUBSYSTEM_* enumeration which specifies the subsystem of an <see cref="IMAGE_OPTIONAL_HEADER32"/> / <see cref="IMAGE_OPTIONAL_HEADER64"/>
    /// that defines the subsystem (if any) required to run an image.
    /// </summary>
    public enum ImageSubsystem : ushort
    {
        /// <summary>
        /// An unknown subsystem
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_UNKNOWN")]
        Unknown = 0,

        /// <summary>
        /// Device drivers and native Windows processes
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_NATIVE")]
        Native = 1,

        /// <summary>
        /// The Windows graphical user interface (GUI) subsystem
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_WINDOWS_GUI")]
        WindowsGui = 2,

        /// <summary>
        /// The Windows character subsystem
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_WINDOWS_CUI")]
        WindowsCui = 3,

        /// <summary>
        /// The OS/2 character subsystem
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_OS2_CUI")]
        OS2Cui = 5,

        /// <summary>
        /// The Posix character subsystem
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_POSIX_CUI")]
        PosixCui = 7,

        /// <summary>
        /// Native Win9x driver
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_NATIVE_WINDOWS")]
        NativeWindows = 8,

        /// <summary>
        /// Windows CE
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_WINDOWS_CE_GUI")]
        WindowsCEGui = 9,

        /// <summary>
        /// An Extensible Firmware Interface (EFI) application
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_EFI_APPLICATION")]
        EfiApplication = 10,

        /// <summary>
        /// An EFI driver with boot services
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER")]
        EfiBootServiceDriver = 11,

        /// <summary>
        /// An EFI driver with run-time services
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER")]
        EfiRuntimeDriver = 12,

        /// <summary>
        /// An EFI ROM image
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_EFI_ROM")]
        EfiRom = 13,

        /// <summary>
        /// XBOX
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_XBOX")]
        Xbox = 14,

        /// <summary>
        /// Windows boot application.
        /// </summary>
        [Description("IMAGE_SUBSYSTEM_WINDOWS_BOOT_APPLICATION")]
        WindowsBootApplication = 16,
    }
}
