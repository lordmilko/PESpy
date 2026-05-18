using System;
using System.ComponentModel;
using ClrDebug.PDB;
using PESpy.PDB;
using Str = PESpy.Strings;

namespace PESpy.View
{
    public enum FieldViewFlags : ushort
    {
        //Specifies that this value is an address and that numbers should exclusively be displayed as hex
        Address = 1,

        //Specifies that the hexadecimal number represents a string, e.g. MZ, PE00, etc
        HexString = 2,

        //Specifies that the value represents a size amount in bytes
        Size = 4
    }

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
        /// A <see cref="ByteBlobView"/> containing at least one non-<see langword="0"/> byte, indicating
        /// that the bytes represent code or data whose meaning could not be automatically determined.
        /// </summary>
        Data,

        /// <summary>
        /// A <see cref="ByteBlobView"/> whose bytes are all <see langword="0" />, indicating
        /// that the bytes are merely used for padding.
        /// </summary>
        Padding,

        /// <summary>
        /// A <see cref="ByteBlobView"/> whose bytes are all <see langword="0xCC" />, indicating
        /// that the bytes are merely used for padding.
        /// </summary>
        CC,

        FF,

        NOP,

        MultiByteNOP,

        /// <summary>
        /// A <see cref="ByteBlobView"/> whose bytes are all <see langword="0x0a" />, indicating
        /// that the bytes are merely used for padding within a <see cref="PESpy.LIBFile"/>.
        /// </summary>
        ImageArchivePad,

        /// <summary>
        /// A named <see cref="IFieldView"/> contained in an <see cref="IStructView"/>.
        /// </summary>
        Field,

        BitField,

        Vftable,

        /// <summary>
        /// A jump table that has been discovered through data flow analysis
        /// </summary>
        JumpTable,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates one or more strings.
        /// </summary>
        Strings,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="PESpy.AnsiString"/>.
        /// </summary>
        AnsiString,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="PESpy.Utf8String"/>.
        /// </summary>
        Utf8String,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="PESpy.Utf16String"/>.
        /// </summary>
        Utf16String,
        SymStringLengthPrefixed,
        SymStringUtf8,
        StringLength, //For length prefixed string

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="System.Guid"/>.
        /// </summary>
        Guid,

        //Unknown decimal value
        Decimal,

        Assembly,
        IL,

        DataDirectory,
        ILMethods,
        UnwindInfos,
        Symbols,
        Types,
        Thunks,
        Functions,

        #region Headers

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDosHeader"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_DOS_HEADER))]
        ImageDosHeader,

        /// <summary>
        /// A <see cref="ByteBlobView"/> that contains the bytes of the DOS Stub.
        /// </summary>
        [Description(nameof(Str.DosStub))]
        DosStub,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RichHeader"/>.
        /// </summary>
        [Description(nameof(Str.RichHeader))]
        RichHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ProdItem"/>.
        /// </summary>
        [Description(nameof(Str.PRODITEM))]
        ProdItem,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageNtHeaders"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_NT_HEADERS))]
        ImageNtHeaders,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageFileHeader"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_FILE_HEADER))]
        ImageFileHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageOptionalHeader"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_OPTIONAL_HEADER))]
        ImageOptionalHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDataDirectory"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_DATA_DIRECTORY))]
        ImageDataDirectory,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageSectionHeader"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_SECTION_HEADER))]
        ImageSectionHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageRelocation"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_RELOCATION))]
        ImageRelocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.CoffSymbolTable"/>.
        /// </summary>
        [Description(nameof(Str.CoffSymbolTable))]
        CoffSymbolTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageSymbol"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_SYMBOL))]
        ImageSymbol,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageAuxSymbol"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_AUX_SYMBOL))]
        ImageAuxSymbol,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageLineNumber"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_LINENUMBER))]
        ImageLineNumber,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.AnonObjectHeader"/>.
        /// </summary>
        [Description(nameof(Str.ANON_OBJECT_HEADER))]
        AnonObjectHeader,

        #endregion
        #region Exports Table (0)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageExportDirectory"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_EXPORT_DIRECTORY))]
        ImageExportDirectory,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="PESpy.AnsiString"/>.
        /// </summary>
        ImageExportDirectory_Name,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="PESpy.AnsiString"/>.
        /// </summary>
        ImageExportDirectory_ForwarderName,

        ImageExportDirectory_AddressOfNameOrdinals_Entry,
        ImageExportDirectory_AddressOfNames_Entry,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="PESpy.AnsiString"/>.
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
        [Description(nameof(Str.IMAGE_IMPORT_DESCRIPTOR))]
        ImageImportDescriptor,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="PESpy.AnsiString"/>.
        /// </summary>
        ImageImportDescriptor_Name,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="PESpy.AnsiString"/>.
        /// </summary>
        ImageEnclaveImport_ImportName,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageThunkData"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_THUNK_DATA))]
        ImageThunkData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageImportByName"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_IMPORT_BY_NAME))]
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
        /// pointed to by the <see cref="ImageThunkData"/> structures of the <see cref="ImportLookupTable"/> in <see cref="ImageImportDescriptor.OriginalFirstThunk"/>
        /// or the <see cref="DelayImportLookupTable"/> in <see cref="ImageDelayLoadDescriptor.ImportNameTableRVA"/>
        /// </summary>
        [Description("Import Strings")]
        ImportStrings,

        #endregion
        #region Resource Directory (2)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageResourceDirectory"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_RESOURCE_DIRECTORY))]
        ImageResourceDirectory,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageResourceDirectoryEntry"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_RESOURCE_DIRECTORY_ENTRY))]
        ImageResourceDirectoryEntry,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageResourceDataEntry"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_RESOURCE_DATA_ENTRY))]
        ImageResourceDataEntry,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageResourceDirStringU"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_RESOURCE_DIR_STRING_U))]
        ImageResourceDirStringU,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VsVersionInfo"/>.
        /// </summary>
        [Description(nameof(Str.VS_VERSIONINFO))]
        VsVersionInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VsFixedFileInfo"/>.
        /// </summary>
        [Description(nameof(Str.VS_FIXEDFILEINFO))]
        VsFixedFileInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VsVersionInfo.StringFileInfo"/>.
        /// </summary>
        [Description(nameof(Str.StringFileInfo))]
        StringFileInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="VsVersionInfo.StringTable"/>.
        /// </summary>
        [Description(nameof(Str.StringTable))]
        StringTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="VsVersionInfo.String"/> contained in a <see cref="VsVersionInfo.StringTable"/>.
        /// </summary>
        [Description(nameof(Str.String))]
        StringTable_String,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VsVersionInfo.VarFileInfo"/>.
        /// </summary>
        [Description(nameof(Str.VarFileInfo))]
        VarFileInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VsVersionInfo.Var"/> contained in a <see cref="PESpy.VsVersionInfo.VarFileInfo"/>.
        /// </summary>
        [Description(nameof(Str.Var))]
        VarFileInfo_Var,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ClrDebugResource"/>.
        /// </summary>
        [Description(nameof(Str.CLR_DEBUG_RESOURCE))]
        ClrDebugResource,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.MessageResourceData"/>.
        /// </summary>
        [Description(nameof(Str.MESSAGE_RESOURCE_DATA))]
        MessageResourceData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.MessageResourceBlock"/>.
        /// </summary>
        [Description(nameof(Str.MESSAGE_RESOURCE_BLOCK))]
        MessageResourceBlock,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.MessageResourceEntry"/>.
        /// </summary>
        [Description(nameof(Str.MESSAGE_RESOURCE_ENTRY))]
        MessageResourceEntry,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="FixedUtf8String"/>.
        /// </summary>
        Manifest,

        /// <summary>
        /// A <see cref="ByteBlobView"/> containing the bytes of a resource not currently supported by PESpy.
        /// </summary>
        UnknownResource,

        #endregion
        #region Exception Table (3)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RuntimeFunction"/>.
        /// </summary>
        [Description(nameof(Str.RUNTIME_FUNCTION))]
        RuntimeFunction,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.UnwindInfo"/>.
        /// </summary>
        [Description(nameof(Str.UNWIND_INFO))]
        UnwindInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.UnwindCode"/>.
        /// </summary>
        [Description(nameof(Str.UNWIND_CODE))]
        UnwindCode,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.GsHandlerData"/>.
        /// </summary>
        [Description(nameof(Str._GS_HANDLER_DATA))]
        GsHandlerData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ScopeTable"/>.
        /// </summary>
        [Description(nameof(Str.SCOPE_TABLE))]
        ScopeTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="ScopeTable.ScopeRecord"/>.
        /// </summary>
        [Description(nameof(Str.ScopeRecord))]
        ScopeRecord,

        #region FuncInfo

