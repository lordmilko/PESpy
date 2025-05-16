using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ClrDebug;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace PESpy.Tests
{
    [TestClass]
    public class Generate
    {
        [TestMethod]
        public void GenerateTypes()
        {
            var c = new GenerationContext();

            #region Top Level

            //ByteBlob.cs

            #region ImageDataDirectory

            c.Struct("ImageDataDirectory")
                .Field("VirtualAddress", typeof(int))
                .Field("Size", typeof(int));

            #endregion
            #region ImageDosHeader

            c.Struct("ImageDosHeader", help: "Represents the <see cref=\"IMAGE_DOS_HEADER\"/> structure.")
                .Field("Magic", typeof(short), help: "Magic number")
                .Field("BytesOnLastPageOfFile", typeof(short), help: "Bytes on last page of file")
                .Field("PagesInFile", typeof(short), help: "Pages in file")
                .Field("Relocations", typeof(short), help: "Relocations")
                .Field("SizeOfHeaderInParagraphs", typeof(short), help: "Size of header in paragraphs")
                .Field("MinimumExtraParagraphsNeeded", typeof(ushort), help: "Minimum extra paragraphs needed")
                .Field("MaximumExtraParagraphsNeeded", typeof(ushort), help: "Maximum extra paragraphs needed")
                .Field("InitialRelativeSSValue", typeof(short), help: "Initial (relative) SS value")
                .Field("InitialSPValue", typeof(short), help: "Initial SP value")
                .Field("Checksum", typeof(short), help: "Checksum")
                .Field("InitialIPValue", typeof(short), help: "Initial IP value")
                .Field("InitialRelativeCSValue", typeof(short), help: "Initial (relative) CS value")
                .Field("FileAddressOfRelocationTable", typeof(short), help: "File address of relocation table")
                .Field("OverlayNumber", typeof(short), help: "Overlay number")
                .Field("ReservedWords", typeof(short), "4", help: "Reserved words")
                .Field("OEMIdentifier", typeof(short), help: "OEM identifier (for e_oeminfo)")
                .Field("OEMInformation", typeof(short), help: "OEM information; e_oemid specific")
                .Field("ReservedWords2", typeof(short), numElems: "10", help: "Reserved words")
                .Field("FileAddressOfNewExeHeader", typeof(int), help: "File address of new exe header");

            #endregion
            #region ImageFileHeader

            c.Struct("ImageFileHeader")
                .Field("Machine", typeof(IMAGE_FILE_MACHINE), serializationType: typeof(ushort))
                .Field("NumberOfSections", typeof(short))
                .Field("TimeDateStamp", typeof(uint))
                .Field("PointerToSymbolTable", typeof(int), va: typeof(VA<CoffSymbolTable>))
                .Field("NumberOfSymbols", typeof(int))
                .Field("SizeOfOptionalHeader", typeof(short))
                .Field("Characteristics", typeof(ImageFile), serializationType: typeof(ushort));

            #endregion
            #region ImageNtHeaders

            c.Struct("ImageNtHeaders")
                .Field("Signature", typeof(int))
                .Field("FileHeader", typeof(ImageFileHeader), eager: true)
                .Field("OptionalHeader", typeof(ImageOptionalHeader), eager: true);

            #endregion
            #region ImageOptionalHeader

            c.Struct("ImageOptionalHeader")
                //Standard Fields
                .Field("Magic", typeof(PEMagic), serializationType: typeof(ushort))
                .Field("MajorLinkerVersion", typeof(byte))
                .Field("MinorLinkerVersion", typeof(byte))
                .Field("SizeOfCode", typeof(int))
                .Field("SizeOfInitializedData", typeof(int))
                .Field("SizeOfUninitializedData", typeof(int))
                .Field("AddressOfEntryPoint", typeof(int))
                .Field("BaseOfCode", typeof(int))
                .Field("BaseOfData", typeof(int), x86Only: true)

                //Windows Specific Fields
                .Field("ImageBase", typeof(long), pointer: true)
                .Field("SectionAlignment", typeof(int))
                .Field("FileAlignment", typeof(int))
                .Field("MajorOperatingSystemVersion", typeof(ushort))
                .Field("MinorOperatingSystemVersion", typeof(ushort))
                .Field("MajorImageVersion", typeof(ushort))
                .Field("MinorImageVersion", typeof(ushort))
                .Field("MajorSubsystemVersion", typeof(ushort))
                .Field("MinorSubsystemVersion", typeof(ushort))
                .Field("Win32VersionValue", typeof(int))
                .Field("SizeOfImage", typeof(int))
                .Field("SizeOfHeaders", typeof(int))
                .Field("CheckSum", typeof(uint))
                .Field("Subsystem", typeof(ImageSubsystem), serializationType: typeof(ushort))
                .Field("DllCharacteristics", typeof(ImageDllCharacteristics), serializationType: typeof(ushort))
                .Field("SizeOfStackReserve", typeof(ulong), pointer: true)
                .Field("SizeOfStackCommit", typeof(ulong), pointer: true)
                .Field("SizeOfHeapReserve", typeof(ulong), pointer: true)
                .Field("SizeOfHeapCommit", typeof(ulong), pointer: true)
                .Field("LoaderFlags", typeof(ImageLoaderFlags), serializationType: typeof(uint))
                .Field("NumberOfRvaAndSizes", typeof(int))

                //Directory Entries
                .Field("ExportTableDirectory", typeof(ImageDataDirectory))
                .Field("ImportTableDirectory", typeof(ImageDataDirectory))
                .Field("ResourceTableDirectory", typeof(ImageDataDirectory))
                .Field("ExceptionTableDirectory", typeof(ImageDataDirectory))
                .Field("SecurityTableDirectory", typeof(ImageDataDirectory))
                .Field("BaseRelocationTableDirectory", typeof(ImageDataDirectory))
                .Field("DebugTableDirectory", typeof(ImageDataDirectory))
                .Field("CopyrightTableDirectory", typeof(ImageDataDirectory))
                .Field("GlobalPointerTableDirectory", typeof(ImageDataDirectory))
                .Field("ThreadLocalStorageTableDirectory", typeof(ImageDataDirectory))
                .Field("LoadConfigTableDirectory", typeof(ImageDataDirectory))
                .Field("BoundImportTableDirectory", typeof(ImageDataDirectory))
                .Field("ImportAddressTableDirectory", typeof(ImageDataDirectory))
                .Field("DelayImportTableDirectory", typeof(ImageDataDirectory))
                .Field("CorHeaderTableDirectory", typeof(ImageDataDirectory))
                .Field("NullDirectory", typeof(ImageDataDirectory));

            #endregion
            #region ImageRelocation

            c.Struct("ImageRelocation")
                .Field("VirtualAddress", typeof(int))
                .Field("RelocCount", typeof(int))
                .Field("SymbolTableIndex", typeof(int))
                .Field("Type", typeof(short));

            #endregion
            #region ImageSectionHeader

            c.Struct("ImageSectionHeader")
                .Field("Name", typeof(string), stringType: StringType.NullPaddedUTF8, nullPaddedUTF8: 8)
                .Field("VirtualSize", typeof(int))
                .Field("VirtualAddress", typeof(int))
                .Field("SizeOfRawData", typeof(int))
                .Field("PointerToRawData", typeof(int))
                .Field("PointerToRelocations", typeof(int))
                .Field("PointerToLineNumbers", typeof(int))
                .Field("NumberOfRelocations", typeof(short))
                .Field("NumberOfLineNumbers", typeof(short))
                .Field("Characteristics", typeof(IMAGE_SCN), serializationType: typeof(uint));

            #endregion

            //ProdItem.cs
            //RawValue.cs
            //RichHeader.cs
            //RVA`1.cs

            #endregion
            #region 00. Export

            c.Struct("ImageExportDirectory")
                .Field("Characteristics", typeof(int))
                .Field("TimeDateStamp", typeof(uint))
                .Field("MajorVersion", typeof(ushort))
                .Field("MinorVersion", typeof(ushort))
                .Field("Name", typeof(int))
                .Field("Base", typeof(int))
                .Field("NumberOfFunctions", typeof(int))
                .Field("NumberOfNames", typeof(int))
                .Field("AddressOfFunctions", typeof(int))
                .Field("AddressOfNames", typeof(int))
                .Field("AddressOfNameOrdinals", typeof(int));

            #endregion
            #region 01. Import
            #region ImageImportByName

            c.Struct("ImageImportByName")
                .Field("Hint", typeof(short))
                .Field("Name", typeof(string), stringType: StringType.AnsiNullTerminated);

            #endregion
            #region ImageImportDescriptor

            c.Struct("ImageImportDescriptor")
                .Field("OriginalFirstThunk", typeof(int), rva: typeof(RVA<ImageThunkData>))
                .Field("TimeDateStamp", typeof(uint))
                .Field("ForwarderChain", typeof(int))
                .Field("Name", typeof(int), rva: typeof(RVA<string>), stringType: StringType.AnsiNullTerminated)
                .Field("FirstThunk", typeof(int), rva: typeof(RVA<ImageThunkData>));

            #endregion

            //01. Import\ImageThunkData.cs

            #endregion
            #region 02. Resource
            #region ClrDebugResource

            c.Struct("ClrDebugResource")
                .Field("Version", typeof(int))
                .Field("Signature", typeof(Guid))
                .Field("DacTimeStamp", typeof(int))
                .Field("DacSizeOfImage", typeof(int))
                .Field("DbiTimeStamp", typeof(int))
                .Field("DbiSizeOfImage", typeof(int));

            #endregion
            #region ImageResourceDataEntry

            c.Struct("ImageResourceDataEntry")
                .Field("OffsetToData", typeof(int))
                .Field("Size", typeof(int))
                .Field("CodePage", typeof(int))
                .Field("Reserved", typeof(int));

            #endregion
            #region ImageResourceDirectory

            c.Struct("ImageResourceDirectory")
                .Field("Characteristics", typeof(uint))
                .Field("TimeDateStamp", typeof(uint))
                .Field("MajorVersion", typeof(ushort))
                .Field("MinorVersion", typeof(ushort))
                .Field("NumberOfNamedEntries", typeof(ushort))
                .Field("NumberOfIdEntries", typeof(ushort));

            #endregion
            #region ImageResourceDirectoryEntry

            c.Struct("ImageResourceDirectoryEntry")
                .Field("NameOrId", typeof(int))
                .Field("dataAndDirectoryUnion", typeof(int));

            #endregion
            #region ImageResourceDirStringU

            c.Struct("ImageResourceDirStringU")
                .Field("Length", typeof(short))
                .Field("NameString", typeof(string), numElems: "Length", stringType: StringType.UnicodeFixedLength);

            #endregion
            #region String

            c.Struct("String")
                .Field("Length", typeof(short))
                .Field("ValueLength", typeof(short))
                .Field("Type", typeof(short))
                .Field("Key", typeof(string), stringType: StringType.Utf16NullTerminated);

            //todo: padding
            //todo: value

            #endregion

            //StringFileInfo and VarFileInfo most both be eagerly read, as their key determines which strict to use

            #region StringTable

            c.Struct("StringTable")
                .Field("Length", typeof(short))
                .Field("ValueLength", typeof(short))
                .Field("Type", typeof(short))
                .Field("Key", typeof(string), stringType: StringType.Utf16NullTerminated);

            //todo: padding, items

            #endregion
            #region Var

            c.Struct("Var")
                .Field("Length", typeof(short))
                .Field("ValueLength", typeof(short))
                .Field("Type", typeof(short))
                .Field("Key", typeof(string), stringType: StringType.Utf16NullTerminated);

            //todo: padding, items

            #endregion
            #region VsFixedFileInfo

            c.Struct("VsFixedFileInfo")
                .Field("Signature", typeof(uint))
                .Field("StrucVersion", typeof(uint))
                .Field("FileVersionMS", typeof(int))
                .Field("FileVersionLS", typeof(int))
                .Field("ProductVersionMS", typeof(int))
                .Field("ProductVersionLS", typeof(int))
                .Field("FileFlagsMask", typeof(uint))
                .Field("FileFlags", typeof(VS_FF), serializationType: typeof(uint))
                .Field("FileOS", typeof(VOS), serializationType: typeof(uint))
                .Field("FileType", typeof(uint))
                .Field("FileSubtype", typeof(uint))
                .Field("FileDateMS", typeof(uint))
                .Field("FileDateLS", typeof(uint));

            #endregion

            //02. Resource\VsVersionInfo.cs

            #endregion
            #region 03. Exception

            //03. Exception\FuncInfo.cs
            //03. Exception\FuncInfoHeader.cs
            //03. Exception\FuncInfoV1.cs
            //03. Exception\HandlerType.cs
            //03. Exception\IptoStateMapEntry.cs
            //03. Exception\RuntimeFunction.cs
            //03. Exception\ScopeTable.cs
            //03. Exception\TryBlockMapEntry.cs
            //03. Exception\TypeDescriptor.cs
            //03. Exception\UnwindCode.cs
            //03. Exception\UnwindInfo.cs
            //03. Exception\UnwindMapEntry.cs

            #endregion
            #region 04. Security

            //04. Security\SignedData.cs
            //04. Security\WinCertificate.cs

            #endregion
            #region 05. BaseRelocation

            //05. BaseRelocation\ImageBaseRelocation.cs

            #endregion
            #region 06. Debug
            #region EmbeddedPortablePdb

            c.Struct("EmbeddedPortablePdb")
                .Field("Signature", typeof(int))
                .Field("UncompressedSize", typeof(int))
                .Field("PortablePdbImage", typeof(byte), numElems: "sizeOfData - 8");

            #endregion
            #region FpoData

            c.Struct("FpoData")
                .Field("OffStart", typeof(int))
                .Field("ProcSize", typeof(int))
                .Field("Locals", typeof(int))
                .Field("Params", typeof(int))
                .Field("flags", typeof(int), modifier: "private");

            #endregion
            #region ImageCoffSymbolsHeader

            c.Struct("ImageCoffSymbolsHeader")
                .Field("NumberOfSymbols", typeof(int))
                .Field("LvaToFirstSymbol", typeof(int))
                .Field("NumberOfLinenumbers", typeof(int))
                .Field("LvaToFirstLinenumber", typeof(int))
                .Field("RvaToFirstByteOfCode", typeof(int))
                .Field("RvaToLastByteOfCode", typeof(int))
                .Field("RvaToFirstByteOfData", typeof(int))
                .Field("RvaToLastByteOfData", typeof(int));

            #endregion
            #region ImageDebugDirectory

            c.Struct("ImageDebugDirectory")
                .Field("Characteristics", typeof(int))
                .Field("TimeDateStamp", typeof(uint))
                .Field("MajorVersion", typeof(ushort))
                .Field("MinorVersion", typeof(ushort))
                .Field("Type", typeof(ImageDebugType), serializationType: typeof(uint))
                .Field("SizeOfData", typeof(int))
                .Field("AddressOfRawData", typeof(int))
                .Field("PointerToRawData", typeof(int));

            #endregion
            #region ImageDebugMisc

            c.Struct("ImageDebugMisc")
                .Field("DataType", typeof(ImageDebugMiscType), serializationType: typeof(uint))
                .Field("Length", typeof(int))
                .Field("Unicode", typeof(bool), serializationType: typeof(byte))
                .Field("Reserved", typeof(byte), numElems: "3");

            //Data is too complicated; use custom field that caches the value

            #endregion
            #region NB10I

            c.Struct("NB10I")
                .Field("Signature", typeof(int))
                .Field("dwOffset", typeof(int))
                .Field("PdbSignature", typeof(int))
                .Field("Age", typeof(int))
                .Field("Path", typeof(string), stringType: StringType.AnsiNullTerminated);

            #endregion
            #region PdbChecksum

            c.Struct("PdbChecksum")
                .Field("AlgorithmName", typeof(string), stringType: StringType.Utf8NullTerminated)
                .Field("Checksum", typeof(byte), numElems: "sizeOfData - (AlgorithmName.Length + 1)");

            #endregion

            //06. Debug\PogoData.cs

            c.Struct("PogoItem")
                .Field("RVA", typeof(int))
                .Field("Size", typeof(int))
                .Field("Name", typeof(string), stringType: StringType.AnsiNullTerminated);

            #region Reproducible

            c.Struct("Reproducible")
                .Field("Size", typeof(int))
                .Field("Hash", typeof(byte), numElems: "Size");

            #endregion
            #region RSDSI

            c.Struct("RSDSI")
                .Field("Signature", typeof(int))
                .Field("Guid", typeof(Guid))
                .Field("Age", typeof(int))
                .Field("Path", typeof(string), stringType: StringType.AnsiNullTerminated);

            #endregion
            #region VCFeature

            c.Struct("VCFeature")
                .Field("PreVC11", typeof(int))
                .Field("C_CPP", typeof(int))
                .Field("GS", typeof(int))
                .Field("SDL", typeof(int))
                .Field("GuardN", typeof(int));

            #endregion
            #region XFixupData

            c.Struct("XFixupData")
                .Field("Type", typeof(short))
                .Field("Extra", typeof(short))
                .Field("Rva", typeof(int))
                .Field("RvaTarget", typeof(int));

            #endregion
            #endregion
            #region 09. TLS

            c.Struct("ImageTlsDirectory")
                .Field("StartAddressOfRawData", typeof(long), pointer: true)
                .Field("EndAddressOfRawData", typeof(long), pointer: true)
                .Field("AddressOfIndex", typeof(long), pointer: true)
                .Field("AddressOfCallBacks", typeof(long), pointer: true)
                .Field("SizeOfZeroFill", typeof(int))
                .Field("Characteristics", typeof(IMAGE_SCN_ALIGN), serializationType: typeof(uint));

            #endregion
            #region 10. LoadConfig

            //10. LoadConfig\GuardAddressTakenIatEntryTable.cs
            //10. LoadConfig\GuardCFFunctionTable.cs
            //10. LoadConfig\GuardEHContinuationTable.cs
            //10. LoadConfig\GuardLongJumpTargetTable.cs
            //10. LoadConfig\ImageBaseRelocation`1.cs
            //10. LoadConfig\ImageBDDDynamicRelocation.cs

            #region ImageBDDDynamicRelocation

            c.Struct("ImageBDDDynamicRelocation")
                .Field("Left", typeof(short))
                .Field("Right", typeof(short))
                .Field("Value", typeof(int));

            #endregion

            //10. LoadConfig\ImageBDDInfo.cs
            //10. LoadConfig\ImageDynamicRelocation.cs
            //10. LoadConfig\ImageDynamicRelocationTable.cs
            //10. LoadConfig\ImageDynamicRelocationV2.cs

            #region ImageEnclaveConfig

            c.Struct("ImageEnclaveConfig")
                .Field("Size", typeof(int))
                .Field("MinimumRequiredConfigSize", typeof(int))
                .Field("PolicyFlags", typeof(int))
                .Field("NumberOfImports", typeof(int))
                .Field("ImportList", typeof(int))
                .Field("ImportEntrySize", typeof(int))
                .Field("FamilyID", typeof(byte), numElems: "IMAGE_ENCLAVE_SHORT_ID_LENGTH")
                .Field("ImageID", typeof(byte), numElems: "IMAGE_ENCLAVE_SHORT_ID_LENGTH")
                .Field("ImageVersion", typeof(int))
                .Field("SecurityVersion", typeof(int))
                .Field("EnclaveSize", typeof(long), pointer: true)
                .Field("NumberOfThreads", typeof(int))
                .Field("EnclaveFlags", typeof(int));

            #endregion
            #region ImageEnclaveImport

            c.Struct("ImageEnclaveImport")
                .Field("MatchType", typeof(IMAGE_ENCLAVE_IMPORT_MATCH), serializationType: typeof(uint))
                .Field("MinimumSecurityVersion", typeof(int))
                .Field("UniqueOrAuthorID", typeof(byte), numElems: "IMAGE_ENCLAVE_LONG_ID_LENGTH")
                .Field("FamilyID", typeof(byte), numElems: "IMAGE_ENCLAVE_SHORT_ID_LENGTH")
                .Field("ImageID", typeof(byte), numElems: "IMAGE_ENCLAVE_SHORT_ID_LENGTH")
                .Field("ImportName", typeof(int))
                .Field("Reserved", typeof(int));

            #endregion

            //10. LoadConfig\ImageEpilogueDynamicRelocationHeader.cs
            //10. LoadConfig\ImageFunctionOverrideDynamicRelocation.cs
            //10. LoadConfig\ImageFunctionOverrideHeader.cs
            //10. LoadConfig\ImageImportControlTransferDynamicRelocation.cs

            #region ImageImportControlTransferDynamicRelocation

            c.Struct("ImageImportControlTransferDynamicRelocation")
                .Field("flags", typeof(uint), modifier: "private");

            #endregion
            #region ImageIndirControlTransferDynamicRelocation

            c.Struct("ImageIndirControlTransferDynamicRelocation")
                .Field("flags", typeof(ushort), modifier: "private");

            #endregion
            #region ImageLoadConfigCodeIntegrity

            c.Struct("ImageLoadConfigCodeIntegrity")
                .Field("Flags", typeof(ushort))
                .Field("Catalog", typeof(ushort))
                .Field("CatalogOffset", typeof(int))
                .Field("Reserved", typeof(int));

            #endregion

            c.Struct("ImageLoadConfigDirectory")
                .Field("Size", typeof(int)) //0
            #region Default
                .Field("TimeDateStamp", typeof(uint), lengthCondition: "Size") //1
                .Field("MajorVersion", typeof(ushort), lengthCondition: "Size") //2
                .Field("MinorVersion", typeof(ushort), lengthCondition: "Size") //3
                .Field("GlobalFlagsClear", typeof(int), lengthCondition: "Size") //4
                .Field("GlobalFlagsSet", typeof(int), lengthCondition: "Size") //5
                .Field("CriticalSectionDefaultTimeout", typeof(int), lengthCondition: "Size") //6
                .Field("DeCommitFreeBlockThreshold", typeof(long), pointer: true, lengthCondition: "Size") //7
                .Field("DeCommitTotalFreeThreshold", typeof(long), pointer: true, lengthCondition: "Size") //8
                .Field("LockPrefixTable", typeof(long), pointer: true, lengthCondition: "Size") //9
                .Field("MaximumAllocationSize", typeof(long), pointer: true, lengthCondition: "Size") //10
                .Field("VirtualMemoryThreshold", typeof(long), pointer: true, lengthCondition: "Size") //11

                //todo: need special handling for these two; theyre back to front in x86 vs x64
                .Field("ProcessAffinityMask", typeof(long), pointer: true, lengthCondition: "Size") //12
                .Field("ProcessHeapFlags", typeof(int), lengthCondition: "Size") //13

                .Field("CSDVersion", typeof(ushort), lengthCondition: "Size") //14
                .Field("DependentLoadFlags", typeof(ushort), lengthCondition: "Size") //15
                .Field("EditList", typeof(long), pointer: true, lengthCondition: "Size") //16
                .Field("SecurityCookie", typeof(long), pointer: true, lengthCondition: "Size") //17
                .Field("SEHandlerTable", typeof(long), pointer: true, lengthCondition: "Size") //18
                .Field("SEHandlerCount", typeof(long), pointer: true, lengthCondition: "Size") //19
            #endregion
            #region Windows SDK 8.1+
                .Field("GuardCFCheckFunctionPointer", typeof(long), pointer: true, lengthCondition: "Size") //20
                .Field("GuardCFDispatchFunctionPointer", typeof(long), pointer: true, lengthCondition: "Size") //21
                .Field("GuardCFFunctionTable", typeof(long), pointer: true, lengthCondition: "Size") //22
                .Field("GuardCFFunctionCount", typeof(long), pointer: true, lengthCondition: "Size") //23
                .Field("GuardFlags", typeof(IMAGE_GUARD), serializationType: typeof(uint), lengthCondition: "Size") //24
            #endregion
            #region #region Windows SDK 10.0.10586.0+
                .Field("CodeIntegrity", typeof(ImageLoadConfigCodeIntegrity), lengthCondition: "Size") //25
                .Field("GuardAddressTakenIatEntryTable", typeof(long), pointer: true, lengthCondition: "Size") //26
                .Field("GuardAddressTakenIatEntryCount", typeof(long), pointer: true, lengthCondition: "Size") //27
                .Field("GuardLongJumpTargetTable", typeof(long), pointer: true, lengthCondition: "Size") //28
                .Field("GuardLongJumpTargetCount", typeof(long), pointer: true, lengthCondition: "Size") //29
                .Field("DynamicValueRelocTable", typeof(long), pointer: true, lengthCondition: "Size") //30
                .Field("CHPEMetadataPointer", typeof(long), pointer: true, lengthCondition: "Size") //31
            #endregion
            #region Windows SDK 10.0.15063.468+
                .Field("GuardRFFailureRoutine", typeof(long), pointer: true, lengthCondition: "Size") //32
                .Field("GuardRFFailureRoutineFunctionPointer", typeof(long), pointer: true, lengthCondition: "Size") //33
                .Field("DynamicValueRelocTableOffset", typeof(int), lengthCondition: "Size") //34
                .Field("DynamicValueRelocTableSection", typeof(ushort), lengthCondition: "Size") //35
                .Field("Reserved2", typeof(ushort), lengthCondition: "Size") //36
                .Field("GuardRFVerifyStackPointerFunctionPointer", typeof(long), pointer: true, lengthCondition: "Size") //37
                .Field("HotPatchTableOffset", typeof(int), lengthCondition: "Size") //38
                .Field("Reserved3", typeof(int), lengthCondition: "Size") //39
                .Field("EnclaveConfigurationPointer", typeof(long), pointer: true, lengthCondition: "Size") //40
                .Field("VolatileMetadataPointer", typeof(long), pointer: true, lengthCondition: "Size") //41
                .Field("GuardEHContinuationTable", typeof(long), pointer: true, lengthCondition: "Size") //42
                .Field("GuardEHContinuationCount", typeof(long), pointer: true, lengthCondition: "Size") //43
                .Field("GuardXFGCheckFunctionPointer", typeof(long), pointer: true, lengthCondition: "Size") //44
                .Field("GuardXFGDispatchFunctionPointer", typeof(long), pointer: true, lengthCondition: "Size") //45
                .Field("GuardXFGTableDispatchFunctionPointer", typeof(long), pointer: true, lengthCondition: "Size") //46
                .Field("CastGuardOsDeterminedFailureMode", typeof(long), pointer: true, lengthCondition: "Size") //47
            #endregion
            #region Windows SDK 10.0.22621+
                .Field("GuardMemcpyFunctionPointer", typeof(long), pointer: true, lengthCondition: "Size"); //48
            #endregion


            //10. LoadConfig\ImageLoadConfigDirectory.cs

            #region ImagePrologueDynamicRelocationHeader

            c.Struct("ImagePrologueDynamicRelocationHeader")
                .Field("PrologueByteCount", typeof(byte))
                .Field("PrologueBytes", typeof(byte), numElems: "PrologueByteCount");

            #endregion
            #region ImageSwitchTableBranchDynamicRelocation

            c.Struct("ImageSwitchTableBranchDynamicRelocation")
                .Field("flags", typeof(ushort), modifier: "private");

            #endregion
            #endregion
            #region 11. BoundImport
            #region ImageBoundForwarderRef

            c.Struct("ImageBoundForwarderRef")
                .Field("TimeDateStamp", typeof(uint))
                .Field("OffsetModuleName", typeof(ushort))
                .Field("Reserved", typeof(ushort));

            #endregion

            //11. BoundImport\ImageBoundImportDescriptor.cs

            #endregion
            #region 13. DelayImport

            //13. DelayImport\ImageDelayLoadDescriptor.cs
            c.Struct("ImageDelayLoadDescriptor")
                .Field("Attributes", typeof(int))
                .Field("DllNameRVA", typeof(int))
                .Field("ModuleHandleRVA", typeof(int))
                .Field("ImportAddressTableRVA", typeof(int))
                .Field("ImportNameTableRVA", typeof(int))
                .Field("BoundImportAddressTableRVA", typeof(int))
                .Field("UnloadInformationTableRVA", typeof(int))
                .Field("TimeDateStamp", typeof(int));

            #endregion
            #region 14. Cor20

            //14. Cor20\AppHostSignature.cs

            #region ClrEngineMetrics

            c.Struct("ClrEngineMetrics")
                .Field("Size", typeof(int))
                .Field("DbiVersion", typeof(int))
                .Field("ContinueStartupEvent", typeof(long), pointer: true);

            #endregion
            #region ImageCor20Header

            c.Struct("ImageCor20Header")
                .Field("ByteCount", typeof(int))
                .Field("MajorRuntimeVersion", typeof(ushort))
                .Field("MinorRuntimeVersion", typeof(ushort))
                .Field("Metadata", typeof(ImageDataDirectory))
                .Field("Flags", typeof(COMIMAGE_FLAGS), serializationType: typeof(uint))
                .Field("EntryPointTokenOrRVA", typeof(int))
                .Field("Resources", typeof(ImageDataDirectory))
                .Field("StrongNameSignature", typeof(ImageDataDirectory))
                .Field("CodeManagerTable", typeof(ImageDataDirectory))
                .Field("VTableFixups", typeof(ImageDataDirectory))
                .Field("ExportAddressTableJumps", typeof(ImageDataDirectory))
                .Field("ManagedNativeHeader", typeof(ImageDataDirectory));

            #endregion

            //14. Cor20\ImageCorILMethod.cs
            //14. Cor20\ImageCorILMethodSect.cs
            //14. Cor20\ImageCorILMethodSectEH.cs
            //14. Cor20\ImageCorILMethodSectEHClause.cs
            //14. Cor20\ImageCorVTableFixup.cs
            //14. Cor20\ImageDataDirectory`1.cs
            //14. Cor20\MetadataReader.cs
            //14. Cor20\RuntimeInfo.cs
            //14. Cor20\StorageHeader.cs
            //14. Cor20\StorageSignature.cs
            //14. Cor20\StorageStream.cs

            #region Ecma335

            //14. Cor20\Ecma335\BlobEntry.cs
            //14. Cor20\Ecma335\BlobHeap.cs
            //14. Cor20\Ecma335\CodedIndexTag.cs
            //14. Cor20\Ecma335\CompressedModelHeader.cs
            //14. Cor20\Ecma335\CompressedModelHeap.cs
            //14. Cor20\Ecma335\GuidHeap.cs
            //14. Cor20\Ecma335\PdbHeap.cs
            //14. Cor20\Ecma335\StringHeap.cs
            //14. Cor20\Ecma335\TableKind.cs
            //14. Cor20\Ecma335\TableMask.cs
            //14. Cor20\Ecma335\Table`1.cs
            //14. Cor20\Ecma335\UserString.cs
            //14. Cor20\Ecma335\UserStringHeap.cs
            //14. Cor20\Ecma335\Rows\AssemblyOSRow.cs
            //14. Cor20\Ecma335\Rows\AssemblyProcessorRow.cs
            //14. Cor20\Ecma335\Rows\AssemblyRefOSRow.cs
            //14. Cor20\Ecma335\Rows\AssemblyRefProcessorRow.cs
            //14. Cor20\Ecma335\Rows\AssemblyRefRow.cs
            //14. Cor20\Ecma335\Rows\AssemblyRow.cs
            //14. Cor20\Ecma335\Rows\ClassLayoutRow.cs
            //14. Cor20\Ecma335\Rows\ConstantRow.cs
            //14. Cor20\Ecma335\Rows\CustomAttributeRow.cs
            //14. Cor20\Ecma335\Rows\CustomDebugInformationRow.cs
            //14. Cor20\Ecma335\Rows\DeclSecurityRow.cs
            //14. Cor20\Ecma335\Rows\DocumentRow.cs
            //14. Cor20\Ecma335\Rows\EncLogRow.cs
            //14. Cor20\Ecma335\Rows\EncMapRow.cs
            //14. Cor20\Ecma335\Rows\EventMapRow.cs
            //14. Cor20\Ecma335\Rows\EventPtrRow.cs
            //14. Cor20\Ecma335\Rows\EventRow.cs
            //14. Cor20\Ecma335\Rows\ExportedTypeRow.cs
            //14. Cor20\Ecma335\Rows\FieldLayoutRow.cs
            //14. Cor20\Ecma335\Rows\FieldMarshalRow.cs
            //14. Cor20\Ecma335\Rows\FieldPtrRow.cs
            //14. Cor20\Ecma335\Rows\FieldRow.cs
            //14. Cor20\Ecma335\Rows\FieldRvaRow.cs
            //14. Cor20\Ecma335\Rows\FileRow.cs
            //14. Cor20\Ecma335\Rows\GenericParamConstraintRow.cs
            //14. Cor20\Ecma335\Rows\GenericParamRow.cs
            //14. Cor20\Ecma335\Rows\ImplMapRow.cs
            //14. Cor20\Ecma335\Rows\ImportScopeRow.cs
            //14. Cor20\Ecma335\Rows\InterfaceImplRow.cs
            //14. Cor20\Ecma335\Rows\LocalConstantRow.cs
            //14. Cor20\Ecma335\Rows\LocalScopeRow.cs
            //14. Cor20\Ecma335\Rows\LocalVariableRow.cs
            //14. Cor20\Ecma335\Rows\ManifestResourceRow.cs
            //14. Cor20\Ecma335\Rows\MemberRefRow.cs
            //14. Cor20\Ecma335\Rows\MethodDebugInformationRow.cs
            //14. Cor20\Ecma335\Rows\MethodDefRow.cs
            //14. Cor20\Ecma335\Rows\MethodImplRow.cs
            //14. Cor20\Ecma335\Rows\MethodPtrRow.cs
            //14. Cor20\Ecma335\Rows\MethodSemanticsRow.cs
            //14. Cor20\Ecma335\Rows\MethodSpecRow.cs
            //14. Cor20\Ecma335\Rows\ModuleRefRow.cs
            //14. Cor20\Ecma335\Rows\ModuleRow.cs
            //14. Cor20\Ecma335\Rows\NestedClassRow.cs
            //14. Cor20\Ecma335\Rows\ParamPtrRow.cs
            //14. Cor20\Ecma335\Rows\ParamRow.cs
            //14. Cor20\Ecma335\Rows\PropertyMapRow.cs
            //14. Cor20\Ecma335\Rows\PropertyPtrRow.cs
            //14. Cor20\Ecma335\Rows\PropertyRow.cs
            //14. Cor20\Ecma335\Rows\StandAloneSigRow.cs
            //14. Cor20\Ecma335\Rows\StateMachineMethodRow.cs
            //14. Cor20\Ecma335\Rows\TypeDefRow.cs
            //14. Cor20\Ecma335\Rows\TypeRefRow.cs
            //14. Cor20\Ecma335\Rows\TypeSpecRow.cs

            #endregion
            #region NativeAOT

            //14. Cor20\NativeAOT\DotNetRuntimeDebugHeader.cs

            #endregion
            #region R2R

            //14. Cor20\R2R\ReadyToRunCoreHeader.cs
            //14. Cor20\R2R\ReadyToRunHeader.cs

            #region ReadyToRunImportSection

            c.Struct("ReadyToRunImportSection")
                .Field("Section", typeof(ImageDataDirectory))
                .Field("Flags", typeof(ReadyToRunImportSectionFlags), serializationType: typeof(ushort))
                .Field("Type", typeof(ReadyToRunImportSectionType), serializationType: typeof(byte))
                .Field("EntrySize", typeof(byte))
                .Field("Signatures", typeof(int))
                .Field("AuxiliaryData", typeof(int));

            #endregion

            //14. Cor20\R2R\ReadyToRunSection.cs

            #endregion
            #endregion
            #region Symbols

            //Symbols\CoffSymbolTable.cs
            //Symbols\ImageAuxSymbol.cs
            //Symbols\ImageSymbol.cs

            #endregion

            var types = WriteTypes(c);

            var rewriter = new FastRewriter(types);
            rewriter.AddFastRegions();
        }

        [TestMethod]
        public void Generate_Primitive()
        {
            Test(
                v => v.Field("Foo", typeof(int)),
                "public int Foo => chunk.PeekInt32(0);"
            );
        }

        [TestMethod]
        public void Generate_Enum()
        {
            Test(
                v => v.Field("Foo", typeof(IMAGE_FILE_MACHINE), serializationType: typeof(ushort)),
                "public IMAGE_FILE_MACHINE Foo => (IMAGE_FILE_MACHINE) chunk.PeekUInt16(0);"
            );
        }

        [TestMethod]
        public void Generate_Array_FixedSize()
        {
            Test(
                v => v.Field("Foo", typeof(int), numElems: "4"),
                "public Span<int> Foo => chunk.PeekSpan<int>(0, 4);"
            );
        }

        [TestMethod]
        public void Generate_Array_DynamicSize()
        {
            Test(
                v => v.Field("Foo", typeof(int), numElems: "MyLength"),
                "public Span<int> Foo => chunk.PeekSpan<int>(0, MyLength);"
            );
        }

        [TestMethod]
        public void Generate_SurroundingArray_DynamicSize()
        {
            Test(
                v => v
                    .Field("Foo", typeof(int))
                    .Field("Bar", typeof(int), numElems: "MyLength")
                    .Field("Baz", typeof(int)),
                @"
public int Foo => chunk.PeekInt32(0);

public Span<int> Bar => chunk.PeekSpan<int>(4, MyLength);

public int Baz => chunk.PeekInt32(4 + (4 * MyLength));"
            );
        }

        [TestMethod]
        public void Generate_String_UnicodeFixedLength()
        {
            Test(
                v => v.Field("Foo", typeof(string), numElems: "10", stringType: StringType.UnicodeFixedLength),
                "public ReadOnlySpan<char> Foo => chunk.PeekUnicodeFixedLength(0);"
            );
        }

        [TestMethod]
        public void Generate_Guid()
        {
            Test(
                v => v.Field("Foo", typeof(Guid)),
                "public Guid Foo => chunk.PeekGuid(0);"
            );
        }

        [TestMethod]
        public void Generate_Pointer()
        {
            Test(
                v => v
                    .Field("Foo1", typeof(long), pointer: true)
                    .Field("Foo2", typeof(long), pointer: true)
                    .Field("Foo3", typeof(int))
                    .Field("Foo4", typeof(long), pointer: true),
                @"
public long Foo1 => chunk.PeekPointer(0);

public long Foo2 => chunk.PeekPointer(chunk.PointerSize);

public int Foo3 => chunk.PeekInt32(2 * chunk.PointerSize);

public long Foo4 => chunk.PeekPointer(4 + (2 * chunk.PointerSize));
"
            );
        }

        [TestMethod]
        public void Generate_Structure()
        {
            Test(
                v => v.Field("Foo", typeof(ImageDataDirectory)),
                "public ImageDataDirectory Foo => new ImageDataDirectory(chunk.Slice(0));"
            );
        }

        [TestMethod]
        public void Generate_x86Only()
        {
            Test(
                v => v
                    .Field("BaseOfData", typeof(int), x86Only: true)
                    .Field("ImageBase", typeof(long), pointer: true),
                @"
public int BaseOfData => chunk.Is32Bit ? chunk.PeekInt32(0) : 0;

public long ImageBase => chunk.Is32Bit ? chunk.PeekPointer(4) : chunk.PeekPointer(0);
"
            );
        }

        [TestMethod]
        public void Generate_Eager()
        {
            Test(
                v => v.Field("Foo", typeof(ImageDataDirectory), eager: true),
                @"public readonly ImageDataDirectory Foo;"
            );
        }

        [TestMethod]
        public void Generate_Setters()
        {
            /* For each file, get all properties.
             * For any property that has an arrow expression, if it contains any identifier that contains "chunk"
             * (ignoring case) then rewrite that getter as a getter/setter pair. Otherwise, if it contains a complex body,
             * add it to the failure list if it does not already have a manually implemented setter, and throw an exception
             * at the end about all properties requiring manually implemented setters */

            var files = FastRewriter.GetFilesOfInterest();

            foreach (var file in files)
            {
                if (!file.Contains("BigMsfHdr"))
                    continue;

                var text = File.ReadAllText(file);
                var syntaxTree = CSharpSyntaxTree.ParseText(text, CSharpParseOptions.Default.WithPreprocessorSymbols("PEFAST"));

                var root = syntaxTree.GetRoot();

                var properties = root.DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>().ToArray();

                var replacements = new Dictionary<PropertyDeclarationSyntax, PropertyDeclarationSyntax>();

                foreach (var property in properties)
                {
                    if (property.ExpressionBody != null)
                    {
                        //It has an expression body, so there's no setter. Is it a "chunk" getter?
                        var identifiers = property.ExpressionBody.DescendantNodes().OfType<NameSyntax>().ToArray();

                        if (identifiers.Any(a => a.ToString().Contains("chunk", StringComparison.OrdinalIgnoreCase)))
                        {
                            //Need to rewrite this property. There should be a singular peek method

                            var peeks = identifiers.Where(v => v.ToString().StartsWith("Peek")).ToArray();

                            if (peeks.Length == 0)
                                continue;

                            if (peeks.Length != 1)
                                throw new NotImplementedException("Don't know how to handle having multiple peeks");

                            var peek = peeks[0];

                            var invocation = property.ExpressionBody.DescendantNodes().OfType<InvocationExpressionSyntax>().First(v => ((MemberAccessExpressionSyntax) v.Expression).Name == peeks[0]);

                            var memberExpr = (MemberAccessExpressionSyntax) invocation.Expression;
                            var invocationArgs = invocation.ArgumentList.Arguments;

                            var setInvocation = invocation
                                .WithExpression(memberExpr.WithName(IdentifierName(peek.ToString().Replace("Peek", "Poke"))))
                                .WithArgumentList(ArgumentList(invocationArgs.Add(Argument(IdentifierName("value").WithLeadingTrivia(Whitespace(" "))))));

                            static AccessorDeclarationSyntax MakeAccessor(SyntaxKind kind, InvocationExpressionSyntax invocation)
                            {
                                return AccessorDeclaration(kind)
                                    .WithExpressionBody(
                                        ArrowExpressionClause(
                                            Token(SyntaxKind.EqualsGreaterThanToken)
                                                .WithLeadingTrivia(Whitespace(" "))
                                                .WithTrailingTrivia(Whitespace(" ")),
                                            invocation
                                        )
                                    ).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)).WithLeadingTrivia(Whitespace("            ")).WithTrailingTrivia(Whitespace(Environment.NewLine));
                            }

                            var accessors = AccessorList(
                                Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(Whitespace(Environment.NewLine + "        ")).WithTrailingTrivia(Whitespace(Environment.NewLine)),
                                List(new[]
                                {
                                MakeAccessor(SyntaxKind.GetAccessorDeclaration, invocation),
                                MakeAccessor(SyntaxKind.SetAccessorDeclaration, setInvocation)
                                }),
                                Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(Whitespace("        ")).WithTrailingTrivia(Whitespace(Environment.NewLine))
                            );

                            accessors = accessors.WithTrailingTrivia(property.ExpressionBody.GetTrailingTrivia());
                            var newProperty = property
                                .WithIdentifier(property.Identifier.WithTrailingTrivia(TriviaList()))
                                .WithExpressionBody(null)
                                .WithAccessorList(accessors)
                                .WithSemicolonToken(Token(SyntaxKind.None)).WithTrailingTrivia(property.SemicolonToken.TrailingTrivia);

                            replacements[property] = newProperty;
                        }
                    }
                    else
                    {
                        Debug.Assert(property.AccessorList != null);

                        //Check if its got a setter. If yes, ignore. Otherwise, check the getter to see if theres the word "chunk" or not
                        throw new NotImplementedException();
                    }
                }

                var val = root.ReplaceNodes(replacements.Keys, (a, b) => replacements[a]);

                var str = val.ToFullString();

                File.WriteAllText(file, str, Encoding.UTF8);
            }
        }

        private TypeDeclarationSyntax[] WriteTypes(GenerationContext ctx)
        {
            var results = new List<TypeDeclarationSyntax>();

            foreach (var item in ctx)
            {
                var writer = new TypeWriter();

                writer.WriteType(item);

                var str = writer.ToString();

                var syntax = (TypeDeclarationSyntax) SyntaxFactory.ParseMemberDeclaration(str);

                if (syntax == null)
                    throw new NotImplementedException();

                results.Add(syntax);
            }

            return results.ToArray();
        }

        private void Test(Action<StructBuilder> createField, string expectedFields)
        {
            var c = new GenerationContext();

            createField(c.Struct("Test"));

            //Add supplemental types
            c.Struct("ImageDataDirectory")
                .Field("VirtualAddress", typeof(int))
                .Field("Size", typeof(int));

            var builder = new StringBuilder();

            var expectedLines = expectedFields.Replace("\r", string.Empty).Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);

            var expected = builder.ToString();
            var actualStrs = WriteTypes(c).Select(v => v.ToFullString());
            var actual = actualStrs.First(v => v.Contains("Test"));

            var actualLines = actual.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Where(v => v.StartsWith("        public")).Select(v => v.Trim()).ToArray();

            Assert.AreEqual(expectedLines.Length, actualLines.Length, "Number of lines was incorrect");

            for (var i = 0; i < expectedLines.Length; i++)
                Assert.AreEqual(expectedLines[i], actualLines[i]);
        }
    }
}
