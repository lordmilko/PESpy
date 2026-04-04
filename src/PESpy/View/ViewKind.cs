using System.ComponentModel;

namespace PESpy.View
{
    /// <summary>
    /// Specifies the kind of value contained in a view.
    /// </summary>
    public enum ViewKind : ushort
    {
        /// <summary>
        /// A <see cref="FileView"/> encapsulating the views of a <see cref="PEFile"/>.
        /// </summary>
        PEFile = 1,

        /// <summary>
        /// A <see cref="HeaderView"/> that encapsulates the beginning of the Portable Executable (starting from <see cref="ImageDosHeader"/>)
        /// up to the beginning of the first section.
        /// </summary>
        Header,

        /// <summary>
        /// A <see cref="SectionView"/> that encapsulates the contents of a given section described by a <see cref="ImageSectionHeader"/>.
        /// </summary>
        Section,

        /// <summary>
        /// An <see cref="OverlayView"/> that encapsulates all data that exists after the end of the last section.<para/>
        /// This value is only present when reading a Portable Executable file from disk.
        /// </summary>
        Overlay,

        /// <summary>
        /// A <see cref="ByteBlob"/> containing at least one non-<see langword="0"/> byte, indicating
        /// that the bytes represent code or data whose meaning could not be automatically determined.
        /// </summary>
        Data,

        /// <summary>
        /// A <see cref="ByteBlob"/> whose bytes are all <see langword="0" />, indicating
        /// that the bytes are merely used for padding.
        /// </summary>
        Padding,

        /// <summary>
        /// A <see cref="ByteBlob"/> whose bytes are all <see langword="0xCC" />, indicating
        /// that the bytes are merely used for padding.
        /// </summary>
        CC,

        ImageArchivePad,

        /// <summary>
        /// A named <see cref="IFieldView"/> contained in an <see cref="IStructView"/>.
        /// </summary>
        Field,

        BitField,

        /// <summary>
        /// A simple <see cref="IValueView"/> that was discovered through an RVA, or is known to exist
        /// within a larger <see cref="LogicalRegionView"/>.<para/>
        /// If the <see cref="IValueView"/> contains a <see cref="string"/>, the type will instead be <see cref="String"/>.
        /// </summary>
        Value,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates one or more strings.
        /// </summary>
        Strings,

        String,
        StringLength, //For length prefixed string

        //Unknown decimal value
        Decimal,

        Assembly,

        DataDirectory,

        ILMethods,

        UnwindInfos,

        #region Headers

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDosHeader"/>.
        /// </summary>
        ImageDosHeader,

        /// <summary>
        /// A <see cref="ByteBlob"/> that contains the bytes of the DOS Stub.
        /// </summary>
        DosStub,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RichHeader"/>.
        /// </summary>
        RichHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ProdItem"/>.
        /// </summary>
        ProdItem,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageNtHeaders"/>.
        /// </summary>
        ImageNtHeaders,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageFileHeader"/>.
        /// </summary>
        ImageFileHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageOptionalHeader"/>.
        /// </summary>
        ImageOptionalHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDataDirectory"/>.
        /// </summary>
        ImageDataDirectory,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageSectionHeader"/>.
        /// </summary>
        ImageSectionHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageRelocation"/>.
        /// </summary>
        ImageRelocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.CoffSymbolTable"/>.
        /// </summary>
        CoffSymbolTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageSymbol"/>.
        /// </summary>
        ImageSymbol,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageAuxSymbol"/>.
        /// </summary>
        ImageAuxSymbol,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageLineNumber"/>.
        /// </summary>
        ImageLineNumber,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.AnonObjectHeader"/>.
        /// </summary>
        AnonObjectHeader,

        #endregion
        #region Exports Table (0)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageExportDirectory"/>.
        /// </summary>
        ImageExportDirectory,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="AnsiString"/>.
        /// </summary>
        ImageExportDirectory_Name,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="AnsiString"/>.
        /// </summary>
        ImageExportDirectory_ForwarderName,