        FuncInfoRva,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.FuncInfo"/>.
        /// </summary>
        [Description(nameof(Str.FuncInfo))]
        FuncInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.HandlerType"/>.
        /// </summary>
        [Description(nameof(Str.HandlerType))]
        HandlerType,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.IptoStateMapEntry"/>.
        /// </summary>
        [Description(nameof(Str.IptoStateMapEntry))]
        IptoStateMapEntry,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.TryBlockMapEntry"/>.
        /// </summary>
        [Description(nameof(Str.TryBlockMapEntry))]
        TryBlockMapEntry,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.TypeDescriptor"/>.
        /// </summary>
        [Description(nameof(Str.TypeDescriptor))]
        TypeDescriptor,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.UnwindMapEntry"/>.
        /// </summary>
        [Description(nameof(Str.UnwindMapEntry))]
        UnwindMapEntry,

        #endregion
        #region FuncInfo4

        FuncInfo4Rva,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.FuncInfo4"/>.
        /// </summary>
        [Description(nameof(Str.FuncInfo4))]
        FuncInfo4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.FuncInfoHeader"/>.
        /// </summary>
        [Description(nameof(Str.FuncInfoHeader))]
        FuncInfoHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.HandlerMap4"/>.
        /// </summary>
        [Description(nameof(Str.HandlerMap4))]
        HandlerMap4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.IPtoStateMap4"/>.
        /// </summary>
        [Description(nameof(Str.IPtoStateMap4))]
        IPtoStateMap4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.SepIPtoStateMap4"/>.
        /// </summary>
        [Description(nameof(Str.SepIPtoStateMap4))]
        SepIPtoStateMap4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.TryBlockMap4"/>.
        /// </summary>
        [Description(nameof(Str.TryBlockMap4))]
        TryBlockMap4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.UWMap4"/>.
        /// </summary>
        [Description(nameof(Str.UWMap4))]
        UWMap4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.HandlerType4"/>.
        /// </summary>
        [Description(nameof(Str.HandlerType4))]
        HandlerType4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.HandlerTypeHeader"/>.
        /// </summary>
        [Description(nameof(Str.HandlerTypeHeader))]
        HandlerTypeHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.IPtoStateMapEntry4"/>.
        /// </summary>
        [Description(nameof(Str.IPtoStateMapEntry4))]
        IPtoStateMapEntry4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.SepIPtoStateMapEntry4"/>.
        /// </summary>
        [Description(nameof(Str.SepIPtoStateMapEntry4))]
        SepIPtoStateMapEntry4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.TryBlockMapEntry4"/>.
        /// </summary>
        [Description(nameof(Str.TryBlockMapEntry4))]
        TryBlockMapEntry4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.UnwindMapEntry4"/>.
        /// </summary>
        [Description(nameof(Str.UnwindMapEntry4))]
        UnwindMapEntry4,

        #endregion
        #endregion

        //Do not insert any new entries in here; MarkUnwindInfoRegions relies on WinCertificate being the last entry after
        //all possible data items that could be present

        #region Security Table (4)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.WinCertificate"/>.
        /// </summary>
        [Description(nameof(Str.WIN_CERTIFICATE))]
        WinCertificate,

        [Description(nameof(Str.SignedData))]
        SignedData,

        CertificateBytes,

        #endregion
        #region Base Relocation Table (5)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageBaseRelocation"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_BASE_RELOCATION))]
        ImageBaseRelocation,

        BaseRelocationEntry,

        #endregion
        #region Debug Table (6)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDebugDirectory"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_DEBUG_DIRECTORY))]
        ImageDebugDirectory,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.NB10I"/>.
        /// </summary>
        [Description(nameof(Str.NB10I))]
        NB10I,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RSDSI"/>.
        /// </summary>
        [Description(nameof(Str.RSDSI))]
        RSDSI,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.FpoData"/>.
        /// </summary>
        [Description(nameof(Str.FPO_DATA))]
        FpoData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.XFixupData"/>.
        /// </summary>
        [Description(nameof(Str.XFIXUP_DATA))]
        XFixupData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDebugMisc"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_DEBUG_MISC))]
        ImageDebugMisc,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCoffSymbolsHeader"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_COFF_SYMBOLS_HEADER))]
        ImageCoffSymbolsHeader,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="OMAP_DATA"/>
        /// </summary>
        [Description(nameof(Str.OMAP_DATA))]
        OmapData,

        /// <summary>
        /// A <see cref="ByteBlobView"/> containing the bytes of the BBT data.
        /// </summary>
        BBT,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VCFeature"/>
        /// </summary>
        [Description(nameof(Str.VCFeature))]
        VCFeature,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PogoData"/>
        /// </summary>
        [Description(nameof(Str.PogoData))]
        PogoData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PogoItem"/>
        /// </summary>
        [Description(nameof(Str.PogoItem))]
        PogoItem,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Reproducible"/>
        /// </summary>
        [Description(nameof(Str.Reproducible))]
        Reproducible,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.EmbeddedPortablePdb"/>
        /// </summary>
        [Description(nameof(Str.EmbeddedPortablePDB))]
        EmbeddedPortablePdb,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PdbChecksum"/>
        /// </summary>
        [Description(nameof(Str.PdbChecksum))]
        PdbChecksum,

        ExDllCharacteristics,

        UnknownDebugData,

        #endregion
        #region Copyright Table (7)

        Copyright,

        #endregion
        #region Global Pointer Table (8)

        #endregion
        #region Thread Local Storage Table (9)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageTlsDirectory"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_TLS_DIRECTORY))]
        ImageTlsDirectory,

        #endregion
        #region Load Config Table (10)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageLoadConfigDirectory"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_LOAD_CONFIG_DIRECTORY))]
        ImageLoadConfigDirectory,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageLoadConfigCodeIntegrity"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_LOAD_CONFIG_CODE_INTEGRITY))]
        ImageLoadConfigCodeIntegrity,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageEnclaveConfig"/><para/>
        /// ___enclave_config
        /// </summary>
        [Description(nameof(Str.IMAGE_ENCLAVE_CONFIG))]
        ImageEnclaveConfig,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageEnclaveImport"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_ENCLAVE_IMPORT))]
        ImageEnclaveImport,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.GuardAddressTakenIatEntryTable"/><para/>
        /// __guard_iat_table
        /// </summary>
        [Description(nameof(Str.__guard_iat_table))]
        GuardAddressTakenIatEntryTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.GuardCFFunctionTable"/>.<para/>
        /// __guard_fids_table
        /// </summary>
        [Description(nameof(Str.__guard_fids_table))]
        GuardCFFunctionTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.GuardEHContinuationTable"/><para/>
        /// __guard_eh_cont_table
        /// </summary>
        [Description(nameof(Str.__guard_eh_cont_table))]
        GuardEHContinuationTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.GuardLongJumpTargetTable"/><para/>
        /// __guard_longjmp_table
        /// </summary>
        [Description(nameof(Str.__guard_longjmp_table))]
        GuardLongJumpTargetTable,

        [Description(nameof(Str.Entry))]
        GuardAddressTakenIatEntryTable_Entry,

        [Description(nameof(Str.GFIDSEntry))]
        GuardCFFunctionTable_Entry,

        [Description(nameof(Str.EHCONTEntry))]
        GuardEHContinuationTable_Entry,

        [Description(nameof(Str.Entry))]
        GuardLongJumpTargetTable_Entry,

