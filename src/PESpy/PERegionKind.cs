using System;

namespace PESpy
{
    /// <summary>
    /// Specifies kinds of regions that can be loaded from a PE File.
    /// </summary>
    [Flags]
    public enum PERegionKind : ulong
    {
        None = 0,
        DosHeader = 1,
        DosStub = 2,
        RichHeader = 4,
        NtHeaders = 8,
        SectionHeaders = 0x10,

        ExportTable             = 0x20,
        ImportTable             = 0x40,
        ResourceDirectory       = 0x80,
        ExceptionTable          = 0x100,
        SecurityTable           = 0x200,
        BaseRelocationTable     = 0x400,
        DebugDirectory          = 0x800,
        CopyrightTable          = 0x1000,
        GlobalPointerTable      = 0x2000,
        TlsDirectory            = 0x4000,
        LoadConfigTable         = 0x8000,
        BoundImportTable        = 0x10000,
        ImportAddressTable      = 0x20000,
        DelayImportTable        = 0x40000,

        /// <summary>
        /// The <see cref="ImageCor20Header"/> from the <see cref="ImageOptionalHeader.CorHeaderTableDirectory"/>.
        /// </summary>
        Cor20Header             = 0x80000,

        ILMethods               = 0x100000,

        Cor20Header_Metadata    = 0x200000,
        Cor20Header_Resources   = 0x400000,
        Cor20Header_StrongNameSignature = 0x800000,
        Cor20Header_CodeManagerTable    = 0x1000000,
        Cor20Header_VTableFixups        = 0x2000000,
        Cor20Header_ExportAddressTableJumps = 0x4000000,
        Cor20Header_ManagedNativeHeader = 0x8000000,

        ReadyToRunHeader        = 0x10000000,
        AppHostSignature        = 0x20000000,
        ClrEngineMetrics        = 0x40000000,
        RuntimeInfo             = 0x80000000,
        DotNetRuntimeDebugHeader = 0x100000000
    }
}