        ImageExportDirectory_AddressOfNameOrdinals_Entry,
        ImageExportDirectory_AddressOfNames_Entry,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="AnsiString"/>.
        /// </summary>
        ImageExportDirectory_AddressOfNames_Name,

        ImageExportDirectory_AddressOfFunctions_Entry,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates the <see cref="ImageExportDirectory.AddressOfFunctions"/> region.
        /// </summary>
        ExportAddressTable,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates the <see cref="ImageExportDirectory.AddressOfNames"/> region.
        /// </summary>
        ExportNamesTable,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates the <see cref="ImageExportDirectory.AddressOfNameOrdinals"/> region.
        /// </summary>
        ExportOrdinalsTable,

        #endregion
        #region Import Table (1)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageImportDescriptor"/>.
        /// </summary>
        ImageImportDescriptor,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="AnsiString"/>.
        /// </summary>
        ImageImportDescriptor_Name,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="AnsiString"/>.
        /// </summary>
        ImageEnclaveImport_ImportName,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageThunkData"/>.
        /// </summary>
        ImageThunkData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageImportByName"/>.
        /// </summary>
        ImageImportByName,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates the <see cref="ImageImportDescriptor.OriginalFirstThunk"/> region.
        /// </summary>
        [Description("Import Lookup Table (Thunks)")]
        ImportLookupTable,

        //When read from the import directory, its a logical region. When read from the IAT, it's a data directory
        //todo: so would it be a logicalregionview or a datadirectory?
        ImportAddressTable,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates the <see cref="ImageImportByName"/> structures (and the padding between them)
        /// pointed to by the <see cref="ImageThunkData"/> structures of the <see cref="ImportLookupTable"/> in <see cref="ImageImportDescriptor.OriginalFirstThunk"/>.
        /// </summary>
        [Description("Import Strings")]
        ImportStrings,

        #endregion
        #region Resource Directory (2)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageResourceDirectory"/>.
        /// </summary>
        ImageResourceDirectory,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageResourceDirectoryEntry"/>.
        /// </summary>
        ImageResourceDirectoryEntry,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageResourceDataEntry"/>.
        /// </summary>
        ImageResourceDataEntry,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageResourceDirStringU"/>.
        /// </summary>
        ImageResourceDirStringU,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VsVersionInfo"/>.
        /// </summary>
        VsVersionInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VsFixedFileInfo"/>.
        /// </summary>
        VsFixedFileInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VsVersionInfo.StringFileInfo"/>.
        /// </summary>
        StringFileInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="VsVersionInfo.StringTable"/>.
        /// </summary>
        StringTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="VsVersionInfo.String"/> contained in a <see cref="VsVersionInfo.StringTable"/>.
        /// </summary>
        StringTable_String,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VsVersionInfo.VarFileInfo"/>.
        /// </summary>
        VarFileInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VsVersionInfo.Var"/> contained in a <see cref="PESpy.VsVersionInfo.VarFileInfo"/>.
        /// </summary>
        VarFileInfo_Var,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ClrDebugResource"/>.
        /// </summary>
        ClrDebugResource,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.MessageResourceData"/>.
        /// </summary>
        MessageResourceData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.MessageResourceBlock"/>.
        /// </summary>
        MessageResourceBlock,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.MessageResourceEntry"/>.
        /// </summary>
        MessageResourceEntry,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="FixedUtf8String"/>.
        /// </summary>
        Manifest,

        #endregion
        #region Exception Table (3)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RuntimeFunction"/>.
        /// </summary>
        RuntimeFunction,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.UnwindInfo"/>.
        /// </summary>
        UnwindInfo,

        UnwindInfo_ExceptionData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.UnwindCode"/>.
        /// </summary>
        UnwindCode,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ScopeTable"/>.
        /// </summary>
        ScopeTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="ScopeTable.ScopeRecord"/>.
        /// </summary>
        ScopeRecord,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.FuncInfo"/>.
        /// </summary>
        FuncInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.FuncInfoV1"/>.
        /// </summary>
        FuncInfoV1,