        XFG,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="int"/>
        /// </summary>
        LockPrefixTable, //Haven't found a PE FIle with this yet so I don't know what the symbol is called

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.<para/>
        /// __security_cookie
        /// </summary>
        [Description(nameof(Str.__security_cookie))]
        SecurityCookie,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="int"/><para/>
        /// __safe_se_handler_table
        /// </summary>
        [Description(nameof(Str.__safe_se_handler_table))]
        SEHandlerTable,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.<para/>
        /// __guard_check_icall_fptr
        /// </summary>
        [Description(nameof(Str.__guard_check_icall_fptr))]
        GuardCFCheckFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.<para/>
        /// __guard_dispatch_icall_fptr
        /// </summary>
        [Description(nameof(Str.__guard_dispatch_icall_fptr))]
        GuardCFDispatchFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.<para/>
        /// __guard_ss_verify_failure_fptr
        /// </summary>
        [Description(nameof(Str.__guard_ss_verify_failure_fptr))]
        GuardRFFailureRoutineFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.<para/>
        /// __guard_ss_verify_sp_fptr
        /// </summary>
        [Description(nameof(Str.__guard_ss_verify_sp_fptr))]
        GuardRFVerifyStackPointerFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.<para/>
        /// __guard_xfg_check_icall_fptr
        /// </summary>
        [Description(nameof(Str.__guard_xfg_check_icall_fptr))]
        GuardXFGCheckFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.<para/>
        /// __guard_xfg_dispatch_icall_fptr
        /// </summary>
        [Description(nameof(Str.__guard_xfg_dispatch_icall_fptr))]
        GuardXFGDispatchFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.<para/>
        /// __guard_xfg_table_dispatch_icall_fptr
        /// </summary>
        [Description(nameof(Str.__guard_xfg_table_dispatch_icall_fptr))]
        GuardXFGTableDispatchFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.<para/>
        /// __castguard_check_failure_os_handled_fptr
        /// </summary>
        [Description(nameof(Str.__castguard_check_failure_os_handled_fptr))]
        CastGuardOsDeterminedFailureMode,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.<para/>
        /// __guard_memcpy_fptr<para/>
        /// Contains a pointer to the memcpy function. <see cref="ImageLoadConfigDirectory.GuardMemcpyFunctionPointer"/> is therefore
        /// a pointer to a pointer.
        /// </summary>
        [Description(nameof(Str.__guard_memcpy_fptr))]
        GuardMemcpyFunctionPointer,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        UmaFunctionPointers, //Don't know what the symbol name is; haven't found a file with this yet

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDynamicRelocationTable"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_DYNAMIC_RELOCATION_TABLE))]
        ImageDynamicRelocationTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDynamicRelocation"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_DYNAMIC_RELOCATION))]
        ImageDynamicRelocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDynamicRelocationV2"/>
        /// </summary>
        ImageDynamicRelocationV2,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageFunctionOverrideHeader"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_FUNCTION_OVERRIDE_HEADER))]
        ImageFunctionOverrideHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImagePrologueDynamicRelocationHeader"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_PROLOGUE_DYNAMIC_RELOCATION_HEADER))]
        ImagePrologueDynamicRelocationHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageEpilogueDynamicRelocationHeader"/>
        /// </summary>
        ImageEpilogueDynamicRelocationHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageImportControlTransferDynamicRelocation"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_IMPORT_CONTROL_TRANSFER_DYNAMIC_RELOCATION))]
        ImageImportControlTransferDynamicRelocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageIndirControlTransferDynamicRelocation"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_INDIR_CONTROL_TRANSFER_DYNAMIC_RELOCATION))]
        ImageIndirControlTransferDynamicRelocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageSwitchTableBranchDynamicRelocation"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_SWITCHTABLE_BRANCH_DYNAMIC_RELOCATION))]
        ImageSwitchTableBranchDynamicRelocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageFunctionOverrideDynamicRelocation"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_FUNCTION_OVERRIDE_DYNAMIC_RELOCATION))]
        ImageFunctionOverrideDynamicRelocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageBDDInfo"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_BDD_INFO))]
        ImageBDDInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageBDDDynamicRelocation"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_BDD_DYNAMIC_RELOCATION))]
        ImageBDDDynamicRelocation,

        #endregion
        #region Bound Import Table (11)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageBoundImportDescriptor"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_BOUND_IMPORT_DESCRIPTOR))]
        ImageBoundImportDescriptor,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="PESpy.AnsiString"/>.
        /// </summary>
        ImageBoundImportName,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageBoundForwarderRef"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_BOUND_FORWARDER_REF))]
        ImageBoundForwarderRef,

        #endregion

        //Import Address Table (12)

        #region Delay Import Table (13)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageDelayLoadDescriptor"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_DELAYLOAD_DESCRIPTOR))]
        ImageDelayLoadDescriptor,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="PESpy.AnsiString"/>.
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

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that encapsulates the <see cref="ImageDelayLoadDescriptor.UnloadInformationTable"/> region.
        /// </summary>
        DelayUnloadInformationTable,

        #endregion
        #region CorHeader Directory (14)

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCor20Header"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_COR20_HEADER))]
        ImageCor20Header,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorILMethod"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_COR_ILMETHOD_TINY))]
        ImageCorILMethodTiny,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorILMethod"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_COR_ILMETHOD_FAT))]
        ImageCorILMethodFat,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorILMethodSectEH"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_COR_ILMETHOD_SECT_EH_FAT))]
        ImageCorILMethodSectEHFat,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorILMethodSectEH"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_COR_ILMETHOD_SECT_EH_SMALL))]
        ImageCorILMethodSectEHSmall,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorILMethodSect"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_COR_ILMETHOD_SECT_FAT))]
        ImageCorILMethodSectFat,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorILMethodSect"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_COR_ILMETHOD_SECT_SMALL))]
        ImageCorILMethodSectSmall,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorILMethodSectEHClause"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT))]
        ImageCorILMethodSectEHClauseFat,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorILMethodSectEHClause"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL))]
        ImageCorILMethodSectEHClauseSmall,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.StorageSignature"/>
        /// </summary>
        [Description(nameof(Str.STORAGESIGNATURE))]
        StorageSignature,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.StorageHeader"/>
        /// </summary>
        [Description(nameof(Str.STORAGEHEADER))]
        StorageHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.StorageStream"/>
        /// </summary>
        [Description(nameof(Str.STORAGESTREAM))]
        StorageStream,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Ecma335.ModelHeap"/>
        /// </summary>
        ModelHeap,

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
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Ecma335.ModelHeader"/>
        /// </summary>
        [Description(nameof(Str.MetadataHeader))]
        MetadataHeader,

        MetadataTable,

        [Description(nameof(Str.PDBHeap))]
        PdbHeap,

        #region Metadata Rows

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="FixedUtf8String"/>.
        /// </summary>
        Metadata_String,

        [Description(nameof(Str.UserString))]
        Metadata_UserString,

        [Description(nameof(Str.BlobEntry))]
        Metadata_Blob,

        Metadata_Guid,

        [Description(nameof(Str.ModuleRow))]
        Metadata_ModuleRow,

        [Description(nameof(Str.TypeRefRow))]
        Metadata_TypeRefRow,

        [Description(nameof(Str.TypeDefRow))]
        Metadata_TypeDefRow,

        [Description(nameof(Str.FieldPtrRow))]
        Metadata_FieldPtrRow,

        [Description(nameof(Str.FieldRow))]
        Metadata_FieldRow,

        [Description(nameof(Str.MethodPtrRow))]
        Metadata_MethodPtrRow,

        [Description(nameof(Str.MethodDefRow))]
        Metadata_MethodDefRow,

        [Description(nameof(Str.ParamPtrRow))]
        Metadata_ParamPtrRow,

        [Description(nameof(Str.ParamRow))]
        Metadata_ParamRow,

        [Description(nameof(Str.InterfaceImplRow))]
        Metadata_InterfaceImplRow,

        [Description(nameof(Str.MemberRefRow))]
        Metadata_MemberRefRow,

        [Description(nameof(Str.ConstantRow))]
        Metadata_ConstantRow,

        [Description(nameof(Str.CustomAttributeRow))]
        Metadata_CustomAttributeRow,

        [Description(nameof(Str.FieldMarshalRow))]
        Metadata_FieldMarshalRow,

        [Description(nameof(Str.DeclSecurityRow))]
        Metadata_DeclSecurityRow,

        [Description(nameof(Str.ClassLayoutRow))]
        Metadata_ClassLayoutRow,

        [Description(nameof(Str.FieldLayoutRow))]
        Metadata_FieldLayoutRow,

        [Description(nameof(Str.StandAloneSigRow))]
        Metadata_StandAloneSigRow,

        [Description(nameof(Str.EventMapRow))]
        Metadata_EventMapRow,

        [Description(nameof(Str.EventPtrRow))]
        Metadata_EventPtrRow,

        [Description(nameof(Str.EventRow))]
        Metadata_EventRow,

        [Description(nameof(Str.PropertyMapRow))]
        Metadata_PropertyMapRow,

        [Description(nameof(Str.PropertyPtrRow))]
        Metadata_PropertyPtrRow,

        [Description(nameof(Str.PropertyRow))]
        Metadata_PropertyRow,

        [Description(nameof(Str.MethodSemanticsRow))]
        Metadata_MethodSemanticsRow,

        [Description(nameof(Str.MethodImplRow))]
        Metadata_MethodImplRow,

        [Description(nameof(Str.ModuleRefRow))]
        Metadata_ModuleRefRow,

        [Description(nameof(Str.TypeSpecRow))]
        Metadata_TypeSpecRow,

        [Description(nameof(Str.ImplMapRow))]
        Metadata_ImplMapRow,

        [Description(nameof(Str.FieldRvaRow))]
        Metadata_FieldRvaRow,

        [Description(nameof(Str.EncLogRow))]
        Metadata_EncLogRow,

        [Description(nameof(Str.EncMapRow))]
        Metadata_EncMapRow,

        [Description(nameof(Str.AssemblyRow))]
        Metadata_AssemblyRow,

        [Description(nameof(Str.AssemblyProcessorRow))]
        Metadata_AssemblyProcessorRow,

        [Description(nameof(Str.AssemblyOSRow))]
        Metadata_AssemblyOSRow,

        [Description(nameof(Str.AssemblyRefRow))]
        Metadata_AssemblyRefRow,

        [Description(nameof(Str.AssemblyRefProcessorRow))]
        Metadata_AssemblyRefProcessorRow,

        [Description(nameof(Str.AssemblyRefOSRow))]
        Metadata_AssemblyRefOSRow,

        [Description(nameof(Str.FileRow))]
        Metadata_FileRow,

        [Description(nameof(Str.ExportedTypeRow))]
        Metadata_ExportedTypeRow,

        [Description(nameof(Str.ManifestResourceRow))]
        Metadata_ManifestResourceRow,

        [Description(nameof(Str.NestedClassRow))]
        Metadata_NestedClassRow,

