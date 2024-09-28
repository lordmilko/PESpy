using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ChaosLib;
using ChaosLib.Symbols;
using ClrDebug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.Tests.SymStore;
using PESpy.View;
namespace PESpy.Tests
{
#pragma warning disable HAA0101 // Array allocation for params parameter
    [TestClass]
    public class PEFileTests
    {
        private static readonly object IgnoreValue = new object();

        #region DOS Header

        [TestMethod]
        public void ImageDosHeader_Test()
        {
            TestStruct<ImageDosHeader>(
                v => v.Magic == ImageDosHeader.DosSignature,
                v => v.BytesOnLastPageOfFile == 144,
                v => v.PagesInFile == 3,
                v => v.Relocations == 0,
                v => v.SizeOfHeaderInParagraphs == 4,
                v => v.MinimumExtraParagraphsNeeded == 0,
                v => v.MaximumExtraParagraphsNeeded == 65535,
                v => v.InitialRelativeSSValue == 0,
                v => v.InitialSPValue == 184,
                v => v.Checksum == 0,
                v => v.InitialIPValue == 0,
                v => v.InitialRelativeCSValue == 0,
                v => v.FileAddressOfRelocationTable == 64,
                v => v.OverlayNumber == 0,
                v => v.ReservedWords == new short[] { 0, 0, 0, 0 },
                v => v.OEMIdentifier == 0,
                v => v.OEMInformation == 0,
                v => v.ReservedWords2 == new short[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                v => v.FileAddressOfNewExeHeader == 224
            );

            TestView<ImageDosHeader>(
                v => v.VerifyStruct(
                    name: "IMAGE_DOS_HEADER", offset: 0, size: 64,
                    c => c.VerifyField(name: "e_magic", value: (short) 23117),
                    c => c.VerifyField(name: "e_cblp", value: (short) 144),
                    c => c.VerifyField(name: "e_cp", value: (short) 3),
                    c => c.VerifyField(name: "e_crlc", value: (short) 0),
                    c => c.VerifyField(name: "e_cparhdr", value: (short) 4),
                    c => c.VerifyField(name: "e_minalloc", value: (ushort) 0),
                    c => c.VerifyField(name: "e_maxalloc", value: (ushort) 65535),
                    c => c.VerifyField(name: "e_ss", value: (short) 0),
                    c => c.VerifyField(name: "e_sp", value: (short) 184),
                    c => c.VerifyField(name: "e_csum", value: (short) 0),
                    c => c.VerifyField(name: "e_ip", value: (short) 0),
                    c => c.VerifyField(name: "e_cs", value: (short) 0),
                    c => c.VerifyField(name: "e_lfarlc", value: (short) 64),
                    c => c.VerifyField(name: "e_ovno", value: (short) 0),
                    c => c.VerifyField(name: "e_res", value: new short[] { 0, 0, 0, 0 }),
                    c => c.VerifyField(name: "e_oemid", value: (short) 0),
                    c => c.VerifyField(name: "e_oeminfo", value: (short) 0),
                    c => c.VerifyField(name: "e_res2", value: new short[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }),
                    c => c.VerifyField(name: "e_lfanew", value: 224)
                )
            );
        [TestMethod]
        public void ImageNtHeaders_Test()
        {
            TestStruct<ImageNtHeaders>(
                v => v.Signature == 17744,
                v => v.FileHeader == IgnoreValue,
                v => v.OptionalHeader == IgnoreValue
            );

            TestView<ImageNtHeaders>(
                v => v.VerifyStruct(
                    name: "IMAGE_NT_HEADERS", offset: 224, size: 264,
                    c => c.VerifyField(name: "Signature", value: 17744),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_FILE_HEADER", offset: 228, size: 20),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_OPTIONAL_HEADER", offset: 248, size: 240)
                )
            );
        }

        [TestMethod]
        public void ImageFileHeader_Test()
        {
            TestStruct<ImageFileHeader>(
                v => v.Machine == IMAGE_FILE_MACHINE.AMD64,
                v => v.NumberOfSections == 11,
                v => v.TimeDateStamp == 3169667970,
                v => v.PointerToSymbolTable == 0,
                v => v.NumberOfSymbols == 0,
                v => v.SizeOfOptionalHeader == 240,
                v => v.Characteristics == (ImageFile.ExecutableImage | ImageFile.LargeAddressAware | ImageFile.Dll)
            );

            TestView<ImageFileHeader>(
                v => v.VerifyStruct(
                    name: "IMAGE_FILE_HEADER", offset: 228, size: 20,
                    c => c.VerifyField(name: "Machine", value: IMAGE_FILE_MACHINE.AMD64),
                    c => c.VerifyField(name: "NumberOfSections", value: (short) 11),
                    c => c.VerifyField(name: "TimeDateStamp", value: (uint) 3169667970),
                    c => c.VerifyField(name: "PointerToSymbolTable", value: 0),
                    c => c.VerifyField(name: "NumberOfSymbols", value: 0),
                    c => c.VerifyField(name: "SizeOfOptionalHeader", value: (short) 240),
                    c => c.VerifyField(name: "Characteristics", value: ImageFile.ExecutableImage | ImageFile.LargeAddressAware | ImageFile.Dll)
                )
            );
        }

        [TestMethod]
        public void ImageOptionalHeader_Test()
        {
            TestStruct<ImageOptionalHeader>(
                v => v.Magic == PEMagic.PE32Plus,
                v => v.MajorLinkerVersion == 14,
                v => v.MinorLinkerVersion == 30,
                v => v.SizeOfCode == 1249280,
                v => v.SizeOfInitializedData == 937984,
                v => v.SizeOfUninitializedData == 0,
                v => v.AddressOfEntryPoint == 0,
                v => v.BaseOfCode == 4096,
                //v => v.BaseOfData == 0,
                v => v.ImageBase == 6442450944,
                v => v.SectionAlignment == 4096,
                v => v.FileAlignment == 4096,
                v => v.MajorOperatingSystemVersion == 10,
                v => v.MinorOperatingSystemVersion == 0,
                v => v.MajorImageVersion == 10,
                v => v.MinorImageVersion == 0,
                v => v.MajorSubsystemVersion == 10,
                v => v.MinorSubsystemVersion == 0,
                v => v.Win32VersionValue == 0,
                v => v.SizeOfImage == 2191360,
                v => v.SizeOfHeaders == 4096,
                v => v.CheckSum == 2219814,
                v => v.Subsystem == ImageSubsystem.WindowsCui,
                v => v.DllCharacteristics == (ImageDllCharacteristics.HighEntropyVirtualAddressSpace | ImageDllCharacteristics.DynamicBase | ImageDllCharacteristics.NxCompatible | ImageDllCharacteristics.GuardCF),
                v => v.SizeOfStackReserve == 262144,
                v => v.SizeOfStackCommit == 4096,
                v => v.SizeOfHeapReserve == 1048576,
                v => v.SizeOfHeapCommit == 4096,
                v => v.LoaderFlags == (ImageLoaderFlags) 0,
                v => v.NumberOfRvaAndSizes == 16,
                v => (object) v.ExportTableDirectory             == IgnoreValue,
                v => (object) v.ImportTableDirectory             == IgnoreValue,
                v => (object) v.ResourceTableDirectory           == IgnoreValue,
                v => (object) v.ExceptionTableDirectory          == IgnoreValue,
                v => (object) v.SecurityTableDirectory           == IgnoreValue,
                v => (object) v.BaseRelocationTableDirectory     == IgnoreValue,
                v => (object) v.DebugTableDirectory              == IgnoreValue,
                v => (object) v.CopyrightTableDirectory          == IgnoreValue,
                v => (object) v.GlobalPointerTableDirectory      == IgnoreValue,
                v => (object) v.ThreadLocalStorageTableDirectory == IgnoreValue,
                v => (object) v.LoadConfigTableDirectory         == IgnoreValue,
                v => (object) v.BoundImportTableDirectory        == IgnoreValue,
                v => (object) v.ImportAddressTableDirectory      == IgnoreValue,
                v => (object) v.DelayImportTableDirectory        == IgnoreValue,
                v => (object) v.CorHeaderTableDirectory          == IgnoreValue,
                v => (object) v.NullDirectory                    == IgnoreValue
            );

            TestView<ImageOptionalHeader>(
                v => v.VerifyStruct(
                    name: "IMAGE_OPTIONAL_HEADER", offset: 248, size: 240,
                    c => c.VerifyField(name: "Magic", value: PEMagic.PE32Plus),
                    c => c.VerifyField(name: "MajorLinkerVersion", value: (byte) 14),
                    c => c.VerifyField(name: "MinorLinkerVersion", value: (byte) 30),
                    c => c.VerifyField(name: "SizeOfCode", value: 1249280),
                    c => c.VerifyField(name: "SizeOfInitializedData", value: 937984),
                    c => c.VerifyField(name: "SizeOfUninitializedData", value: 0),
                    c => c.VerifyField(name: "AddressOfEntryPoint", value: 0),
                    c => c.VerifyField(name: "BaseOfCode", value: 4096),
                    //c => c.VerifyField(name: "BaseOfData", value: 0),
                    c => c.VerifyField(name: "ImageBase", value: (long) 6442450944),
                    c => c.VerifyField(name: "SectionAlignment", value: 4096),
                    c => c.VerifyField(name: "FileAlignment", value: 4096),
                    c => c.VerifyField(name: "MajorOperatingSystemVersion", value: (ushort) 10),
                    c => c.VerifyField(name: "MinorOperatingSystemVersion", value: (ushort) 0),
                    c => c.VerifyField(name: "MajorImageVersion", value: (ushort) 10),
                    c => c.VerifyField(name: "MinorImageVersion", value: (ushort) 0),
                    c => c.VerifyField(name: "MajorSubsystemVersion", value: (ushort) 10),
                    c => c.VerifyField(name: "MinorSubsystemVersion", value: (ushort) 0),
                    c => c.VerifyField(name: "Win32VersionValue", value: 0),
                    c => c.VerifyField(name: "SizeOfImage", value: 2191360),
                    c => c.VerifyField(name: "SizeOfHeaders", value: 4096),
                    c => c.VerifyField(name: "CheckSum", value: (uint) 2219814),
                    c => c.VerifyField(name: "Subsystem", value: ImageSubsystem.WindowsCui),
                    c => c.VerifyField(name: "DllCharacteristics", value: (ImageDllCharacteristics.HighEntropyVirtualAddressSpace | ImageDllCharacteristics.DynamicBase | ImageDllCharacteristics.NxCompatible | ImageDllCharacteristics.GuardCF)),
                    c => c.VerifyField(name: "SizeOfStackReserve", value: (ulong) 262144),
                    c => c.VerifyField(name: "SizeOfStackCommit", value: (ulong) 4096),
                    c => c.VerifyField(name: "SizeOfHeapReserve", value: (ulong) 1048576),
                    c => c.VerifyField(name: "SizeOfHeapCommit", value: (ulong) 4096),
                    c => c.VerifyField(name: "LoaderFlags", value: (ImageLoaderFlags) 0),
                    c => c.VerifyField(name: "NumberOfRvaAndSizes", value: 16),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT (0)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT (1)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE (2)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_EXCEPTION (3)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY (4)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC (5)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG (6)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_COPYRIGHT / IMAGE_DIRECTORY_ENTRY_ARCHITECTURE (7)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_GLOBALPTR (8)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_TLS (9)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG (10)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT (11)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_IAT (12)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT (13)]"),
                    c => c.VerifyFieldIgnoreValue(name: "DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR (14)]"),
                    c => c.VerifyFieldIgnoreValue(name: "NullDirectory")
                )
            );
        [TestMethod]
        public void ImageDataDirectory_Test()
        {
            TestStruct<ImageDataDirectory>(
                v => v.VirtualAddress == 1489728,
                v => v.Size == 79317
            );

            TestView<ImageDataDirectory>(
                v => v.VerifyStruct(
                    name: "IMAGE_DATA_DIRECTORY", offset: 360, size: 8,
                    c => c.VerifyField(name: "VirtualAddress", value: 1489728),
                    c => c.VerifyField(name: "Size", value: 79317)
                )
            );
        }

        #endregion
        #region Section Headers

        [TestMethod]
        public void ImageSectionHeaders_Test()
        {
            TestStruct<ImageSectionHeader>(
                v => v.Name == ".text",
                v => v.VirtualSize == 1233454,
                v => v.VirtualAddress == 4096,
                v => v.SizeOfRawData == 1236992,
                v => v.PointerToRawData == 4096,
                v => v.PointerToRelocations == 0,
                v => v.PointerToLineNumbers == 0,
                v => v.NumberOfRelocations == 0,
                v => v.NumberOfLineNumbers == 0,
                v => v.Characteristics == (IMAGE_SCN.CNT_CODE | IMAGE_SCN.MEM_EXECUTE | IMAGE_SCN.MEM_READ)
            );

            TestView<ImageSectionHeader>(
                v => v.VerifyStruct(
                    name: "IMAGE_SECTION_HEADER", offset: 488, size: 40,
                    c => c.VerifyField(name: "Name", value: ".text"),
                    c => c.VerifyField(name: "VirtualSize", value: 1233454),
                    c => c.VerifyField(name: "VirtualAddress", value: 4096),
                    c => c.VerifyField(name: "SizeOfRawData", value: 1236992),
                    c => c.VerifyField(name: "PointerToRawData", value: 4096),
                    c => c.VerifyField(name: "PointerToRelocations", value: 0),
                    c => c.VerifyField(name: "PointerToLineNumbers", value: 0),
                    c => c.VerifyField(name: "NumberOfRelocations", value: (short) 0),
                    c => c.VerifyField(name: "NumberOfLineNumbers", value: (short) 0),
                    c => c.VerifyField(name: "Characteristics", value: (IMAGE_SCN.CNT_CODE | IMAGE_SCN.MEM_EXECUTE | IMAGE_SCN.MEM_READ))
                )
            );
        }

        #endregion
        #region Export Table (0)

        [TestMethod]
        public void ImageExportDirectory_Test()
        {
            TestStruct<ImageExportDirectory>(
                v => v.Characteristics == 0,
                v => v.TimeDateStamp == 3169667970,
                v => v.MajorVersion == 0,
                v => v.MinorVersion == 0,
                v => v.Name.ListedOffset == 1514622,
                v => v.Base == 8,
                v => v.NumberOfFunctions == 2486,
                v => v.NumberOfNames == 2485,
                v => v.AddressOfFunctions.ListedOffset == 1489768,
                v => v.AddressOfNames.ListedOffset == 1499712,
                v => v.AddressOfNameOrdinals.ListedOffset == 1509652,
                v => v.Exports == IgnoreValue
            );

            TestView<ImageExportDirectory>(
                new Action<IView>[]
                {
                    v => v.VerifyStruct(
                        name: "IMAGE_EXPORT_DIRECTORY", offset: 1489728, size: 40,
                        c => c.VerifyField(name: "Characteristics", value: 0),
                        c => c.VerifyField(name: "TimeDateStamp", value: (uint) 3169667970),
                        c => c.VerifyField(name: "MajorVersion", value: (ushort) 0),
                        c => c.VerifyField(name: "MinorVersion", value: (ushort) 0),
                        c => c.VerifyField(name: "Name", value: 1514622),
                        c => c.VerifyField(name: "Base", value: 8),
                        c => c.VerifyField(name: "NumberOfFunctions", value: 2486),
                        c => c.VerifyField(name: "NumberOfNames", value: 2485),
                        c => c.VerifyField(name: "AddressOfFunctions", value: 1489768),
                        c => c.VerifyField(name: "AddressOfNames", value: 1499712),
                        c => c.VerifyField(name: "AddressOfNameOrdinals", value: 1509652)
                    )
                },
                false
            );
        [TestMethod]
        public void ImageImportDescriptor_Test()
        {
            TestStruct<ImageImportDescriptor>(
                v => v.OriginalFirstThunk.ListedOffset == 237080,
                v => v.TimeDateStamp == 0,
                v => v.ForwarderChain == 0,
                v => v.Name.ListedOffset == 237918,
                v => v.FirstThunk.ListedOffset == 196224,
                v => v.ImportLookupTable.ListedOffset == 237080,
                v => v.ImportAddressTable.ListedOffset == 196224
            );

            TestView<ImageImportDescriptor>(
                new Action<IView>[]
                {
                    v => { }, //ImportAddressTable region

                    v => v.VerifyStruct(
                        name: "IMAGE_IMPORT_DESCRIPTOR ucrtbase_enclave.dll", offset: 236888, size: 20,
                        c => c.VerifyField(name: "OriginalFirstThunk", value: 237080),
                        c => c.VerifyField(name: "TimeDateStamp", value: (uint) 0),
                        c => c.VerifyField(name: "ForwarderChain", value: 0),
                        c => c.VerifyField(name: "Name", value: 237918),
                        c => c.VerifyField(name: "FirstThunk", value: 196224)
                    )
                },
                false
            );
        }

        [TestMethod]
        public void VsVersionInfo_Test()
        {
            TestStruct<VsVersionInfo>(
                v => v.Length == 892,
                v => v.ValueLength == 52,
                v => v.Type == 0,
                v => v.Key == "VS_VERSION_INFO",
                v => v.Padding1 == 0,
                v => v.Value == IgnoreValue,
                v => v.Padding2 == 0
            );

            TestView<VsVersionInfo>(
                v => v.VerifyStruct(
                    name: "VS_VERSIONINFO", offset: 1671408, size: 892,
                    c => c.VerifyField(name: "wLength", value: (short) 892),
                    c => c.VerifyField(name: "wValueLength", value: (short) 52),
                    c => c.VerifyField(name: "wType", value: (short) 0),
                    c => c.VerifyField(name: "szKey", value: "VS_VERSION_INFO"),
                    c => c.VerifyField(name: "Padding1", value: (short) 0),
                    c => c.VerifyStructIgnoreChildren(name: "VS_FIXEDFILEINFO", offset: 1671448, size: 52),
                    c => c.VerifyStructIgnoreChildren(name: "StringFileInfo", offset: 1671500, size: 732),
                    c => c.VerifyStructIgnoreChildren(name: "VarFileInfo", offset: 1672232, size: 68)
                )
            );
        }

        [TestMethod]
        public void VsFixedFileInfo_Test()
        {
            TestStruct<VsFixedFileInfo>(
                v => v.Signature == 4277077181,
                v => v.StrucVersion == 65536,
                v => v.FileVersionMS == 655360,
                v => v.FileVersionMinor == 0,
                v => v.FileVersionMajor == 10,
                v => v.FileVersionLS == 1482492571,
                v => v.FileVersionRevision == 2715,
                v => v.FileVersionBuild == 22621,
                v => v.ProductVersionMS == 655360,
                v => v.ProductVersionMinor == 0,
                v => v.ProductVersionMajor == 10,
                v => v.ProductVersionLS == 1482492571,
                v => v.ProductVersionRevision == 2715,
                v => v.ProductVersionBuild == 22621,
                v => v.FileFlagsMask == 63,
                v => v.FileFlags == (VS_FF) 0,
                v => v.FileOS == (VOS.WINDOWS32 | VOS.NT),
                v => v.FileType == 2,
                v => v.FileSubtype == 0,
                v => v.FileDateMS == 0,
                v => v.FileDateLS == 0
            );

            TestView<VsFixedFileInfo>(
                v => v.VerifyStruct(
                    name: "VS_FIXEDFILEINFO", offset: 1671448, size: 52,
                    c => c.VerifyField(name: "dwSignature", value: (uint) 4277077181),
                    c => c.VerifyField(name: "dwStrucVersion", value: (uint) 65536),
                    c => c.VerifyField(name: "dwFileVersionMS", value: 655360),
                    c => c.VerifyField(name: "dwFileVersionLS", value: 1482492571),
                    c => c.VerifyField(name: "dwProductVersionMS", value: 655360),
                    c => c.VerifyField(name: "dwProductVersionLS", value: 1482492571),
                    c => c.VerifyField(name: "dwFileFlagsMask", value: (uint) 63),
                    c => c.VerifyField(name: "dwFileFlags", value: (VS_FF) 0),
                    c => c.VerifyField(name: "dwFileOS", value: (VOS.WINDOWS32 | VOS.NT)),
                    c => c.VerifyField(name: "dwFileType", value: (uint) 2),
                    c => c.VerifyField(name: "dwFileSubtype", value: (uint) 0),
                    c => c.VerifyField(name: "dwFileDateMS", value: (uint) 0),
                    c => c.VerifyField(name: "dwFileDateLS", value: (uint) 0)
                )
            );
        }

        [TestMethod]
        public void StringFileInfo_Test()
        {
            TestStruct<VsVersionInfo.StringFileInfo>(
                v => v.Length == 732,
                v => v.ValueLength == 0,
                v => v.Type == 1,
                v => v.Key == "StringFileInfo",
                v => v.Padding == 0,
                v => v.Children == IgnoreValue
            );

            TestView<VsVersionInfo.StringFileInfo>(
                v => v.VerifyStruct(
                    name: "StringFileInfo", offset: 1671500, size: 732,
                    c => c.VerifyField(name: "wLength", value: (short) 732),
                    c => c.VerifyField(name: "wValueLength", value: (short) 0),
                    c => c.VerifyField(name: "wType", value: (short) 1),
                    c => c.VerifyField(name: "szKey", value: "StringFileInfo"),
                    c => c.VerifyStructIgnoreChildren(name: "StringTable", offset: 1671536, size: 696)
                )
            );
        }

        [TestMethod]
        public void StringTable_Test()
        {
            TestStruct<VsVersionInfo.StringTable>(
                v => v.Length == 696,
                v => v.ValueLength == 0,
                v => v.Type == 1,
                v => v.Key == "040904B0",
                v => v.Padding == 0,
                v => v.Children == IgnoreValue
            );

            TestView<VsVersionInfo.StringTable>(
                v => v.VerifyStruct(
                    name: "StringTable", offset: 1671536, size: 696,
                    c => c.VerifyField(name: "wLength", value: (short) 696),
                    c => c.VerifyField(name: "wValueLength", value: (short) 0),
                    c => c.VerifyField(name: "wType", value: (short) 1),
                    c => c.VerifyField(name: "szKey", value: "040904B0"),
                    c => c.VerifyStructIgnoreChildren(name: "String", offset: 1671560, size: 76),
                    c => c.VerifyStructIgnoreChildren(name: "String", offset: 1671636, size: 66),
                    c => c.VerifyByteBlob(offset: 1671702, value: new byte[] {0, 0}),
                    c => c.VerifyStructIgnoreChildren(name: "String", offset: 1671704, size: 110),
                    c => c.VerifyByteBlob(offset: 1671814, value: new byte[] {0, 0}),
                    c => c.VerifyStructIgnoreChildren(name: "String", offset: 1671816, size: 52),
                    c => c.VerifyStructIgnoreChildren(name: "String", offset: 1671868, size: 128),
                    c => c.VerifyStructIgnoreChildren(name: "String", offset: 1671996, size: 60),
                    c => c.VerifyStructIgnoreChildren(name: "String", offset: 1672056, size: 106),
                    c => c.VerifyByteBlob(offset: 1672162, value: new byte[] {0, 0}),
                    c => c.VerifyStructIgnoreChildren(name: "String", offset: 1672164, size: 68)
                )
            );
        }

        [TestMethod]
        public void String_Test()
        {
            TestStruct<VsVersionInfo.String>(
                v => v.Length == 76,
                v => v.ValueLength == 22,
                v => v.Type == 1,
                v => v.Key == "CompanyName",
                v => v.Padding == 0,
                v => v.Value == "Microsoft Corporation"
            );

            TestView<VsVersionInfo.String>(
                v => v.VerifyStruct(
                    name: "String", offset: 1671560, size: 76,
                    c => c.VerifyField(name: "wLength", value: (short) 76),
                    c => c.VerifyField(name: "wValueLength", value: (short) 22),
                    c => c.VerifyField(name: "wType", value: (short) 1),
                    c => c.VerifyField(name: "szKey", value: "CompanyName"),
                    c => c.VerifyField(name: "Padding", value: (short) 0),
                    c => c.VerifyField(name: "Value", value: "Microsoft Corporation")
                )
            );
        }

        [TestMethod]
        public void VarFileInfo_Test()
        {
            TestStruct<VsVersionInfo.VarFileInfo>(
                v => v.Length == 68,
                v => v.ValueLength == 0,
                v => v.Type == 1,
                v => v.Key == "VarFileInfo",
                v => v.Padding == 0,
                v => v.Children == IgnoreValue
            );

            TestView<VsVersionInfo.VarFileInfo>(
                v => v.VerifyStruct(
                    name: "VarFileInfo", offset: 1672232, size: 68,
                    c => c.VerifyField(name: "wLength", value: (short) 68),
                    c => c.VerifyField(name: "wValueLength", value: (short) 0),
                    c => c.VerifyField(name: "wType", value: (short) 1),
                    c => c.VerifyField(name: "szKey", value: "VarFileInfo"),
                    c => c.VerifyField(name: "Padding", value: (short) 0),
                    c => c.VerifyStructIgnoreChildren(name: "Var", offset: 1672264, size: 36)
                )
            );
        }

        [TestMethod]
        public void Var_Test()
        {
            TestStruct<VsVersionInfo.Var>(
                v => v.Length == 36,
                v => v.ValueLength == 4,
                v => v.Type == 0,
                v => v.Key == "Translation",
                v => v.Padding == 0,
                v => v.Value == new int[] { 78644233 }
            );

            TestView<VsVersionInfo.Var>(
                v => v.VerifyStruct(
                    name: "Var", offset: 1672264, size: 36,
                    c => c.VerifyField(name: "wLength", value: (short) 36),
                    c => c.VerifyField(name: "wValueLength", value: (short) 4),
                    c => c.VerifyField(name: "wType", value: (short) 0),
                    c => c.VerifyField(name: "szKey", value: "Translation"),
                    c => c.VerifyField(name: "Padding", value: (short) 0),
                    c => c.VerifyField(name: "Value", value: new int[] { 78644233 })
                )
            );
        }
        }
        }
        }
        }
        }
        }
        [TestMethod]
        public void ImageBaseRelocation_Test()
        {
            TestStruct<ImageBaseRelocation>(
                v => v.VirtualAddress == 1699840,
                v => v.SizeOfBlock == 16,
                v => v.Entries == IgnoreValue
            );

            TestView<ImageBaseRelocation>(
                v => v.VerifyStruct(
                    name: "IMAGE_BASE_RELOCATION", offset: 2155852, size: 16,
                    c => c.VerifyField(name: "VirtualAddress", value: 1699840),
                    c => c.VerifyField(name: "SizeOfBlock", value: 16),

                    c => c.VerifyStruct(
                        name: "Entry", offset: 2155860, size: 2,
                        b => b.VerifyBitField(name: "Type", value: ImageRelBased.Dir64, bits: 4),
                        b => b.VerifyBitField(name: "Offset", value: (short) 0, bits: 12)
                    ),
                    c => c.VerifyStruct(
                        name: "Entry", offset: 2155862, size: 2,
                        b => b.VerifyBitField(name: "Type", value: ImageRelBased.Dir64, bits: 4),
                        b => b.VerifyBitField(name: "Offset", value: (short) 8, bits: 12)
                    ),
                    c => c.VerifyStruct(
                        name: "Entry", offset: 2155864, size: 2,
                        b => b.VerifyBitField(name: "Type", value: ImageRelBased.Dir64, bits: 4),
                        b => b.VerifyBitField(name: "Offset", value: (short) 16, bits: 12)
                    ),
                    c => c.VerifyStruct(
                        name: "Entry", offset: 2155866, size: 2,
                        b => b.VerifyBitField(name: "Type", value: ImageRelBased.Dir64, bits: 4),
                        b => b.VerifyBitField(name: "Offset", value: (short) 24, bits: 12)
                    )
                )
            );
        }

        #endregion
        #region Debug Table (6)

        [TestMethod]
        public void ImageDebugDirectory_Test()
        {
            TestStruct<ImageDebugDirectory>(
                v => v.Characteristics == 0,
                v => v.TimeDateStamp == 3169667970,
                v => v.MajorVersion == 0,
                v => v.MinorVersion == 0,
                v => v.Type == ImageDebugType.CodeView,
                v => v.SizeOfData == 34,
                v => v.AddressOfRawData == 1423344,
                v => v.PointerToRawData == 1423344
            );
        }

        #region Types
        #region Coff (1)

        [TestMethod]
        public void ImageDebugDirectory_Coff_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        #region CodeView (2)

        [TestMethod]
        public void ImageDebugDirectory_CodeView_RSDSI_Test()
        {
            TestStruct<RSDSI>(
                v => v.Signature == 1396986706,
                v => v.Guid == new Guid("58a282c2-4aee-7e03-a8cf-8cb0a782ce0c"),
                v => v.Age == 1,
                v => v.Path == "ntdll.pdb"
            );

            TestView<RSDSI>(
                v => v.VerifyStruct(
                    name: "RSDSI", offset: 1423344, size: 34,
                    c => c.VerifyField(name: "dwSig", value: 1396986706),
                    c => c.VerifyField(name: "guidSig", value: new Guid("58a282c2-4aee-7e03-a8cf-8cb0a782ce0c")),
                    c => c.VerifyField(name: "age", value: 1),
                    c => c.VerifyField(name: "szPdb", value: "ntdll.pdb")
                )
            );
        }

        [TestMethod]
        public void ImageDebugDirectory_CodeView_NB10I_Test()
        {
            TestStruct<NB10I>(
                v => v.Signature == 808534606,
                v => v.dwOffset == 0,
                v => v.PdbSignature == 988769516,
                v => v.Age == 1,
                v => v.Path == "crtdll.pdb"
            );

            TestView<NB10I>(
                v => v.VerifyStruct(
                    name: "NB10I", offset: 148992, size: 27,
                    c => c.VerifyField(name: "dwSig", value: 808534606),
                    c => c.VerifyField(name: "dwOffset", value: 0),
                    c => c.VerifyField(name: "sig", value: 988769516),
                    c => c.VerifyField(name: "age", value: 1),
                    c => c.VerifyField(name: "szPdb", value: "crtdll.pdb")
                )
            );
        }

        #endregion
        #region FPO (3)

        [TestMethod]
        public void ImageDebugDirectory_FpoData_Test()
        {
            TestStruct<FpoData>(
                v => v.OffStart == 4096,
                v => v.ProcSize == 29,
                v => v.Locals == 0,
                v => v.Params == 1,
                v => v.cbProlog == 0,
                v => v.cbRegs == 0,
                v => v.fHasSEH == false,
                v => v.fUseBP == false,
                v => v.reserved == false,
                v => v.cbFrame == FrameType.FPO
            );

            TestView<FpoData>(
                v => v.VerifyStruct(
                    name: "FPO_DATA", offset: 26112, size: 16,
                    c => c.VerifyField(name: "ulOffStart", value: 4096),
                    c => c.VerifyField(name: "cbProcSize", value: 29),
                    c => c.VerifyField(name: "cdwLocals", value: 0),
                    c => c.VerifyField(name: "cdwParams", value: (short)1),
                    b => b.VerifyBitField(name: "cbProlog", value: (byte)0, bits: 8),
                    b => b.VerifyBitField(name: "cbRegs", value: (byte)0, bits: 3),
                    b => b.VerifyBitField(name: "fHasSEH", value: (byte)0, bits: 1),
                    b => b.VerifyBitField(name: "fUseBP", value: (byte)0, bits: 1),
                    b => b.VerifyBitField(name: "reserved", value: (byte)0, bits: 1),
                    b => b.VerifyBitField(name: "cbFrame", value: FrameType.FPO, bits: 2)
                )
            );
        }

        #endregion
        #region Misc (4)

        [TestMethod]
        public void ImageDebugDirectory_Misc_Test()
        {
            TestStruct<ImageDebugMisc>(
                v => v.DataType == ImageDebugMiscType.ExeName,
                v => v.Length == 272,
                v => v.Unicode == false,
                v => v.Data == "mfc40_opt.DBG"
            );

            TestView<ImageDebugMisc>(
                v => v.VerifyStruct(
                    name: "IMAGE_DEBUG_MISC", offset: 924672, size: 26,
                    c => c.VerifyField(name: "DataType", value: ImageDebugMiscType.ExeName),
                    c => c.VerifyField(name: "Length", value: 272),
                    c => c.VerifyField(name: "Unicode", value: (byte)0),
                    c => c.VerifyField(name: "Reserved", value: new byte[] { 0, 0, 0 }),
                    c => c.VerifyField(name: "Data", value: "mfc40_opt.DBG")
                )
            );
        }

        #endregion
        #region Exception (5)

        [TestMethod]
        public void ImageDebugDirectory_Exception_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        #region Fixup (6)

        [TestMethod]
        public void ImageDebugDirectory_Fixup_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        #region OmapToSrc (7)

        [TestMethod]
        public void ImageDebugDirectory_OmapToSrc_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        #region OmapFromSrc (8)

        [TestMethod]
        public void ImageDebugDirectory_OmapFromSrc_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        #region Borland (9)

        [TestMethod]
        public void ImageDebugDirectory_Borland_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        #region BBT (10)

        [TestMethod]
        public void ImageDebugDirectory_BBT_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        #region Clsid (11)

        [TestMethod]
        public void ImageDebugDirectory_Clsid_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        }

        #endregion
        #region ILTCG (14)

        [TestMethod]
        public void ImageDebugDirectory_ILTCG_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        #region MPX (15)

        [TestMethod]
        public void ImageDebugDirectory_MPX_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        #region ILTCG (14)

        [TestMethod]
        public void ImageDebugDirectory_ILTCG_Test()
        {
            Assert.Inconclusive();
        }
        #region R2RPerfMap (21)

        [TestMethod]
        public void ImageDebugDirectory_R2RPerfMap_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        #endregion
        #endregion
        #region Copyright Table (7)

        #endregion
        #region Global Pointer Table (8)

        #endregion
        #region Thread Local Storage Table (9)

        [TestMethod]
        public void ImageTlsDirectory_Test()
        {
            TestStruct<ImageTlsDirectory>(
                v => v.StartAddressOfRawData == 6442675092,
                v => v.EndAddressOfRawData == 6442675100,
                v => v.AddressOfIndex == 6442697760,
                v => v.AddressOfCallBacks == 6442651784,
                v => v.SizeOfZeroFill == 0,
                v => v.Characteristics == IMAGE_SCN_ALIGN.ALIGN_4BYTES
            );

            TestView<ImageTlsDirectory>(
                v => v.VerifyStruct(
                    name: "IMAGE_TLS_DIRECTORY", offset: 196072, size: 40,
                    c => c.VerifyField(name: "StartAddressOfRawData", value: (long) 6442675092),
                    c => c.VerifyField(name: "EndAddressOfRawData", value: (long) 6442675100),
                    c => c.VerifyField(name: "AddressOfIndex", value: (long) 6442697760),
                    c => c.VerifyField(name: "AddressOfCallBacks", value: (long) 6442651784),
                    c => c.VerifyField(name: "SizeOfZeroFill", value: 0),
                    c => c.VerifyField(name: "Characteristics", value: IMAGE_SCN_ALIGN.ALIGN_4BYTES)
                )
            );
        }

        #endregion
        #region Load Config Table (10)

        [TestMethod]
        public void ImageLoadConfigDirectory_Test()
        {
            TestStruct<ImageLoadConfigDirectory>(
                v => v.Size == 320,
                v => v.TimeDateStamp == 0,
                v => v.MajorVersion == 0,
                v => v.MinorVersion == 0,
                v => v.GlobalFlagsClear == 0,
                v => v.GlobalFlagsSet == 0,
                v => v.CriticalSectionDefaultTimeout == 0,
                v => v.DeCommitFreeBlockThreshold == 0,
                v => v.DeCommitTotalFreeThreshold == 0,
                v => v.LockPrefixTable == 0,
                v => v.MaximumAllocationSize == 0,
                v => v.VirtualMemoryThreshold == 0,
                v => v.ProcessAffinityMask == 0,
                v => v.ProcessHeapFlags == 0,
                v => v.CSDVersion == 0,
                v => v.DependentLoadFlags == 2048,
                v => v.EditList == 0,
                v => v.SecurityCookie.ListedAddress == 6444148016,
                v => v.SEHandlerTable.ListedAddress == (long) 0,
                v => v.SEHandlerCount == 0,
                v => v.GuardCFCheckFunctionPointer.ListedAddress == (long) 6444134880,
                v => v.GuardCFDispatchFunctionPointer.ListedAddress == (long) 6444150784,
                v => v.GuardCFFunctionTable.ListedAddress == (long) 6443712236,
                v => v.GuardCFFunctionCount == 2217,
                v => v.GuardFlags == (IMAGE_GUARD) 281113856, //The high bits contain size info that will be masked out, which mucks up our visualization of the value
                v => (object) v.CodeIntegrity == IgnoreValue,
                v => v.GuardAddressTakenIatEntryTable.ListedAddress == (long) 0,
                v => v.GuardAddressTakenIatEntryCount == 0,
                v => v.GuardLongJumpTargetTable.ListedAddress == (long) 0,
                v => v.GuardLongJumpTargetCount == 0,
                v => v.DynamicValueRelocTable == 0,
                v => v.CHPEMetadataPointer == 0,
                v => v.GuardRFFailureRoutine == 0,
                v => v.GuardRFFailureRoutineFunctionPointer.ListedAddress == (long) 0,
                v => v.DynamicValueRelocTableOffset.ListedOffset == 1372,
                v => v.DynamicValueRelocTableSection == 11,
                v => v.Reserved2 == 0,
                v => v.GuardRFVerifyStackPointerFunctionPointer.ListedAddress == (long) 0,
                v => v.HotPatchTableOffset == 0,
                v => v.Reserved3 == 0,
                v => v.EnclaveConfigurationPointer.ListedAddress == (long) 0,
                v => v.VolatileMetadataPointer == 0,
                v => v.GuardEHContinuationTable.ListedAddress == (long) 6443711376,
                v => v.GuardEHContinuationCount == 172,
                v => v.GuardXFGCheckFunctionPointer.ListedAddress == (long) 6444150792,
                v => v.GuardXFGDispatchFunctionPointer.ListedAddress == (long) 6444150800,
                v => v.GuardXFGTableDispatchFunctionPointer.ListedAddress == (long) 6444150808,
                v => v.CastGuardOsDeterminedFailureMode == 6444150816,
                v => v.GuardMemcpyFunctionPointer.ListedAddress == (long) 0,
                v => v.UnknownBytes == null
            );

            var actions = new List<Action<IView>>();

            //The first 1579 views are XFG

            for (var i = 0; i < 1579; i++)
                actions.Add(v => { });

            actions.Add(
                v => v.VerifyStruct(
                    name: "IMAGE_LOAD_CONFIG_DIRECTORY", offset: 1256944, size: 320,
                    c => c.VerifyField(name: "Size", value: 320),
                    c => c.VerifyField(name: "TimeDateStamp", value: (uint) 0),
                    c => c.VerifyField(name: "MajorVersion", value: (ushort) 0),
                    c => c.VerifyField(name: "MinorVersion", value: (ushort) 0),
                    c => c.VerifyField(name: "GlobalFlagsClear", value: 0),
                    c => c.VerifyField(name: "GlobalFlagsSet", value: 0),
                    c => c.VerifyField(name: "CriticalSectionDefaultTimeout", value: 0),
                    c => c.VerifyField(name: "DeCommitFreeBlockThreshold", value: (long) 0),
                    c => c.VerifyField(name: "DeCommitTotalFreeThreshold", value: (long) 0),
                    c => c.VerifyField(name: "LockPrefixTable", value: (long) 0),
                    c => c.VerifyField(name: "MaximumAllocationSize", value: (long) 0),
                    c => c.VerifyField(name: "VirtualMemoryThreshold", value: (long) 0),
                    c => c.VerifyField(name: "ProcessAffinityMask", value: (long) 0),
                    c => c.VerifyField(name: "ProcessHeapFlags", value: 0),
                    c => c.VerifyField(name: "CSDVersion", value: (ushort) 0),
                    c => c.VerifyField(name: "DependentLoadFlags", value: (ushort) 2048),
                    c => c.VerifyField(name: "EditList", value: (long) 0),
                    c => c.VerifyField(name: "SecurityCookie", value: 6444148016),
                    c => c.VerifyField(name: "SEHandlerTable", value: (long) 0),
                    c => c.VerifyField(name: "SEHandlerCount", value: (long) 0),
                    c => c.VerifyField(name: "GuardCFCheckFunctionPointer", value: 6444134880),
                    c => c.VerifyField(name: "GuardCFDispatchFunctionPointer", value: 6444150784),
                    c => c.VerifyField(name: "GuardCFFunctionTable", value: 6443712236),
                    c => c.VerifyField(name: "GuardCFFunctionCount", value: (long) 2217),
                    c => c.VerifyField(name: "GuardFlags", value: (IMAGE_GUARD) 281113856), //The high bits contain size info that will be masked out, which mucks up our visualization of the value
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_LOAD_CONFIG_CODE_INTEGRITY", offset: 1257092, size: 12),
                    c => c.VerifyField(name: "GuardAddressTakenIatEntryTable", value: (long) 0),
                    c => c.VerifyField(name: "GuardAddressTakenIatEntryCount", value: (long) 0),
                    c => c.VerifyField(name: "GuardLongJumpTargetTable", value: (long) 0),
                    c => c.VerifyField(name: "GuardLongJumpTargetCount", value: (long) 0),
                    c => c.VerifyField(name: "DynamicValueRelocTable", value: (long) 0),
                    c => c.VerifyField(name: "CHPEMetadataPointer", value: (long) 0),
                    c => c.VerifyField(name: "GuardRFFailureRoutine", value: (long) 0),
                    c => c.VerifyField(name: "GuardRFFailureRoutineFunctionPointer", value: (long) 0),
                    c => c.VerifyField(name: "DynamicValueRelocTableOffset", value: 1372),
                    c => c.VerifyField(name: "DynamicValueRelocTableSection", value: (ushort) 11),
                    c => c.VerifyField(name: "Reserved2", value: (ushort) 0),
                    c => c.VerifyField(name: "GuardRFVerifyStackPointerFunctionPointer", value: (long) 0),
                    c => c.VerifyField(name: "HotPatchTableOffset", value: 0),
                    c => c.VerifyField(name: "Reserved3", value: 0),
                    c => c.VerifyField(name: "EnclaveConfigurationPointer", value: (long) 0),
                    c => c.VerifyField(name: "VolatileMetadataPointer", value: (long) 0),
                    c => c.VerifyField(name: "GuardEHContinuationTable", value: 6443711376),
                    c => c.VerifyField(name: "GuardEHContinuationCount", value: (long) 172),
                    c => c.VerifyField(name: "GuardXFGCheckFunctionPointer", value: 6444150792),
                    c => c.VerifyField(name: "GuardXFGDispatchFunctionPointer", value: 6444150800),
                    c => c.VerifyField(name: "GuardXFGTableDispatchFunctionPointer", value: 6444150808),
                    c => c.VerifyField(name: "CastGuardOsDeterminedFailureMode", value: (long) 6444150816),
                    c => c.VerifyField(name: "GuardMemcpyFunctionPointer", value: (long) 0)
                )
            );

            TestView<ImageLoadConfigDirectory>(
                actions.ToArray(),
                false
            );
        }

        [TestMethod]
        public void ImageLoadConfigCodeIntegrity_Test()
        {
            TestStruct<ImageLoadConfigCodeIntegrity>(
                v => v.Flags == 0,
                v => v.Catalog == 0,
                v => v.CatalogOffset == 0,
                v => v.Reserved == 0
            );

            TestView<ImageLoadConfigCodeIntegrity>(
                v => v.VerifyStruct(
                    name: "IMAGE_LOAD_CONFIG_CODE_INTEGRITY", offset: 1257092, size: 12,
                    c => c.VerifyField(name: "Flags", value: (ushort) 0),
                    c => c.VerifyField(name: "Catalog", value: (ushort) 0),
                    c => c.VerifyField(name: "CatalogOffset", value: 0),
                    c => c.VerifyField(name: "Reserved", value: 0)
                )
            );
        }

        [TestMethod]
        public void ImageEnclaveConfig_Test()
        {
            TestStruct<ImageEnclaveConfig>(
                v => v.Size == 80,
                v => v.MinimumRequiredConfigSize == 0,
                v => v.PolicyFlags == 0,
                v => v.NumberOfImports == 4,
                v => v.ImportList.ListedOffset == 222852,
                v => v.ImportEntrySize == 80,
                v => v.FamilyID == new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                v => v.ImageID == new byte[] { 80, 65, 198, 45, 179, 131, 66, 49, 170, 226, 169, 74, 131, 219, 200, 22 },
                v => v.ImageVersion == 0,
                v => v.SecurityVersion == 0,
                v => v.EnclaveSize == 0,
                v => v.NumberOfThreads == 0,
                v => v.EnclaveFlags == 0
            );

            TestView<ImageEnclaveConfig>(
                v => v.VerifyStruct(
                    name: "IMAGE_ENCLAVE_CONFIG", offset: 206864, size: 0,
                    c => c.VerifyField(name: "Size", value: 80),
                    c => c.VerifyField(name: "MinimumRequiredConfigSize", value: 0),
                    c => c.VerifyField(name: "PolicyFlags", value: 0),
                    c => c.VerifyField(name: "NumberOfImports", value: 4),
                    c => c.VerifyField(name: "ImportList", value: 222852),
                    c => c.VerifyField(name: "ImportEntrySize", value: 80),
                    c => c.VerifyField(name: "FamilyID", value: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }),
                    c => c.VerifyField(name: "ImageID", value: new byte[] { 80, 65, 198, 45, 179, 131, 66, 49, 170, 226, 169, 74, 131, 219, 200, 22 }),
                    c => c.VerifyField(name: "ImageVersion", value: 0),
                    c => c.VerifyField(name: "SecurityVersion", value: 0),
                    c => c.VerifyField(name: "EnclaveSize", value: (long) 0),
                    c => c.VerifyField(name: "NumberOfThreads", value: 0),
                    c => c.VerifyField(name: "EnclaveFlags", value: 0)
                )
            );
        [TestMethod]
        [TestMethod]
        public void ImageDynamicRelocation_Test()
        {
            TestStruct<ImageDynamicRelocation>(
                v => v.Symbol == ImageDynamicRelocationKind.FUNCTION_OVERRIDE,
                v => v.BaseRelocSize == 180,
                v => v.Data == IgnoreValue
            );

            TestView<ImageDynamicRelocation>(
                v => v.VerifyStruct(
                    name: "IMAGE_DYNAMIC_RELOCATION", offset: 2155876, size: 196,
                    c => c.VerifyField(name: "Symbol", value: ImageDynamicRelocationKind.FUNCTION_OVERRIDE),
                    c => c.VerifyField(name: "BaseRelocSize", value: 180),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_FUNCTION_OVERRIDE_HEADER", offset: 2155888, size: 184)
                )
            );
        }
        public void ImageDynamicRelocationTable_Test()
        {
            TestStruct<ImageDynamicRelocationTable>(
                v => v.Version == 1,
                v => v.Size == 192,
                v => v.DynamicRelocations == IgnoreValue
            );

            TestView<ImageDynamicRelocationTable>(
                v => v.VerifyStruct(
                    name: "IMAGE_DYNAMIC_RELOCATION_TABLE", offset: 2155868, size: 204,
                    c => c.VerifyField(name: "Version", value: 1),
                    c => c.VerifyField(name: "Size", value: 192),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_DYNAMIC_RELOCATION", offset: 2155876, size: 196) //There's a massive hierarchy of children; test each of these in their own test
                )
            );
        #region Symbol 1

        [TestMethod]
        public void ImagePrologueDynamicRelocationHeader_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        #region Symbol 2

        [TestMethod]
        public void ImageEpilogueDynamicRelocationHeader_Test()
        {
            Assert.Inconclusive();
        }

        #endregion
        #region Symbol 3

        [TestMethod]
        public void ImageImportControlTransferDynamicRelocation_Test()
        {
            TestStruct<ImageImportControlTransferDynamicRelocation>(
                v => v.PageRelativeOffset == 297,
                v => v.IndirectCall == true,
                v => v.IATIndex == 100
            );

            TestView<ImageImportControlTransferDynamicRelocation>(
                v => v.VerifyStruct(
                    name: "IMAGE_IMPORT_CONTROL_TRANSFER_DYNAMIC_RELOCATION", offset: 274956, size: 4,
                    c => c.VerifyBitField(name: "PageRelativeOffset", value: 297, 12),
                    c => c.VerifyBitField(name: "IndirectCall", value: (byte) 1, 1),
                    c => c.VerifyBitField(name: "IATIndex", value: 100, 19)
                )
            );
        }

        #endregion
        #region Symbol 4

        [TestMethod]
        public void ImageIndirControlTransferDynamicRelocation_Test()
        {
            TestStruct<ImageIndirControlTransferDynamicRelocation>(
                v => v.PageRelativeOffset == 254,
                v => v.IndirectCall == true,
                v => v.RexWPrefix == false,
                v => v.CfgCheck == true,
                v => v.Reserved == false
            );

            TestView<ImageIndirControlTransferDynamicRelocation>(
                v => v.VerifyStruct(
                    name: "IMAGE_INDIR_CONTROL_TRANSFER_DYNAMIC_RELOCATION", offset: 73800, size: 2,
                    c => c.VerifyBitField(name: "PageRelativeOffset", value: (short) 254, 12),
                    c => c.VerifyBitField(name: "IndirectCall", value: (byte) 1, 1),
                    c => c.VerifyBitField(name: "RexWPrefix", value: (byte) 0, 1),
                    c => c.VerifyBitField(name: "CfgCheck", value: (byte) 1, 1),
                    c => c.VerifyBitField(name: "Reserved", value: (byte) 0, 1)
                )
            );

        }

        #endregion
        #region Symbol 5

        [TestMethod]
        public void ImageSwitchTableBranchDynamicRelocation_Test()
        {
            TestStruct<ImageSwitchTableBranchDynamicRelocation>(
                v => v.PageRelativeOffset == 3569,
                v => v.RegisterNumber == 1
            );

            TestView<ImageSwitchTableBranchDynamicRelocation>(
                v => v.VerifyStruct(
                    name: "IMAGE_SWITCHTABLE_BRANCH_DYNAMIC_RELOCATION", offset: 78120, size: 2,
                    c => c.VerifyBitField(name: "PageRelativeOffset", value: (short) 3569, 12),
                    c => c.VerifyBitField(name: "RegisterNumber", value: (short) 1, 4)
                )
            );
        }

        #endregion
        #region Symbol 7

        [TestMethod]
        public void ImageFunctionOverrideHeader_Test()
        {
            TestStruct<ImageFunctionOverrideHeader>(
                v => v.FuncOverrideSize == 48,
                v => v.FuncOverrides == IgnoreValue,
                v => (object) v.BDDInfo == IgnoreValue
            );

            TestView<ImageFunctionOverrideHeader>(
                v => v.VerifyStruct(
                    name: "IMAGE_FUNCTION_OVERRIDE_HEADER", offset: 2155888, size: 184,
                    c => c.VerifyField(name: "FuncOverrideSize", value: 48),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_FUNCTION_OVERRIDE_DYNAMIC_RELOCATION", offset: 2155892, size: 52),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_INFO", offset: 2155940, size: 128)
                )
            );
        }

        [TestMethod]
        public void ImageFunctionOverrideDynamicRelocation_Test()
        {
            TestStruct<ImageFunctionOverrideDynamicRelocation>(
                v => v.OriginalRva == 680448,
                v => v.BDDOffset == 0,
                v => v.RvaSize == 20,
                v => v.BaseRelocSize == 12,
                v => v.RVAs == new int[] { 681088, 681600, 681984, 682432, 682752 },
                v => v.BaseRelocs == IgnoreValue
            );

            TestView<ImageFunctionOverrideDynamicRelocation>(
                v => v.VerifyStruct(
                    name: "IMAGE_FUNCTION_OVERRIDE_DYNAMIC_RELOCATION", offset: 2155892, size: 48,
                    c => c.VerifyField(name: "OriginalRva", value: 680448),
                    c => c.VerifyField(name: "BDDOffset", value: 0),
                    c => c.VerifyField(name: "RvaSize", value: 20),
                    c => c.VerifyField(name: "BaseRelocSize", value: 12),
                    c => c.VerifyField(name: "RVAs", value: new int[] { 681088, 681600, 681984, 682432, 682752 }),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BASE_RELOCATION", offset: 2155928, size: 12)
                )
            );
        }

        [TestMethod]
        public void ImageBDDInfo_Test()
        {
            TestStruct<ImageBDDInfo>(
                v => v.Version == 1,
                v => v.BDDSize == 120,
                v => v.BDDNodes == IgnoreValue
            );

            TestView<ImageBDDInfo>(
                v => v.VerifyStruct(
                    name: "IMAGE_BDD_INFO", offset: 2155940, size: 128,
                    c => c.VerifyField(name: "Version", value: 1),
                    c => c.VerifyField(name: "BDDSize", value: 120),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2155948, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2155956, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2155964, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2155972, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2155980, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2155988, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2155996, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2156004, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2156012, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2156020, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2156028, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2156036, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2156044, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2156052, size: 8),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2156060, size: 8)
                )
            );
        }

        [TestMethod]
        public void ImageBDDDynamicRelocation_Test()
        {
            TestStruct<ImageBDDDynamicRelocation>(
                v => v.Left == 4,
                v => v.Right == 1,
                v => v.Value == 319
            );

            TestView<ImageBDDDynamicRelocation>(
                v => v.VerifyStruct(
                    name: "IMAGE_BDD_DYNAMIC_RELOCATION", offset: 2155948, size: 8,
                    c => c.VerifyField(name: "Left", value: (short) 4),
                    c => c.VerifyField(name: "Right", value: (short) 1),
                    c => c.VerifyField(name: "Value", value: 319)
                )
            );
        }

        #endregion
        #endregion
        #region Bound Import Table (11)
        }
        }

        #endregion
        }

        #endregion
        }
        }
        }
                )
            );
        }
        }
        }
        }
        }