        FuncInfo4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.FuncInfoHeader"/>.
        /// </summary>
        FuncInfoHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.HandlerType"/>.
        /// </summary>
        HandlerType,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.IptoStateMapEntry"/>.
        /// </summary>
        IptoStateMapEntry,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.TryBlockMapEntry"/>.
        /// </summary>
        TryBlockMapEntry,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.TypeDescriptor"/>.
        /// </summary>
        TypeDescriptor,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.UnwindMapEntry"/>.
        /// </summary>
        UnwindMapEntry,

        #endregion
        #region Security Table (4)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.WinCertificate"/>.
        /// </summary>
        WinCertificate,

        SignedData,

        #endregion
        #region Base Relocation Table (5)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageBaseRelocation"/>.
        /// </summary>
        ImageBaseRelocation,

        BaseRelocationEntry,

        #endregion
        #region Debug Table (6)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDebugDirectory"/>.
        /// </summary>
        ImageDebugDirectory,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.NB10I"/>.
        /// </summary>
        NB10I,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RSDSI"/>.
        /// </summary>
        RSDSI,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.FpoData"/>.
        /// </summary>
        FpoData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.XFixupData"/>.
        /// </summary>
        XFixupData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDebugMisc"/>.
        /// </summary>
        ImageDebugMisc,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCoffSymbolsHeader"/>.
        /// </summary>
        ImageCoffSymbolsHeader,

        Omap,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VCFeature"/>
        /// </summary>
        VCFeature,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PogoData"/>
        /// </summary>
        PogoData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PogoItem"/>
        /// </summary>
        PogoItem,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Reproducible"/>
        /// </summary>
        Reproducible,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.EmbeddedPortablePdb"/>
        /// </summary>
        EmbeddedPortablePdb,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PdbChecksum"/>
        /// </summary>
        PdbChecksum,

        ExDllCharacteristics,

        #endregion
        #region Copyright Table (7)

        #endregion
        #region Global Pointer Table (8)

        #endregion
        #region Thread Local Storage Table (9)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageTlsDirectory"/>.
        /// </summary>
        ImageTlsDirectory,

        #endregion
        #region Load Config Table (10)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageLoadConfigDirectory"/>
        /// </summary>
        ImageLoadConfigDirectory,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageLoadConfigCodeIntegrity"/>
        /// </summary>
        ImageLoadConfigCodeIntegrity,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageEnclaveConfig"/>
        /// </summary>
        ImageEnclaveConfig,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageEnclaveImport"/>
        /// </summary>
        ImageEnclaveImport,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.GuardAddressTakenIatEntryTable"/>
        /// </summary>
        GuardAddressTakenIatEntryTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.GuardCFFunctionTable"/>.
        /// </summary>
        GuardCFFunctionTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.GuardEHContinuationTable"/>
        /// </summary>
        GuardEHContinuationTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.GuardLongJumpTargetTable"/>
        /// </summary>
        GuardLongJumpTargetTable,

        GuardAddressTakenIatEntryTable_Entry,
        GuardCFFunctionTable_Entry,
        GuardEHContinuationTable_Entry,
        GuardLongJumpTargetTable_Entry,