        [Description(nameof(Str.GenericParamRow))]
        Metadata_GenericParamRow,

        [Description(nameof(Str.MethodSpecRow))]
        Metadata_MethodSpecRow,

        [Description(nameof(Str.GenericParamConstraintRow))]
        Metadata_GenericParamConstraintRow,

        //Portable PDB

        [Description(nameof(Str.DocumentRow))]
        PortablePdb_DocumentRow,

        [Description(nameof(Str.MethodDebugInformationRow))]
        PortablePdb_MethodDebugInformationRow,

        [Description(nameof(Str.LocalScopeRow))]
        PortablePdb_LocalScopeRow,

        [Description(nameof(Str.LocalVariableRow))]
        PortablePdb_LocalVariableRow,

        [Description(nameof(Str.LocalConstantRow))]
        PortablePdb_LocalConstantRow,

        [Description(nameof(Str.ImportScopeRow))]
        PortablePdb_ImportScopeRow,

        [Description(nameof(Str.StateMachineMethodRow))]
        PortablePdb_StateMachineMethodRow,

        [Description(nameof(Str.CustomDebugInformationRow))]
        PortablePdb_CustomDebugInformationRow,

        #endregion
        #endregion
        #region CLR

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RuntimeInfo"/>
        /// </summary>
        [Description(nameof(Str.RuntimeInfo))]
        RuntimeInfo,

        [Description(nameof(Str.ModuleIndex))]
        ModuleIndex,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ClrEngineMetrics"/>
        /// </summary>
        [Description(nameof(Str.CLR_ENGINE_METRICS))]
        ClrEngineMetrics,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageCorVTableFixup"/>
        /// </summary>
        [Description(nameof(Str.IMAGE_COR_VTABLEFIXUP))]
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
        ModuleImage,
        StrongNameSignature,

        #endregion
        #region R2R

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.R2R.ReadyToRunHeader"/>
        /// </summary>
        [Description(nameof(Str.READYTORUN_HEADER))]
        ReadyToRunHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.R2R.ReadyToRunCoreHeader"/>
        /// </summary>
        [Description(nameof(Str.READYTORUN_CORE_HEADER))]
        ReadyToRunCoreHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.R2R.ReadyToRunSection"/>
        /// </summary>
        [Description(nameof(Str.READYTORUN_SECTION))]
        ReadyToRunSection,

        ReadyToRunSection_CompilerIdentifier,

        #region ReadyToRunSection Bytes

        ReadyToRunSection_ImportSections,
        ReadyToRunSection_RuntimeFunctions,
        ReadyToRunSection_MethodDefEntryPoints,
        ReadyToRunSection_ExceptionInfo,
        ReadyToRunSection_DebugInfo,
        ReadyToRunSection_DelayLoadMethodCallThunks,
        ReadyToRunSection_AvailableTypes,
        ReadyToRunSection_InstanceMethodEntryPoints,
        ReadyToRunSection_InliningInfo,
        ReadyToRunSection_ProfileDataInfo,
        ReadyToRunSection_ManifestMetadata,
        ReadyToRunSection_AttributePresence,
        ReadyToRunSection_InliningInfo2,
        ReadyToRunSection_ComponentAssemblies,
        ReadyToRunSection_OwnerCompositeExecutable,
        ReadyToRunSection_PgoInstrumentationData,
        ReadyToRunSection_ManifestAssemblyMvids,
        ReadyToRunSection_CrossModuleInlineInfo,
        ReadyToRunSection_HotColdMap,
        ReadyToRunSection_MethodIsGenericMap,
        ReadyToRunSection_EnclosingTypeMap,
        ReadyToRunSection_TypeGenericInfoMap,
        ReadyToRunSection_ExternalTypeMaps,
        ReadyToRunSection_ProxyTypeMaps,
        ReadyToRunSection_TypeMapAssemblyTargets,
        ReadyToRunSection_StringTable,
        ReadyToRunSection_GCStaticRegion,
        ReadyToRunSection_ThreadStaticRegion,
        ReadyToRunSection_TypeManagerIndirection,
        ReadyToRunSection_EagerCctor,
        ReadyToRunSection_FrozenObjectRegion,
        ReadyToRunSection_DehydratedData,
        ReadyToRunSection_ThreadStaticOffsetRegion,
        ReadyToRunSection_ImportAddressTables,
        ReadyToRunSection_ModuleInitializerList,
        ReadyToRunSection_ReadonlyBlobRegionStart,
        ReadyToRunSection_TypeMap,
        ReadyToRunSection_ArrayMap,
        ReadyToRunSection_PointerTypeMap,
        ReadyToRunSection_GenericInstanceMap,
        ReadyToRunSection_FunctionPointerTypeMap,
        ReadyToRunSection_GenericParameterMap,
        ReadyToRunSection_BlockReflectionTypeMap,
        ReadyToRunSection_InvokeMap,
        ReadyToRunSection_VirtualInvokeMap,
        ReadyToRunSection_CommonFixupsTable,
        ReadyToRunSection_FieldAccessMap,
        ReadyToRunSection_CCtorContextMap,
        ReadyToRunSection_ByRefTypeMap,
        ReadyToRunSection_DiagGenericInstanceMap,
        ReadyToRunSection_DiagGenericParameterMap,
        ReadyToRunSection_EmbeddedMetadata,
        ReadyToRunSection_DefaultConstructorMap,
        ReadyToRunSection_UnboxingAndInstantiatingStubMap,
        ReadyToRunSection_StructMarshallingStubMap,
        ReadyToRunSection_DelegateMarshallingStubMap,
        ReadyToRunSection_GenericVirtualMethodTable,
        ReadyToRunSection_InterfaceGenericVirtualMethodTable,
        ReadyToRunSection_TypeTemplateMap,
        ReadyToRunSection_GenericMethodsTemplateMap,
        ReadyToRunSection_DynamicInvokeTemplateData,
        ReadyToRunSection_BlobIdResourceIndex,
        ReadyToRunSection_BlobIdResourceData,
        ReadyToRunSection_BlobIdStackTraceEmbeddedMetadata,
        ReadyToRunSection_BlobIdStackTraceMethodRvaToTokenMapping,
        ReadyToRunSection_BlobIdStackTraceLineNumbers,
        ReadyToRunSection_BlobIdStackTraceDocuments,
        ReadyToRunSection_NativeLayoutInfo,
        ReadyToRunSection_NativeReferences,
        ReadyToRunSection_GenericsHashtable,
        ReadyToRunSection_NativeStatics,
        ReadyToRunSection_StaticsInfoHashtable,
        ReadyToRunSection_GenericMethodsHashtable,
        ReadyToRunSection_ExactMethodInstantiationsHashtable,
        ReadyToRunSection_ReadonlyBlobRegionEnd,

        #endregion

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.R2R.ReadyToRunImportSection"/>
        /// </summary>
        [Description(nameof(Str.READYTORUN_IMPORT_SECTION))]
        ReadyToRunImportSection,

        #endregion

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.AppHostSignature"/>
        /// </summary>
        [Description(nameof(Str.AppHostSignature))]
        AppHostSignature,

        /// <summary>
        /// A <see cref="LogicalRegionView"/> that represents a <see cref="PESpy.Bundle.Manifest"/>
        /// </summary>
        BundleManifest,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Bundle.HeaderFixed"/>
        /// </summary>
        [Description(nameof(Str.header_fixed_t))]
        BundleHeaderFixed,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Bundle.HeaderFixedV2"/>
        /// </summary>
        [Description(nameof(Str.header_fixed_v2_t))]
        BundleHeaderFixedV2,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Bundle.FileEntry"/>
        /// </summary>
        [Description(nameof(Str.file_entry_t))]
        BundleFileEntry,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Bundle.FileEntryFixed"/>
        /// </summary>
        [Description(nameof(Str.file_entry_fixed_t))]
        BundleFileEntryFixed,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.Bundle.Location"/>
        /// </summary>
        [Description(nameof(Str.location_t))]
        BundleLocation,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.BundleEncodedString"/>
        /// </summary>
        [Description(nameof(Str.BundleEncodedString))]
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

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.NativeAOT.DotNetRuntimeDebugHeader"/>
        /// </summary>
        [Description(nameof(Str.DotNetRuntimeDebugHeader))]
        DotNetRuntimeDebugHeader,

        DebugTypeEntries,
        GlobalValueEntries,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        NativeAOTModulesA,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        NativeAOTModuleAddress,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="IntPtr"/> typed as a <see cref="long"/>.
        /// </summary>
        NativeAOTModulesZ,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.NativeAOT.ReadyToRunHeader"/>
        /// </summary>
        [Description(nameof(Str.ReadyToRunHeader))]
        NativeAOTReadyToRunHeader,

        [Description(nameof(Str.ModuleInfoRow))]
        ModuleInfoRowV1,

        [Description(nameof(Str.ModuleInfoRow))]
        ModuleInfoRowV2,

        UnknownModuleInfoRowData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.NativeAOT.DebugTypeEntry"/>
        /// </summary>
        [Description(nameof(Str.DebugTypeEntry))]
        DebugTypeEntry,

