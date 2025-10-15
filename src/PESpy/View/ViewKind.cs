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
        /// A <see cref="ByteBlob"/> whose bytes are all <see langword="0xCC" />, indicating
        /// that the bytes are merely used for padding.
        /// </summary>
        CC,

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

        Assembly,

        DataDirectory,

        #region Headers

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDosHeader"/>.
        /// </summary>
        ImageDosHeader,

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

        ImageRelocation,

        CoffSymbolTable,

        ImageSymbol,

        ImageAuxSymbol,

        ImageLineNumber,

        AnonObjectHeader,

        #endregion
        #region Exports Table (0)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageExportDirectory"/>.
        /// </summary>
        ImageExportDirectory,

        ImageExportDirectory_Name,

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

        //Either the ImageImportDescriptor.Name, or the ImageEnclaveImport.ImportName
        ImportName,

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
        /// An <see cref="IStructView"/> that represents a <see cref="ClrDebugResource"/>.
        /// </summary>
        ClrDebugResource,

        MessageResourceData,

        MessageResourceBlock,

        MessageResourceEntry,

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

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.UnwindCode"/>.
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
        XFixupData,
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
        ImageEnclaveImport_ImportName,

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
        ImageBoundImportName,
        ImageBoundForwarderRef,

        #endregion
        #region Import Address Table (12)

        #endregion
        #region Delay Import Table (13)

        ImageDelayLoadDescriptor,

        ImageDelayLoadDescriptor_DllNameRVA,

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
        ClrEngineMetrics,

        ImageCorVTableFixup,

        ReadyToRunHeader,
        ReadyToRunCoreHeader,
        ReadyToRunSection,
        ReadyToRunImportSection,

        AppHostSignature,
        BundleManifest,
        BundleHeaderFixed,
        BundleHeaderFixedV2,
        BundleFileEntry,
        BundleFileEntryFixed,
        BundleLocation,
        BundleEncodedString,

        //Native AOT
        DotNetRuntimeDebugHeader,
        DebugTypeEntries,
        GlobalValueEntries,
        DebugTypeEntry,
        DebugTypeEntry_TypeName,
        DebugTypeEntry_FieldName,
        GlobalValueEntry,
        GlobalValueEntry_Name,

        #endregion

        //NE
        NEFile,
        NE_ImportedName_Length,
        NE_ImportedName_String,
        NE_ModuleReference,
        ImageOS2Header,
        NewSeg,
        NonResidentNameTable,

        //LE
        LEFile,
        ImageVXDHeader,

        //PDB

        PDBFile,

        PN,
        Page,

        //MSF
        MsfHdr,
        BigMsfHdr,
        SI_PERSIST,
        StreamTable,
        SI,

        //snPDB
        PDBStream,
        PDBStream70,
        StreamNameTable,
        Map,

        //snTpi
        Hdr,
        Hdr_16t,
        TpiHash,
        OffCb,

        //snDbi
        DbiHdr,
        NewDbiHdr,
        Modi,
        Modi50,
        Modi60Persist,
        ECInfo,
        SC40,
        SC,
        SC2,
        SectionContribsV40,
        SectionContribsV60,
        OMFSegMap,
        OMFSegMapDesc,
        OMFFileIndex,
        NameTable,
        VHdr,
        DbgDataHdr,
        SymType,
        TypType,

        PdbFeature,
        CvSignature,
        HRFile,
        HashBuckets,

        CvDebugSSubsectionHeader,
        CvFileCheckSum,
        RvaAndFrameData,
        FrameData,
        CvDebugSLinesHeader,
        CvDebugSLinesFileBlockHeader,
        CvLine,

        #region Symbols

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

        #endregion

        //Globals
        GSIHashHdr,
        
        //Publics
        PSGSIHDR,

        //DBG

        DBGFile,
        ImageSeparateDebugHeader,
        ExportedNames,

        //OBJ

        OBJFile,
        InterSectionData,
        Relocations,

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
        smd,
        loe,
        LineNumberOffset,

        DNRBModule,

        //DOS
        DOSFile
    }
}