        XFG,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="int"/>
        /// </summary>
        LockPrefixTable,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        SecurityCookie,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="int"/>
        /// </summary>
        SEHandlerTable,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        GuardCFCheckFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        GuardCFDispatchFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        GuardRFFailureRoutineFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        GuardRFVerifyStackPointerFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        GuardXFGCheckFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        GuardXFGDispatchFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        GuardXFGTableDispatchFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        CastGuardOsDeterminedFailureMode,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        GuardMemcpyFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        UmaFunctionPointers,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDynamicRelocationTable"/>
        /// </summary>
        ImageDynamicRelocationTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDynamicRelocation"/>
        /// </summary>
        ImageDynamicRelocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDynamicRelocationV2"/>
        /// </summary>
        ImageDynamicRelocationV2,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageFunctionOverrideHeader"/>
        /// </summary>
        ImageFunctionOverrideHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImagePrologueDynamicRelocationHeader"/>
        /// </summary>
        ImagePrologueDynamicRelocationHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageEpilogueDynamicRelocationHeader"/>
        /// </summary>
        ImageEpilogueDynamicRelocationHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageImportControlTransferDynamicRelocation"/>
        /// </summary>
        ImageImportControlTransferDynamicRelocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageIndirControlTransferDynamicRelocation"/>
        /// </summary>
        ImageIndirControlTransferDynamicRelocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageSwitchTableBranchDynamicRelocation"/>
        /// </summary>
        ImageSwitchTableBranchDynamicRelocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageFunctionOverrideDynamicRelocation"/>
        /// </summary>
        ImageFunctionOverrideDynamicRelocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageBDDInfo"/>
        /// </summary>
        ImageBDDInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageBDDDynamicRelocation"/>
        /// </summary>
        ImageBDDDynamicRelocation,

        #endregion
        #region Bound Import Table (11)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageBoundImportDescriptor"/>
        /// </summary>
        ImageBoundImportDescriptor,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="AnsiString"/>.
        /// </summary>
        ImageBoundImportName,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageBoundForwarderRef"/>
        /// </summary>
        ImageBoundForwarderRef,

        #endregion

        //Import Address Table (12)

        #region Delay Import Table (13)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDelayLoadDescriptor"/>
        /// </summary>
        ImageDelayLoadDescriptor,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="AnsiString"/>.
        /// </summary>
        ImageDelayLoadDescriptor_DllNameRVA,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        ImageDelayLoadDescriptor_ModuleHandleRVA,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates the <see cref="ImageDelayLoadDescriptor.ImportNameTableRVA"/> region.
        /// </summary>
        [Description("Delay Import Lookup Table (Thunks)")]
        DelayImportLookupTable,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates the <see cref="ImageDelayLoadDescriptor.ImportAddressTableRVA"/> region.
        /// </summary>
        DelayImportAddressTable,

        DelayUnloadInformationTable,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates the <see cref="ImageImportByName"/> structures (and the padding between them)
        /// pointed to by the <see cref="ImageThunkData"/> structures of the <see cref="DelayImportLookupTable"/> in <see cref="ImageDelayLoadDescriptor.ImportNameTableRVA"/>.
        /// </summary>
        [Description("Delay Import Strings")]
        DelayImportStrings,

        #endregion
        #region CorHeader Directory (14)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCor20Header"/>
        /// </summary>
        ImageCor20Header,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorILMethod"/>
        /// </summary>
        ImageCorILMethodTiny,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorILMethod"/>
        /// </summary>
        ImageCorILMethodFat,

        ImageCorILMethodSectEH,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorILMethodSect"/>
        /// </summary>
        ImageCorILMethodSect,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorILMethodSectEHClause"/>
        /// </summary>
        ImageCorILMethodSectEHClause,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.StorageSignature"/>
        /// </summary>
        StorageSignature,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.StorageHeader"/>
        /// </summary>
        StorageHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.StorageStream"/>
        /// </summary>
        StorageStream,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Ecma335.CompressedModelHeap"/>
        /// </summary>
        CompressedModelHeap,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Ecma335.StringHeap"/>
        /// </summary>
        StringPoolHeap,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Ecma335.UserStringHeap"/>
        /// </summary>
        USBlobPoolHeap,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Ecma335.BlobHeap"/>
        /// </summary>
        BlobPoolHeap,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Ecma335.GuidHeap"/>
        /// </summary>
        GuidPoolHeap,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Ecma335.CompressedModelHeader"/>
        /// </summary>
        MetadataHeader,

        MetadataTable,

        #region Metadata Rows

        Metadata_String,
        Metadata_UserString,
        Metadata_Blob,
        Metadata_Guid,

