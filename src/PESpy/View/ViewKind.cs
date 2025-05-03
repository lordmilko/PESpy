using System.ComponentModel;

namespace PESpy.View
{
    /// <summary>
    /// Specifies the kind of value contained in a view.
    /// </summary>
    public enum ViewKind
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
        /// A named <see cref="IFieldView"/> contained in a <see cref="StructView"/>.
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

        Assembly,

        DataDirectory,

        #region Headers

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageDosHeader"/>.
        /// </summary>
        ImageDosHeader,

        DosStub,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.RichHeader"/>.
        /// </summary>
        RichHeader,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ProdItem"/>.
        /// </summary>
        ProdItem,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageNtHeaders"/>.
        /// </summary>
        ImageNtHeaders,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageFileHeader"/>.
        /// </summary>
        ImageFileHeader,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageOptionalHeader"/>.
        /// </summary>
        ImageOptionalHeader,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageDataDirectory"/>.
        /// </summary>
        ImageDataDirectory,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageSectionHeader"/>.
        /// </summary>
        ImageSectionHeader,

        ImageRelocation,

        CoffSymbolTable,

        ImageSymbol,

        ImageAuxSymbol,

        AnonObjectHeader,

        #endregion
        #region Exports Table (0)

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageExportDirectory"/>.
        /// </summary>
        ImageExportDirectory,

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
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageImportDescriptor"/>.
        /// </summary>
        ImageImportDescriptor,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageThunkData"/>.
        /// </summary>
        ImageThunkData,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageImportByName"/>.
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
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageResourceDirectory"/>.
        /// </summary>
        ImageResourceDirectory,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageResourceDirectoryEntry"/>.
        /// </summary>
        ImageResourceDirectoryEntry,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageResourceDataEntry"/>.
        /// </summary>
        ImageResourceDataEntry,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.ImageResourceDirStringU"/>.
        /// </summary>
        ImageResourceDirStringU,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.VsVersionInfo"/>.
        /// </summary>
        VsVersionInfo,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.VsFixedFileInfo"/>.
        /// </summary>
        VsFixedFileInfo,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="VsVersionInfo.StringFileInfo"/>.
        /// </summary>
        StringFileInfo,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="VsVersionInfo.StringTable"/>.
        /// </summary>
        StringTable,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="VsVersionInfo.String"/> contained in a <see cref="VsVersionInfo.StringTable"/>.
        /// </summary>
        StringTable_String,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="VsVersionInfo.VarFileInfo"/>.
        /// </summary>
        VarFileInfo,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="VsVersionInfo.Var"/> contained in a <see cref="VsVersionInfo.VarFileInfo"/>.
        /// </summary>
        VarFileInfo_Var,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="ClrDebugResource"/>.
        /// </summary>
        ClrDebugResource,

        #endregion
        #region Exception Table (3)

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.RuntimeFunction"/>.
        /// </summary>
        RuntimeFunction,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.UnwindInfo"/>.
        /// </summary>
        UnwindInfo,

        /// <summary>
        /// A <see cref="StructView"/> that represents a <see cref="PESpy.UnwindCode"/>.
        /// </summary>
        UnwindCode,

        ScopeTable,
        ScopeRecord,
        FuncInfo,
        FuncInfoV1,
        FuncInfo4,
        FuncInfoHeader,
        HandlerType,
        IptoStateMapEntry,
        TryBlockMapEntry,
        TypeDescriptor,
        UnwindMapEntry,

        #endregion
        #region Security Table (4)

        WinCertificate,
        SignedData,

        #endregion
        #region Base Relocation Table (5)

        ImageBaseRelocation,
        BaseRelocationEntry,

        #endregion
        #region Debug Table (6)

        ImageDebugDirectory,

        NB10I,
        RSDSI,
        FpoData,
        ImageDebugMisc,
        ImageCoffSymbolsHeader,
        Omap,
        VCFeature,
        PogoData,
        PogoItem,
        Reproducible,
        EmbeddedPortablePdb,
        PdbChecksum,
        ExDllCharacteristics,

        #endregion
        #region Copyright Table (7)

        #endregion
        #region Global Pointer Table (8)

        #endregion
        #region Thread Local Storage Table (9)

        ImageTlsDirectory,

        #endregion
        #region Load Config Table (10)

        ImageLoadConfigDirectory,
        ImageLoadConfigCodeIntegrity,

        ImageEnclaveConfig,
        ImageEnclaveImport,

