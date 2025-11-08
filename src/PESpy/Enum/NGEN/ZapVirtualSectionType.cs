using System;

namespace PESpy
{
    public static class ZapVirtualSectionTypeFlags
    {
        public const uint IBCTypeReservedFlag = 0xFF000000;
        public const uint RangeTypeReservedFlag = 0x00FF0000;
        public const uint VirtualSectionTypeReservedFlag = 0x0000FFFF;
    }

    [Flags]
    public enum ZapVirtualSectionType
    {
        #region IBCType

        IBCUnProfiledSection = 0x01000000,
        IBCProfiledSection = 0x02000000,

        #endregion
        #region RangeType

        HotRange = 0x00010000,
        WarmRange = 0x00020000,
        ColdRange = 0x00040000,
        HotColdSortedRange = 0x00080000,

        #endregion
        #region VirtualSectionType

        VirtualSectionTypeStartSection = 0x0, // reserved so the first section start at 0x1
        ModuleSection,
        EETableSection,
        WriteDataSection,
        WriteableDataSection,
        DataSection,
        RVAStaticsSection,
        EEDataSection,
        DelayLoadInfoTableEagerSection,
        DelayLoadInfoTableSection,
        EEReadonlyDataSection,
        ReadonlyDataSection,
        ClassSection,
        CrossDomainInfoSection,
        MethodDescSection,
        MethodDescWriteableSection,
        ExceptionSection,
        InstrumentSection,
        VirtualImportThunkSection,
        ExternalMethodThunkSection,
        HelperTableSection,
        MethodPrecodeWriteableSection,
        MethodPrecodeWriteSection,
        MethodPrecodeSection,
        Win32ResourcesSection,
        HeaderSection,
        MetadataSection,
        DelayLoadInfoSection,
        ImportTableSection,
        CodeSection,
        CodeHeaderSection,
        CodeManagerSection,
        UnwindDataSection,
        RuntimeFunctionSection,
        StubsSection,
        StubDispatchDataSection,
        ExternalMethodDataSection,
        DelayLoadInfoDelayListSection,
        ReadonlySharedSection,
        ReadonlySection,
        ILSection,
        GCInfoSection,
        ILMetadataSection,
        ResourcesSection,
        CompressedMapsSection,
        DebugSection,
        BaseRelocsSection,

        CORCOMPILE_SECTION_TYPE_COUNT

        #endregion
    }
}