        Metadata_ModuleRow,
        Metadata_TypeRefRow,
        Metadata_TypeDefRow,
        Metadata_FieldPtrRow,
        Metadata_FieldRow,
        Metadata_MethodPtrRow,
        Metadata_MethodDefRow,
        Metadata_ParamPtrRow,
        Metadata_ParamRow,
        Metadata_InterfaceImplRow,
        Metadata_MemberRefRow,
        Metadata_ConstantRow,
        Metadata_CustomAttributeRow,
        Metadata_FieldMarshalRow,
        Metadata_DeclSecurityRow,
        Metadata_ClassLayoutRow,
        Metadata_FieldLayoutRow,
        Metadata_StandAloneSigRow,
        Metadata_EventMapRow,
        Metadata_EventPtrRow,
        Metadata_EventRow,
        Metadata_PropertyMapRow,
        Metadata_PropertyPtrRow,
        Metadata_PropertyRow,
        Metadata_MethodSemanticsRow,
        Metadata_MethodImplRow,
        Metadata_ModuleRefRow,
        Metadata_TypeSpecRow,
        Metadata_ImplMapRow,
        Metadata_FieldRvaRow,
        Metadata_EncLogRow,
        Metadata_EncMapRow,
        Metadata_AssemblyRow,
        Metadata_AssemblyProcessorRow,
        Metadata_AssemblyOSRow,
        Metadata_AssemblyRefRow,
        Metadata_AssemblyRefProcessorRow,
        Metadata_AssemblyRefOSRow,
        Metadata_FileRow,
        Metadata_ExportedTypeRow,
        Metadata_ManifestResourceRow,
        Metadata_NestedClassRow,
        Metadata_GenericParamRow,
        Metadata_MethodSpecRow,
        Metadata_GenericParamConstraintRow,

        PortablePdb_DocumentRow,
        PortablePdb_MethodDebugInformationRow,
        PortablePdb_LocalScopeRow,
        PortablePdb_LocalVariableRow,
        PortablePdb_LocalConstantRow,
        PortablePdb_ImportScopeRow,
        PortablePdb_StateMachineMethodRow,
        PortablePdb_CustomDebugInformationRow,

        #endregion
        #endregion
        #region CLR

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RuntimeInfo"/>
        /// </summary>
        RuntimeInfo,
        ModuleIndex,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ClrEngineMetrics"/>
        /// </summary>
        ClrEngineMetrics,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorVTableFixup"/>
        /// </summary>
        ImageCorVTableFixup,

        #region NGEN

        CorCompileHeader,

        NgenHelperEntry,

        CorCompileImportSection,

        CorCompileImportTableEntry,

        CorCompileVersionInfo,

        CorCompileDepepdency,

        CorCompileCodeManagerEntry,

        CorCompileVirtualSectionInfo,

        #endregion
        #region R2R

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ReadyToRunHeader"/>
        /// </summary>
        ReadyToRunHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ReadyToRunCoreHeader"/>
        /// </summary>
        ReadyToRunCoreHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ReadyToRunSection"/>
        /// </summary>
        ReadyToRunSection,

        ReadyToRunSection_CompilerIdentifier,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ReadyToRunImportSection"/>
        /// </summary>
        ReadyToRunImportSection,

        #endregion

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.AppHostSignature"/>
        /// </summary>
        AppHostSignature,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Bundle.Manifest"/>
        /// </summary>
        BundleManifest,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Bundle.HeaderFixed"/>
        /// </summary>
        BundleHeaderFixed,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Bundle.HeaderFixedV2"/>
        /// </summary>
        BundleHeaderFixedV2,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Bundle.FileEntry"/>
        /// </summary>
        BundleFileEntry,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Bundle.FileEntryFixed"/>
        /// </summary>
        BundleFileEntryFixed,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Bundle.Location"/>
        /// </summary>
        BundleLocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.BundleEncodedString"/>
        /// </summary>
        BundleEncodedString,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="FixedUtf8String"/>.
        /// </summary>
        DepsJson,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="FixedUtf8String"/>.
        /// </summary>
        RuntimeConfigJson,