        GuardAddressTakenIatEntryTable,
        GuardCFFunctionTable,
        GuardEHContinuationTable,
        GuardLongJumpTargetTable,

        GuardAddressTakenIatEntryTable_Entry,
        GuardCFFunctionTable_Entry,
        GuardEHContinuationTable_Entry,
        GuardLongJumpTargetTable_Entry,

        XFG,

        LockPrefixTable,
        SecurityCookie,
        SEHandlerTable,
        GuardCFCheckFunctionPointer,
        GuardCFDispatchFunctionPointer,
        GuardRFFailureRoutineFunctionPointer,
        GuardRFVerifyStackPointerFunctionPointer,
        GuardXFGCheckFunctionPointer,
        GuardXFGDispatchFunctionPointer,
        GuardXFGTableDispatchFunctionPointer,
        GuardMemcpyFunctionPointer,

        ImageDynamicRelocationTable,
        ImageDynamicRelocation,
        ImageDynamicRelocationV2,
        ImageFunctionOverrideHeader,
        ImagePrologueDynamicRelocationHeader,
        ImageEpilogueDynamicRelocationHeader,
        ImageImportControlTransferDynamicRelocation,
        ImageIndirControlTransferDynamicRelocation,
        ImageSwitchTableBranchDynamicRelocation,
        ImageFunctionOverrideDynamicRelocation,
        ImageBDDInfo,
        ImageBDDDynamicRelocation,

        #endregion
        #region Bound Import Table (11)

        ImageBoundImportDescriptor,
        ImageBoundForwarderRef,

        #endregion
        #region Import Address Table (12)

        #endregion
        #region Delay Import Table (13)

        ImageDelayLoadDescriptor,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates the <see cref="ImageDelayLoadDescriptor.ImportNameTableRVA"/> region.
        /// </summary>
        [Description("Delay Import Lookup Table (Thunks)")]
        DelayImportLookupTable,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates the <see cref="ImageDelayLoadDescriptor.ImportAddressTableRVA"/> region.
        /// </summary>
        DelayImportAddressTable,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates the <see cref="ImageImportByName"/> structures (and the padding between them)
        /// pointed to by the <see cref="ImageThunkData"/> structures of the <see cref="DelayImportLookupTable"/> in <see cref="ImageDelayLoadDescriptor.ImportNameTableRVA"/>.
        /// </summary>
        [Description("Delay Import Strings")]
        DelayImportStrings,

        #endregion
        #region CorHeader Directory (14)

        ImageCor20Header,

        ImageCorILMethodTiny,
        ImageCorILMethodFat,

        ImageCorILMethodSectEH,
        ImageCorILMethodSect,
        ImageCorILMethodSectEHClause,

        StorageSignature,
        StorageHeader,
        StorageStream,

        CompressedModelHeap,
        StringPoolHeap,
        USBlobPoolHeap,
        BlobPoolHeap,
        GuidPoolHeap,

        Metadata_String,
        Metadata_UserString,
        Metadata_Blob,
        Metadata_Guid,

        MetadataHeader,
        MetadataTable,

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
        #region CLR

        RuntimeInfo,
        ModuleIndex,

        //Native AOT
        DotNetRuntimeDebugHeader,
        DebugTypeEntries,
        GlobalValueEntries,
        DebugTypeEntry,
        GlobalValueEntry,

        #endregion

        //PDB

        PDBFile,

        Page,

        //MSF
        MsfHdr,
        BigMsfHdr,
        SI_PERSIST,
        StreamTable,
        SI,

        //snPDB
        PDBStream70,
        StreamNameTable,

        //snTpi
        Hdr,
        Hdr_16t,
        TpiHash,
        OffCb,

        //snDbi
        DbiHdr,
        NewDbiHdr,
        Modi,
        Modi60Persist,
        ECInfo,
        SC,
        SectionContribsV40,
        SectionContribsV60,
        OMFSegMap,
        OMFSegMapDesc,
        FileInfo,
        NameTable,
        VHdr,
        DbgDataHdr,
        SymType,
        TypType,

        CvDebugSSubsectionHeader,
        CvFileCheckSum,
        RvaAndFrameData,
        FrameData,
        CvDebugSLinesHeader,
        CvDebugSLinesFileBlockHeader,
        CvLine,

        //DBG

        DBGFile,
        ImageSeparateDebugHeader,
        ExportedNames,

        //OBJ

        OBJFile,

        //LIB

        LIBFile,

        ImageArchiveMemberHeader,
        FirstLinkerMember,
        LongImportLibraryMember,
        ShortImportLibraryMember,
        ImportObjectHeader
    }
}