        DebugTypeEntry_TypeName,
        DebugTypeEntry_FieldName,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.NativeAOT.GlobalValueEntry"/>
        /// </summary>
        [Description(nameof(Str.GlobalValueEntry))]
        GlobalValueEntry,

        GlobalValueEntry_Name,

        #endregion
        #region RTTI

        //TypeDescriptor is covered under exception data

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RTTIBaseClassDescriptor"/>
        /// </summary>
        [Description(nameof(Str._RTTIBaseClassDescriptor))]
        RTTIBaseClassDescriptor,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RTTIBaseClassArray"/>
        /// </summary>
        [Description(nameof(Str._RTTIBaseClassArray))]
        RTTIBaseClassArray,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RTTIClassHierarchyDescriptor"/>
        /// </summary>
        [Description(nameof(Str._RTTIClassHierarchyDescriptor))]
        RTTIClassHierarchyDescriptor,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.RTTICompleteObjectLocator"/>
        /// </summary>
        [Description(nameof(Str._RTTICompleteObjectLocator))]
        RTTICompleteObjectLocator,

        [Description(nameof(Str.PMD))]
        PMD,

        #endregion

        //NE
        NEFile,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="SymString"/>.
        /// </summary>
        NE_ImportedName_String,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="ushort"/>.
        /// </summary>
        NE_ModuleReference,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.NE.ImageOS2Header"/>.
        /// </summary>
        ImageOS2Header,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.NE.new_seg"/>.
        /// </summary>
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

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageVXDHeader"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_VXD_HEADER))]
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
        [Description(nameof(Str.MSF_HDR))]
        MsfHdr,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.BigMsfHdr"/>.
        /// </summary>
        [Description(nameof(Str.BIGMSF_HDR))]
        BigMsfHdr,

        [Description(nameof(Str.SI_PERSIST))]
        SI_PERSIST,

        /// <summary>
        /// An <see cref="IStructView"/> that represents an <see cref="PESpy.PDB.IStreamTable"/>.
        /// </summary>
        [Description(nameof(Str.StreamTable))]
        StreamTable,

        SI,

        //snPDB

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.PDBStream"/>.
        /// </summary>
        [Description(nameof(Str.PDBStream))]
        PDBStream,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.PDBStream70"/>.
        /// </summary>
        [Description(nameof(Str.PDBStream70))]
        PDBStream70,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.NMTNI"/>.
        /// </summary>
        [Description(nameof(Str.StreamNameTable))]
        StreamNameTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.Map{D, R, H}"/>.
        /// </summary>
        [Description(nameof(Str.Map))]
        Map,

        [Description(nameof(Str.Entry))]
        Map_Entry,

        //snTpi

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.HDR"/>.
        /// </summary>
        [Description(nameof(Str.HDR))]
        Hdr,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.HDR_16t"/>.
        /// </summary>
        [Description(nameof(Str.HDR_16t))]
        Hdr_16t,

        [Description(nameof(Str.TpiHash))]
        TpiHash,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="uint"/>
        /// </summary>
        TpiHashValues32,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="ushort"/>
        /// </summary>
        TpiHashValues16,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="TI_OFF"/>
        /// </summary>
        TpiHashOffsets32,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="TI_OFF_16t"/>
        /// </summary>
        TpiHashOffsets16,

        [Description(nameof(Str.OffCb))]
        OffCb,

        //snDbi

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.DBIHdr"/>.
        /// </summary>
        [Description(nameof(Str.DBIHdr))]
        DbiHdr,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.NewDBIHdr"/>.
        /// </summary>
        [Description(nameof(Str.NewDBIHdr))]
        NewDbiHdr,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.Modi20"/>.
        /// </summary>
        [Description(nameof(Str.MODIv2))]
        Modiv2,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.Modi20"/>.
        /// </summary>
        [Description(nameof(Str.MODIv4))]
        Modiv4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.Modi50"/>.
        /// </summary>
        [Description(nameof(Str.MODI50))]
        Modi50,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.Modi60"/>.
        /// </summary>
        [Description(nameof(Str.MODI_60_Persist))]
        Modi60Persist,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.ECInfo"/>.
        /// </summary>
        [Description(nameof(Str.ECInfo))]
        ECInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SC20"/>.
        /// </summary>
        [Description(nameof(Str.SC20))]
        SC20,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SC40"/>.
        /// </summary>
        [Description(nameof(Str.SC40))]
        SC40,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SC"/>.
        /// </summary>
        [Description(nameof(Str.SC))]
        SC,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SC2"/>.
        /// </summary>
        [Description(nameof(Str.SC2))]
        SC2,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SectionContribsV20"/>.
        /// </summary>
        [Description(nameof(Str.SectionContribs))]
        SectionContribsV20,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SectionContribsV40"/>.
        /// </summary>
        [Description(nameof(Str.SectionContribs))]
        SectionContribsV40,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SectionContribsV60"/>.
        /// </summary>
        [Description(nameof(Str.SectionContribs))]
        SectionContribsV60,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.SectionContribs2"/>.
        /// </summary>
        [Description(nameof(Str.SectionContribs))]
        SectionContribs2,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFSegMap"/>.
        /// </summary>
        [Description(nameof(Str.OMFSegMap))]
        OMFSegMap,

        [Description(nameof(Str.OMFSegMapDesc))]
        OMFSegMapDesc,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFFileIndex"/>.
        /// </summary>
        [Description(nameof(Str.OMFFileIndex))]
        OMFFileIndex,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.NMT"/>.
        /// </summary>
        [Description(nameof(Str.NameTable))]
        NameTable,

        [Description(nameof(Str.VHdr))]
        VHdr,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.DbgDataHdr"/>.
        /// </summary>
        [Description(nameof(Str.DbgDataHdr))]
        DbgDataHdr,

        //The string is TYPTYPE but all types have a more specific leaf type,
        //unlike with SYMTYPE where you have things like S_END which is just
        //a simple SYMTYPE
        TypType,

        [Description(nameof(Str.NumericData))]
        NumericData,

        LeafKind, //lfFieldList padding, numeric data. Not necessarily part of numeric data however
        NumericValue,
        NumericStringLength,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="PdbFeature"/>.
        /// </summary>
        PdbFeature,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="CV_SIGNATURE"/>.
        /// </summary>
        CvSignature,

        [Description(nameof(Str.HRFile))]
        HRFile,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="int"/>
        /// </summary>
        HashBucketsBitmap,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="int"/>
        /// </summary>
        HashBuckets,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.CvDebugSSubsectionHeader"/>.
        /// </summary>
        [Description(nameof(Str.CV_DebugSSubsectionHeader_t))]
        CvDebugSSubsectionHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.CvFileCheckSum"/>.
        /// </summary>
        [Description(nameof(Str.CV_FileCheckSum))]
        CvFileCheckSum,

        [Description(nameof(Str.RVAAndFrameData))]
        RvaAndFrameData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.FrameData"/>.
        /// </summary>
        [Description(nameof(Str.FRAMEDATA))]
        FrameData,

        [Description(nameof(Str.CV_DebugSLinesHeader_t))]
        CvDebugSLinesHeader,

        [Description(nameof(Str.CV_DebugSLinesFileBlockHeader_t))]
        CvDebugSLinesFileBlockHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.CvLine"/>.
        /// </summary>
        [Description(nameof(Str.CV_Line_t))]
        CvLine,

        [Description(nameof(Str.InlineeSigAndLines))]
        InlineeSigAndLines,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.InlineeSourceLine"/>.
        /// </summary>
        [Description(nameof(Str.InlineeSourceLine))]
        InlineeSourceLine,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.InlineeSourceLineEx"/>.
        /// </summary>
        [Description(nameof(Str.InlineeSourceLineEx))]
        InlineeSourceLineEx,

        [Description(nameof(Str.FuncMDTokenMap))]
        FuncMDTokenMap,

        [Description(nameof(Str.TypeMDTokenMap))]
        TypeMDTokenMap,

        [Description(nameof(Str.Entry))]
        FuncMDTokenMap_Entry,

        FuncMDTokenMap_MethodData,

        [Description(nameof(Str.Entry))]
        TypeMDTokenMap_Entry,

        TypeMDTokenMap_TypeData,

        [Description(nameof(Str.CrossScopeReferences))]
        CrossScopeReferences,

        [Description(nameof(Str.LocalIdAndGlobalIdPair))]
        LocalIdAndGlobalIdPair,

        [Description(nameof(Str.MergedAssemblyInfo))]
        MergedAssemblyInfo,

        [Description(nameof(Str.PdbIdScope))]
        PdbIdScope,

        [Description(nameof(Str.SrcHeaderOut))]
        SrcHeaderOut,

        #region Symbols

        [Description(nameof(Str.SYMTYPE))]
        SymType,

        [Description(nameof(Str.ALIGNSYM))]
        AlignSym,

        [Description(nameof(Str.ANNOTATIONSYM))]
        AnnotationSym,

        [Description(nameof(Str.ARMSWITCHTABLE))]
        ArmSwitchTable,