        //Native AOT
        DotNetRuntimeDebugHeader,
        DebugTypeEntries,
        GlobalValueEntries,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.DebugTypeEntry"/>
        /// </summary>
        DebugTypeEntry,

        DebugTypeEntry_TypeName,
        DebugTypeEntry_FieldName,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.GlobalValueEntry"/>
        /// </summary>
        GlobalValueEntry,

        GlobalValueEntry_Name,

        #endregion
        #region RTTI

        //TypeDescriptor is covered under exception data


        RTTIBaseClassDescriptor,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RTTIBaseClassArray"/>
        /// </summary>
        RTTIBaseClassArray,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RTTIClassHierarchyDescriptor"/>
        /// </summary>
        RTTIClassHierarchyDescriptor,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RTTICompleteObjectLocator"/>
        /// </summary>
        RTTICompleteObjectLocator,

        PMD,

        #endregion

        //NE
        NEFile,
        NE_ImportedName_Length,
        NE_ImportedName_String,
        NE_ModuleReference,
        ImageOS2Header,
        NewSeg,
        NE_SegmentTable,
        NE_ResourceTable,
        NE_ResidentNameTable,
        NE_ModuleReferenceTable,
        NE_ImportedNamesTable,
        NE_EntryTable,
        NE_NonResidentNameTable,

        //LE
        LEFile,
        ImageVXDHeader,

        LE_ObjectTable,
        LE_ObjectPageMap,
        LE_ResourceTable,
        LE_ResidentNameTable,
        LE_EntryTable,
        LE_ModuleDirectiveTable,
        LE_PerPageChecksum,
        LE_FixupPageTable,
        LE_FixupRecordTable,
        LE_ImportModuleNameTable,
        LE_EnumeratedDataPages,
        LE_IteratedDataMap,
        LE_NonResidentNamesTable,
        LE_DebugInfo,

        //PDB

        PDBFile,

        PN,
        Page,

        //MSF

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.MsfHdr"/>.
        /// </summary>
        MsfHdr,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.BigMsfHdr"/>.
        /// </summary>
        BigMsfHdr,

        SI_PERSIST,

        /// <summary>
        /// An <see cref="IStructView"/> that represents an <see cref="PESpy.PDB.IStreamTable"/>.
        /// </summary>
        StreamTable,

        SI,

        //snPDB

        /// <summary>
        /// An <see cref="IStructView"/> that represents an <see cref="PESpy.PDB.PDBStream"/>.
        /// </summary>
        PDBStream,

        /// <summary>
        /// An <see cref="IStructView"/> that represents an <see cref="PESpy.PDB.PDBStream70"/>.
        /// </summary>
        PDBStream70,

        /// <summary>
        /// An <see cref="IStructView"/> that represents an <see cref="PESpy.PDB.NMTNI"/>.
        /// </summary>
        StreamNameTable,

        Map,
        Map_Entry,

        //snTpi

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.HDR"/>.
        /// </summary>
        Hdr,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.HDR_16t"/>.
        /// </summary>
        Hdr_16t,
        TpiHash,
        OffCb,

        //snDbi

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.DBIHdr"/>.
        /// </summary>
        DbiHdr,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.NewDBIHdr"/>.
        /// </summary>
        NewDbiHdr,
        Modi,
        Modi50,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.Modi60"/>.
        /// </summary>
        Modi60Persist,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.ECInfo"/>.
        /// </summary>
        ECInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SC20"/>.
        /// </summary>
        SC20,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SC40"/>.
        /// </summary>
        SC40,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SC"/>.
        /// </summary>
        SC,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SC2"/>.
        /// </summary>
        SC2,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SectionContribsV40"/>.
        /// </summary>
        SectionContribsV40,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SectionContribsV60"/>.
        /// </summary>
        SectionContribsV60,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFSegMap"/>.
        /// </summary>
        OMFSegMap,

        OMFSegMapDesc,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFFileIndex"/>.
        /// </summary>
        OMFFileIndex,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.NMT"/>.
        /// </summary>
        NameTable,

        VHdr,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.DbgDataHdr"/>.
        /// </summary>
        DbgDataHdr,

        TypType,

        NumericData,
        NumericLeafKind, //lfFieldList padding, numeric data
        NumericValue,
        NumericStringLength,

        PdbFeature,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="CV_SIGNATURE"/>.
        /// </summary>
        CvSignature,

        HRFile,
        HashBucketsBitmap,
        HashBuckets,

        CvDebugSSubsectionHeader,
        CvFileCheckSum,
        RvaAndFrameData,
        FrameData,
        CvDebugSLinesHeader,
        CvDebugSLinesFileBlockHeader,
        CvLine,
        InlineeSigAndLines,
        InlineeSourceLine,
        InlineeSourceLineEx,
        FuncMDTokenMap,
        TypeMDTokenMap,
        FuncMDTokenMap_Entry,
        FuncMDTokenMap_MethodData,
        TypeMDTokenMap_Entry,
        TypeMDTokenMap_TypeData,
        CrossScopeReferences,
        LocalIdAndGlobalIdPair,
        MergedAssemblyInfo,
        PdbIdScope,

        SrcHeaderOut,

        #region Symbols

        SymType,

        AlignSym,
        AnnotationSym,
        ArmSwitchTable,
        AttrManyRegSym2,
        AttrRegRel,
        AttrRegSym,
        AttrSlotSym,
        BlockSym16,
        BlockSym32,
        BPRelSym16,
        BPRelSym32,
        BPRelSym3216t,
        BuildInfoSym,
        CallSiteInfo,
        CExMSym16,
        CExMSym32,
        CFlagSym,
        CoffGroupSym,
        CompileSym,
        CompileSym3,
        ConstSym,
        ConstSym16t,
        DataSym16,
        DataSym32,
        DataSym3216t,
        DataSymHLSL,
        DataSymHLSL32,
        DataSymHLSL32Ex,
        DefRangeSym,
        DefRangeSymFramePointerRel,
        DefRangeSymFramePointerRelFullScope,
        DefRangeSymHLSL,
        DefRangeSymRegister,
        DefRangeSymRegisterRel,
        DefRangeSymSubField,
        DefRangeSymSubfieldRegister,
        DiscardedSym,
        DPCSymTagMap,
        EntryThisSym,
        EnvBlockSym,
        ExportSym,
        FileStaticSym,
        FrameCookie,
        FrameProcSym,
        FrameRelSym,
        FunctionList,
        HeapAllocSite,
        InlineSiteSym,
        InlineSiteSym2,
        LabelSym16,
        LabelSym32,
        LocalDPCGroupSharedSym,
        LocalSym,
        ManProcSym,
        ManTypRef,
        ManyRegSym,
        ManyRegSym16t,
        ManyRegSym2,
        ModTypeRef,
        ObjNameSym,
        OemSymbol,
        PdbMap,
        PogoInfo,
        ProcSym16,
        ProcSym32,
        ProcSym3216t,
        ProcSymIA64,
        ProcSymMips,
        ProcSymMips16t,
        PubSym32,
        RefMiniPdb,
        RefSym,
        RefSym2,
        RegRel16,
        RegRel32,
        RegRel3216t,
        RegSym,
        RegSym16t,
        ReturnSym,
        SearchSym,
        SectionSym,
        SepCodeSym,
        SLink32,
        SlotSym32,
        ThunkSym16,
        ThunkSym32,
        TrampolineSym,
        UdtSym,
        UdtSym16t,
        UNameSpace,

        #endregion
        #region Types