        [Description(nameof(Str.ATTRMANYREGSYM2))]
        AttrManyRegSym2,

        [Description(nameof(Str.ATTRREGREL))]
        AttrRegRel,

        [Description(nameof(Str.ATTRREGSYM))]
        AttrRegSym,

        [Description(nameof(Str.ATTRSLOTSYM))]
        AttrSlotSym,

        [Description(nameof(Str.BLOCKSYM16))]
        BlockSym16,

        [Description(nameof(Str.BLOCKSYM32))]
        BlockSym32,

        [Description(nameof(Str.BPRELSYM16))]
        BPRelSym16,

        [Description(nameof(Str.BPRELSYM32))]
        BPRelSym32,

        [Description(nameof(Str.BPRELSYM32_16t))]
        BPRelSym3216t,

        [Description(nameof(Str.BUILDINFOSYM))]
        BuildInfoSym,

        [Description(nameof(Str.CALLSITEINFO))]
        CallSiteInfo,

        [Description(nameof(Str.CEXMSYM16))]
        CExMSym16,

        [Description(nameof(Str.CEXMSYM32))]
        CExMSym32,

        [Description(nameof(Str.CFLAGSYM))]
        CFlagSym,

        [Description(nameof(Str.COFFGROUPSYM))]
        CoffGroupSym,

        [Description(nameof(Str.COMPILESYM))]
        CompileSym,

        [Description(nameof(Str.COMPILESYM3))]
        CompileSym3,

        [Description(nameof(Str.CONSTSYM))]
        ConstSym,

        [Description(nameof(Str.CONSTSYM_16t))]
        ConstSym16t,

        [Description(nameof(Str.DATASYM16))]
        DataSym16,

        [Description(nameof(Str.DATASYM32))]
        DataSym32,

        [Description(nameof(Str.DATASYM32_16t))]
        DataSym3216t,

        [Description(nameof(Str.DATASYMHLSL))]
        DataSymHLSL,

        [Description(nameof(Str.DATASYMHLSL32))]
        DataSymHLSL32,

        [Description(nameof(Str.DATASYMHLSL32_EX))]
        DataSymHLSL32Ex,

        [Description(nameof(Str.DEFRANGESYM))]
        DefRangeSym,

        [Description(nameof(Str.DEFRANGESYMFRAMEPOINTERREL))]
        DefRangeSymFramePointerRel,

        [Description(nameof(Str.DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE))]
        DefRangeSymFramePointerRelFullScope,

        [Description(nameof(Str.DEFRANGESYMHLSL))]
        DefRangeSymHLSL,

        [Description(nameof(Str.DEFRANGESYMREGISTER))]
        DefRangeSymRegister,

        [Description(nameof(Str.DEFRANGESYMREGISTERREL))]
        DefRangeSymRegisterRel,

        [Description(nameof(Str.DEFRANGESYMSUBFIELD))]
        DefRangeSymSubField,

        [Description(nameof(Str.DEFRANGESYMSUBFIELDREGISTER))]
        DefRangeSymSubfieldRegister,

        [Description(nameof(Str.DISCARDEDSYM))]
        DiscardedSym,

        [Description(nameof(Str.DPCSYMTAGMAP))]
        DPCSymTagMap,

        [Description(nameof(Str.ENTRYTHISSYM))]
        EntryThisSym,

        [Description(nameof(Str.ENVBLOCKSYM))]
        EnvBlockSym,

        [Description(nameof(Str.EXPORTSYM))]
        ExportSym,

        [Description(nameof(Str.FILESTATICSYM))]
        FileStaticSym,

        [Description(nameof(Str.FRAMECOOKIE))]
        FrameCookie,

        [Description(nameof(Str.FRAMEPROCSYM))]
        FrameProcSym,

        [Description(nameof(Str.FRAMERELSYM))]
        FrameRelSym,

        [Description(nameof(Str.FUNCTIONLIST))]
        FunctionList,

        [Description(nameof(Str.HEAPALLOCSITE))]
        HeapAllocSite,

        [Description(nameof(Str.INLINESITESYM))]
        InlineSiteSym,

        [Description(nameof(Str.INLINESITESYM2))]
        InlineSiteSym2,

        [Description(nameof(Str.LABELSYM16))]
        LabelSym16,

        [Description(nameof(Str.LABELSYM32))]
        LabelSym32,

        [Description(nameof(Str.LOCALDPCGROUPSHAREDSYM))]
        LocalDPCGroupSharedSym,

        [Description(nameof(Str.LOCALSYM))]
        LocalSym,

        [Description(nameof(Str.MANPROCSYM))]
        ManProcSym,

        [Description(nameof(Str.MANTYPREF))]
        ManTypRef,

        [Description(nameof(Str.MANYREGSYM))]
        ManyRegSym,

        [Description(nameof(Str.MANYREGSYM_16t))]
        ManyRegSym16t,

        [Description(nameof(Str.MANYREGSYM2))]
        ManyRegSym2,

        [Description(nameof(Str.MODTYPEREF))]
        ModTypeRef,

        [Description(nameof(Str.OBJNAMESYM))]
        ObjNameSym,

        [Description(nameof(Str.OEMSYMBOL))]
        OemSymbol,

        [Description(nameof(Str.PDBMAP))]
        PdbMap,

        [Description(nameof(Str.POGOINFO))]
        PogoInfo,

        [Description(nameof(Str.PROCSYM16))]
        ProcSym16,

        [Description(nameof(Str.PROCSYM32))]
        ProcSym32,

        [Description(nameof(Str.PROCSYM32_16t))]
        ProcSym3216t,

        [Description(nameof(Str.PROCSYMIA64))]
        ProcSymIA64,

        [Description(nameof(Str.PROCSYMMIPS))]
        ProcSymMips,

        [Description(nameof(Str.PROCSYMMIPS_16t))]
        ProcSymMips16t,

        [Description(nameof(Str.PUBSYM32))]
        PubSym32,

        [Description(nameof(Str.REFMINIPDB))]
        RefMiniPdb,

        [Description(nameof(Str.REFSYM))]
        RefSym,

        [Description(nameof(Str.REFSYM2))]
        RefSym2,

        [Description(nameof(Str.REGREL16))]
        RegRel16,

        [Description(nameof(Str.REGREL32))]
        RegRel32,

        [Description(nameof(Str.REGREL32_16t))]
        RegRel3216t,

        [Description(nameof(Str.REGSYM))]
        RegSym,

        [Description(nameof(Str.REGSYM_16t))]
        RegSym16t,

        [Description(nameof(Str.RETURNSYM))]
        ReturnSym,

        [Description(nameof(Str.SEARCHSYM))]
        SearchSym,

        [Description(nameof(Str.SECTIONSYM))]
        SectionSym,

        [Description(nameof(Str.SEPCODESYM))]
        SepCodeSym,

        [Description(nameof(Str.SLINK32))]
        SLink32,

        [Description(nameof(Str.SLOTSYM32))]
        SlotSym32,

        [Description(nameof(Str.THUNKSYM16))]
        ThunkSym16,

        [Description(nameof(Str.THUNKSYM32))]
        ThunkSym32,

        [Description(nameof(Str.TRAMPOLINESYM))]
        TrampolineSym,

        [Description(nameof(Str.UDTSYM))]
        UdtSym,

        [Description(nameof(Str.UDTSYM_16t))]
        UdtSym16t,

        [Description(nameof(Str.UNAMESPACE))]
        UNameSpace,

        #endregion

        //Do not add any additional items here; MarkSymbols relies on LfAlias being after the last symbol

        #region Types

        [Description(nameof(Str.lfAlias))]
        LfAlias,

        [Description(nameof(Str.lfArgList))]
        LfArgList,

        [Description(nameof(Str.lfArgList_16t))]
        LfArgList16t,

        [Description(nameof(Str.lfArray))]
        LfArray,

        [Description(nameof(Str.lfArray_16t))]
        LfArray16t,

        [Description(nameof(Str.lfBArray))]
        LfBArray,

        [Description(nameof(Str.lfBArray_16t))]
        LfBArray16t,

        [Description(nameof(Str.lfBClass))]
        LfBClass,

        [Description(nameof(Str.lfBClass_16t))]
        LfBClass16t,

        [Description(nameof(Str.lfBitfield))]
        LfBitfield,

        [Description(nameof(Str.lfBitfield_16t))]
        LfBitfield16t,

        [Description(nameof(Str.lfBuildInfo))]
        LfBuildInfo,

        [Description(nameof(Str.lfChar))]
        LfChar,

        [Description(nameof(Str.lfClass))]
        LfClass,

        [Description(nameof(Str.lfClass_16t))]
        LfClass16t,

        [Description(nameof(Str.lfCmplx128))]
        LfCmplx128,

        [Description(nameof(Str.lfCmplx32))]
        LfCmplx32,

        [Description(nameof(Str.lfCmplx64))]
        LfCmplx64,

        [Description(nameof(Str.lfCmplx80))]
        LfCmplx80,

        [Description(nameof(Str.lfCobol0))]
        LfCobol0,