        LfAlias,
        LfArgList,
        LfArgList16t,
        LfArray,
        LfArray16t,
        LfBArray,
        LfBArray16t,
        LfBClass,
        LfBClass16t,
        LfBitfield,
        LfBitfield16t,
        LfBuildInfo,
        LfChar,
        LfClass,
        LfClass16t,
        LfCmplx128,
        LfCmplx32,
        LfCmplx64,
        LfCmplx80,
        LfCobol0,
        LfCobol016t,
        LfCobol1,
        LfDefArg,
        LfDefArg16t,
        LfDerived,
        LfDerived16t,
        LfDimArray,
        LfDimArray16t,
        LfDimCon,
        LfDimCon16t,
        LfDimVar,
        LfDimVar16t,
        LfEasy,
        LfEndPreComp,
        LfEnum,
        LfEnum16t,
        LfEnumerate,
        LfFieldList,
        LfFieldList16t,
        LfFriendCls,
        LfFriendCls16t,
        LfFriendFcn,
        LfFriendFcn16t,
        LfFuncId,
        LfHLSL,
        LfIndex,
        LfIndex16t,
        LfLabel,
        LfList,
        LfLong,
        LfManaged,
        LfMatrix,
        LfMember,
        LfMember16t,
        LfMemberModify,
        LfMethod,
        LfMethod16t,
        LfMethodList,
        LfMethodList16t,
        LfMFunc,
        LfMFunc16t,
        LfMFuncId,
        LfModifier,
        LfModifier16t,
        LfModifierEx,
        LfNestType,
        LfNestType16t,
        LfNestTypeEx,
        LfOct,
        LfOEM,
        LfOEM16t,
        LfOEM2,
        LfOneMethod,
        LfOneMethod16t,
        LfPad,
        LfPointer,
        LfPointer16t,
        LfPreComp,
        LfPreComp16t,
        LfProc,
        LfProc16t,
        LfQuad,
        LfReal128,
        LfReal16,
        LfReal32,
        LfReal48,
        LfReal64,
        LfReal80,
        LfRefSym,
        LfShort,
        LfSkip,
        LfSkip16t,
        LfSTMember,
        LfSTMember16t,
        LfStridedArray,
        LfStringId,
        LfTypeServer,
        LfTypeServer2,
        LfUdtModSrcLine,
        LfUdtSrcLine,
        LfULong,
        LfUnion,
        LfUnion16t,
        LfUOct,
        LfUQuad,
        LfUShort,
        LfVarString,
        LfVBClass,
        LfVBClass16t,
        LfVector,
        LfVftable,
        LfVFTPath,
        LfVFTPath16t,
        LfVFuncOff,
        LfVFuncOff16t,
        LfVFuncTab,
        LfVFuncTab16t,
        LfVTShape,

        MlMethod,
        MlMethod16t,

        NumericData,

        #endregion

        //Globals
        GSIHashHdr,

        //Publics

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.PSGSIHDR"/>.
        /// </summary>
        PSGSIHDR,

        //DBG

        DBGFile,
        ImageSeparateDebugHeader,
        ExportedNames,
        ExportedNames_Entry,

        //OBJ

        OBJFile,
        InterSectionData,
        Relocations,

        drectve,
        text,
        text_mn,
        data,
        idata,
        edata,
        rdata,
        debug_f, //FPO
        bss,
        rsrc,
        sxdata,
        UnknownSection,

        //LIB

        LIBFile,

        LIBFile_Signature,

        ImageArchiveMemberHeader,
        FirstLinkerMember,
        SecondLinkerMember,
        LongNamesMember,
        LongImportLibraryMember,
        ShortImportLibraryMember,
        ShortImportLibrary_DllName,
        ShortImportLibrary_ImportName,
        ImportObjectHeader,

        //OMF
        NB05Data,
        OMFDirHeader,
        OMFDirEntry,
        CodeViewSig,
        OMFModule,
        OMFSegDesc,
        OMFSymHash,

        //NB02
        dnt,
        nsg,
        nsg32,
        pbi,
        pbi32,
        smd,
        smd32,
        loe,
        loe32,
        LineNumberOffset,
        LineNumberOffset32,
        LibraryName,
        OldSymType,
        OldTypType,

        DNRBModule,
        DNRB_Publics,
        DNRB_Types,
        DNRB_Symbols,
        DNRB_SourceLines,

        //DOS
        DOSFile
    }
}