        [Description(nameof(Str.lfCobol0_16t))]
        LfCobol016t,

        [Description(nameof(Str.lfCobol1))]
        LfCobol1,

        [Description(nameof(Str.lfDefArg))]
        LfDefArg,

        [Description(nameof(Str.lfDefArg_16t))]
        LfDefArg16t,

        [Description(nameof(Str.lfDerived))]
        LfDerived,

        [Description(nameof(Str.lfDerived_16t))]
        LfDerived16t,

        [Description(nameof(Str.lfDimArray))]
        LfDimArray,

        [Description(nameof(Str.lfDimArray_16t))]
        LfDimArray16t,

        [Description(nameof(Str.lfDimCon))]
        LfDimCon,

        [Description(nameof(Str.lfDimCon_16t))]
        LfDimCon16t,

        [Description(nameof(Str.lfDimVar))]
        LfDimVar,

        [Description(nameof(Str.lfDimVar_16t))]
        LfDimVar16t,

        [Description(nameof(Str.lfEasy))]
        LfEasy,

        [Description(nameof(Str.lfEndPreComp))]
        LfEndPreComp,

        [Description(nameof(Str.lfEnum))]
        LfEnum,

        [Description(nameof(Str.lfEnum_16t))]
        LfEnum16t,

        [Description(nameof(Str.lfEnumerate))]
        LfEnumerate,

        [Description(nameof(Str.lfFieldList))]
        LfFieldList,

        [Description(nameof(Str.lfFieldList_16t))]
        LfFieldList16t,

        [Description(nameof(Str.lfFriendCls))]
        LfFriendCls,

        [Description(nameof(Str.lfFriendCls_16t))]
        LfFriendCls16t,

        [Description(nameof(Str.lfFriendFcn))]
        LfFriendFcn,

        [Description(nameof(Str.lfFriendFcn_16t))]
        LfFriendFcn16t,

        [Description(nameof(Str.lfFuncId))]
        LfFuncId,

        [Description(nameof(Str.lfHLSL))]
        LfHLSL,

        [Description(nameof(Str.lfIndex))]
        LfIndex,

        [Description(nameof(Str.lfIndex_16t))]
        LfIndex16t,

        [Description(nameof(Str.lfLabel))]
        LfLabel,

        [Description(nameof(Str.lfList))]
        LfList,

        [Description(nameof(Str.lfLong))]
        LfLong,

        [Description(nameof(Str.lfManaged))]
        LfManaged,

        [Description(nameof(Str.lfMatrix))]
        LfMatrix,

        [Description(nameof(Str.lfMember))]
        LfMember,

        [Description(nameof(Str.lfMember_16t))]
        LfMember16t,

        [Description(nameof(Str.lfMemberModify))]
        LfMemberModify,

        [Description(nameof(Str.lfMethod))]
        LfMethod,

        [Description(nameof(Str.lfMethod_16t))]
        LfMethod16t,

        [Description(nameof(Str.lfMethodList))]
        LfMethodList,

        [Description(nameof(Str.lfMethodList_16t))]
        LfMethodList16t,

        [Description(nameof(Str.lfMFunc))]
        LfMFunc,

        [Description(nameof(Str.lfMFunc_16t))]
        LfMFunc16t,

        [Description(nameof(Str.lfMFuncId))]
        LfMFuncId,

        [Description(nameof(Str.lfModifier))]
        LfModifier,

        [Description(nameof(Str.lfModifier_16t))]
        LfModifier16t,

        [Description(nameof(Str.lfModifierEx))]
        LfModifierEx,

        [Description(nameof(Str.lfNestType))]
        LfNestType,

        [Description(nameof(Str.lfNestType_16t))]
        LfNestType16t,

        [Description(nameof(Str.lfNestTypeEx))]
        LfNestTypeEx,

        [Description(nameof(Str.lfOct))]
        LfOct,

        [Description(nameof(Str.lfOEM))]
        LfOEM,

        [Description(nameof(Str.lfOEM_16t))]
        LfOEM16t,

        [Description(nameof(Str.lfOEM2))]
        LfOEM2,

        [Description(nameof(Str.lfOneMethod))]
        LfOneMethod,

        [Description(nameof(Str.lfOneMethod_16t))]
        LfOneMethod16t,

        [Description(nameof(Str.lfPad))]
        LfPad,

        [Description(nameof(Str.lfPointer))]
        LfPointer,

        [Description(nameof(Str.lfPointer_16t))]
        LfPointer16t,

        [Description(nameof(Str.lfPreComp))]
        LfPreComp,

        [Description(nameof(Str.lfPreComp_16t))]
        LfPreComp16t,

        [Description(nameof(Str.lfProc))]
        LfProc,

        [Description(nameof(Str.lfProc_16t))]
        LfProc16t,

        [Description(nameof(Str.lfQuad))]
        LfQuad,

        [Description(nameof(Str.lfReal128))]
        LfReal128,

        [Description(nameof(Str.lfReal16))]
        LfReal16,

        [Description(nameof(Str.lfReal32))]
        LfReal32,

        [Description(nameof(Str.lfReal48))]
        LfReal48,

        [Description(nameof(Str.lfReal64))]
        LfReal64,

        [Description(nameof(Str.lfReal80))]
        LfReal80,

        [Description(nameof(Str.lfRefSym))]
        LfRefSym,

        [Description(nameof(Str.lfShort))]
        LfShort,

        [Description(nameof(Str.lfSkip))]
        LfSkip,

        [Description(nameof(Str.lfSkip_16t))]
        LfSkip16t,

        [Description(nameof(Str.lfSTMember))]
        LfSTMember,

        [Description(nameof(Str.lfSTMember_16t))]
        LfSTMember16t,

        [Description(nameof(Str.lfStridedArray))]
        LfStridedArray,

        [Description(nameof(Str.lfStringId))]
        LfStringId,

        [Description(nameof(Str.lfTypeServer))]
        LfTypeServer,

        [Description(nameof(Str.lfTypeServer2))]
        LfTypeServer2,

        [Description(nameof(Str.lfUdtModSrcLine))]
        LfUdtModSrcLine,

        [Description(nameof(Str.lfUdtSrcLine))]
        LfUdtSrcLine,

        [Description(nameof(Str.lfULong))]
        LfULong,

        [Description(nameof(Str.lfUnion))]
        LfUnion,

        [Description(nameof(Str.lfUnion_16t))]
        LfUnion16t,

        [Description(nameof(Str.lfUOct))]
        LfUOct,

        [Description(nameof(Str.lfUQuad))]
        LfUQuad,

        [Description(nameof(Str.lfUShort))]
        LfUShort,

        [Description(nameof(Str.lfVarString))]
        LfVarString,

        [Description(nameof(Str.lfVBClass))]
        LfVBClass,

        [Description(nameof(Str.lfVBClass_16t))]
        LfVBClass16t,

        [Description(nameof(Str.lfVector))]
        LfVector,

        [Description(nameof(Str.lfVftable))]
        LfVftable,

        [Description(nameof(Str.lfVFTPath))]
        LfVFTPath,

        [Description(nameof(Str.lfVFTPath_16t))]
        LfVFTPath16t,

        [Description(nameof(Str.lfVFuncOff))]
        LfVFuncOff,

        [Description(nameof(Str.lfVFuncOff_16t))]
        LfVFuncOff16t,

        [Description(nameof(Str.lfVFuncTab))]
        LfVFuncTab,

        [Description(nameof(Str.lfVFuncTab_16t))]
        LfVFuncTab16t,

        [Description(nameof(Str.lfVTShape))]
        LfVTShape,

        [Description(nameof(Str.mlMethod))]
        MlMethod,

        [Description(nameof(Str.mlMethod_16t))]
        MlMethod16t,

        #endregion

        //Do not add any additional items here; MarkTypes GSIHashHdr being after the last type

        //Globals

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.GSIHashHdr"/>.
        /// </summary>
        [Description(nameof(Str.GSIHashHdr))]
        GSIHashHdr,

        //Publics

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.PDB.PSGSIHDR"/>.
        /// </summary>
        [Description(nameof(Str.PSGSIHDR))]
        PSGSIHDR,

        /// <summary>
        /// An <see cref="IFieldView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="int"/>
        /// </summary>
        [Description(nameof(Str.AddressMap))]
        AddressMap,

        /// <summary>
        /// An <see cref="IFieldView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="int"/>
        /// </summary>
        [Description(nameof(Str.ThunkMap))]
        ThunkMap,

        /// <summary>
        /// An <see cref="IFieldView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="SO"/>
        /// </summary>
        [Description(nameof(Str.SectionMap))]
        SectionMap,

        //DBG

        DBGFile,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageSeparateDebugHeader"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_SEPARATE_DEBUG_HEADER))]
        ImageSeparateDebugHeader,

        ExportedNames,
        ExportedNames_Entry,

        //OBJ

        OBJFile,
        InterSectionData,
        Relocations,

        //Do not insert any sections above drectve; ViewByteViewWriter relies on it being the first

        #region Sections

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="FixedUtf8String"/>.
        /// </summary>
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
        chks64,
        cil_db,
        cil_ex,
        cil_fg,
        cil_gl,
        cil_in,
        cil_md,
        cil_sy,
        UnknownSection,

        #endregion

        //LIB

        LIBFile,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="FixedAnsiString"/>.
        /// </summary>
        LIBFile_Signature,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImageArchiveMemberHeader"/>.
        /// </summary>
        [Description(nameof(Str.IMAGE_ARCHIVE_MEMBER_HEADER))]
        ImageArchiveMemberHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.LIB.FirstLinkerMember"/>.
        /// </summary>
        [Description(nameof(Str.FirstLinkerMember))]
        FirstLinkerMember,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.LIB.SecondLinkerMember"/>.
        /// </summary>
        [Description(nameof(Str.SecondLinkerMember))]
        SecondLinkerMember,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.LIB.LongNamesMember"/>.
        /// </summary>
        [Description(nameof(Str.LongNamesMember))]
        LongNamesMember,

        LongImportLibraryMember,
        ShortImportLibraryMember,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="PESpy.AnsiString"/>.
        /// </summary>
        ShortImportLibrary_DllName,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="PESpy.AnsiString"/>.
        /// </summary>
        ShortImportLibrary_ImportName,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.ImportObjectHeader"/>.
        /// </summary>
        [Description(nameof(Str.IMPORT_OBJECT_HEADER))]
        ImportObjectHeader,

        //OMF
        NB05Data,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFDirHeader"/>.
        /// </summary>
        [Description(nameof(Str.OMFDirHeader))]
        OMFDirHeader,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFDirEntry"/>.
        /// </summary>
        [Description(nameof(Str.OMFDirEntry))]
        OMFDirEntry,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="int"/>.
        /// </summary>
        CodeViewSig,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFGlobalTypes"/>.
        /// </summary>
        [Description(nameof(Str.OMFGlobalTypes))]
        OMFGlobalTypes,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFModule"/>.
        /// </summary>
        [Description(nameof(Str.OMFModule))]
        OMFModule,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFSegDesc"/>.
        /// </summary>
        [Description(nameof(Str.OMFSegDesc))]
        OMFSegDesc,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFSymHash"/>.
        /// </summary>
        [Description(nameof(Str.OMFSymHash))]
        OMFSymHash,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFSourceFile"/>.
        /// </summary>
        [Description(nameof(Str.OMFSourceFile))]
        OMFSourceFile,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFSourceLine"/>.
        /// </summary>
        [Description(nameof(Str.OMFSourceLine))]
        OMFSourceLine,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFSourceModule"/>.
        /// </summary>
        [Description(nameof(Str.OMFSourceModule))]
        OMFSourceModule,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.OMFTypeFlags"/>.
        /// </summary>
        [Description(nameof(Str.OMFTypeFlags))]
        OMFTypeFlags,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.SymHash32"/>.
        /// </summary>
        [Description(nameof(Str.SymHash32))]
        SymHash32,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.SymHash32Long"/>.
        /// </summary>
        [Description(nameof(Str.SymHash32Long))]
        SymHash32Long,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.AddrHash32v4"/>.
        /// </summary>
        [Description(nameof(Str.AddrHash32v4))]
        AddrHash32v4,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.AddrHash32v5"/>.
        /// </summary>
        [Description(nameof(Str.AddrHash32v5))]
        AddrHash32v5,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.AddrHash32v8"/>.
        /// </summary>
        [Description(nameof(Str.AddrHash32v8))]
        AddrHash32v8,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.AddrHash32v12"/>.
        /// </summary>
        [Description(nameof(Str.AddrHash32v12))]
        AddrHash32v12,

        /// <summary>
        /// A <see cref="ByteBlobView"/> that contains the bytes of a SymHash that PESpy does not currently
        /// know how to parse.
        /// </summary>
        UnknownSymHash,

        /// <summary>
        /// A <see cref="ByteBlobView"/> that contains the bytes of an AddrHash that PESpy does not currently
        /// know how to parse.
        /// </summary>
        UnknownAddrHash,

        //NB02

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.dnt"/>.
        /// </summary>
        [Description(nameof(Str.dnt))]
        dnt,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.nsg"/>.
        /// </summary>
        [Description(nameof(Str.nsg))]
        nsg,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.nsg32"/>.
        /// </summary>
        [Description(nameof(Str.nsg32))]
        nsg32,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.pbi"/>.
        /// </summary>
        [Description(nameof(Str.pbi))]
        pbi,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.pbi32"/>.
        /// </summary>
        [Description(nameof(Str.pbi32))]
        pbi32,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.smd"/>.
        /// </summary>
        [Description(nameof(Str.smd))]
        smd,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.smd32"/>.
        /// </summary>
        [Description(nameof(Str.smd32))]
        smd32,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.loe"/>.
        /// </summary>
        [Description(nameof(Str.loe))]
        loe,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.loe32"/>.
        /// </summary>
        [Description(nameof(Str.loe32))]
        loe32,

        [Description(nameof(Str.LineNumberOffset))]
        LineNumberOffset,

        [Description(nameof(Str.LineNumberOffset))]
        LineNumberOffset32,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="SymString"/>.
        /// </summary>
        [Description(nameof(Str.LibraryName))]
        LibraryName,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="PESpy.AnsiString"/>.
        /// </summary>
        [Description(nameof(Str.SegmentName))]
        SegmentName,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an array of <see cref="PESpy.OldSymType"/>.
        /// </summary>
        OldSymType,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an array of <see cref="PESpy.OldTypType"/>.
        /// </summary>
        OldTypType,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.DNRBModule"/>.
        /// </summary>
        [Description(nameof(Str.DNRBModule))]
        DNRBModule,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an array of <see cref="PESpy.pbi"/>.
        /// </summary>
        DNRB_Publics,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an array of <see cref="PESpy.OldTypType"/>.
        /// </summary>
        DNRB_Types,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an array of <see cref="PESpy.OldSymType"/>.
        /// </summary>
        DNRB_Symbols,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an array of <see cref="PESpy.loe"/>.
        /// </summary>
        DNRB_SourceLines,

        /// <summary>
        /// An <see cref="IValueView"/> that represents a <see cref="NativeSpan{T}"/> of <see cref="int"/>
        /// </summary>
        [Description(nameof(Str.secOffset))]
        DNRBSecOffset,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="ushort"/>.
        /// </summary>
        [Description(nameof(Str.version))]
        DNRBVersion,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="CodeViewSig"/>.
        /// </summary>
        [Description(nameof(Str.signature))]
        DNRBSignature,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="int"/>.
        /// </summary>
        [Description(nameof(Str.secTblOffset))]
        DNRBSecTblOffset,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="int"/>.
        /// </summary>
        [Description(nameof(Str.lfoDir))]
        LfoDir,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="int"/>.
        /// </summary>
        [Description(nameof(Str.lfoBase))]
        LfoBase,

        /// <summary>
        /// An <see cref="IValueView"/> that represents an <see cref="ushort"/>.
        /// </summary>
        [Description(nameof(Str.cDir))]
        cDir,

        //VB

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VB.ExeProjectInfo"/>.
        /// </summary>
        [Description(nameof(Str.EXEPROJECTINFO))]
        ExeProjectInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VB.ExeFormInfo"/>.
        /// </summary>
        [Description(nameof(Str.EXEFORMINFO))]
        ExeFormInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VB.ExeOcxInfo"/>.
        /// </summary>
        [Description(nameof(Str.EXEOCXINFO))]
        ExeOcxInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VB.RegData"/>.
        /// </summary>
        [Description(nameof(Str.REGDATA))]
        RegData,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VB.VBProjectInfo1"/>.
        /// </summary>
        [Description(nameof(Str.ProjectInfo))]
        VBProjectInfo1,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VB.VBProjectInfo2"/>.
        /// </summary>
        [Description(nameof(Str.SecondaryProjectInfo))]
        VBProjectInfo2,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VB.VBObjectTable"/>.
        /// </summary>
        [Description(nameof(Str.ObjectTable))]
        VBObjectTable,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VB.VBPublicObjectDescriptor"/>.
        /// </summary>
        [Description(nameof(Str.PublicObjectDescriptor))]
        VBPublicObjectDescriptor,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VB.VBPrivateObjectDescriptor"/>.
        /// </summary>
        [Description(nameof(Str.PrivateObjectDescriptor))]
        VBPrivateObjectDescriptor,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VB.VBObjectInfo"/>.
        /// </summary>
        [Description(nameof(Str.ObjectInfo))]
        VBObjectInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VB.VBOptionalObjectInfo"/>.
        /// </summary>
        [Description(nameof(Str.OptionalObjectInfo))]
        VBOptionalObjectInfo,

        /// <summary>
        /// An <see cref="IStructView"/> that represents a <see cref="PESpy.VB.VBControlInfo"/>.
        /// </summary>
        [Description(nameof(Str.ControlInfo))]
        VBControlInfo,

        PortablePDBFile,

        //DOS
        DOSFile,

        OMFFile,
        OMFLIBFile,
        OMFDBGFile,

        SYMFile
    }
}
