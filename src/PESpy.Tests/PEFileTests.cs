using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using ChaosLib;
using ClrDebug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.Ecma335;
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
                v => GetSpan<short>(v, "ReservedWords") == new short[] { 0, 0, 0, 0 },
                v => v.OEMIdentifier == 0,
                v => v.OEMInformation == 0,
                v => GetSpan<short>(v, "ReservedWords2") == new short[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
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
        }

        [TestMethod]
        public void DOSStub_Test()
        {
            TestBytes(
                "DosStub",
                new byte[]
                {
                    0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD, 0x21, 0xB8, 0x01, 0x4C, 0xCD, 0x21, 0x54, 0x68, 0x69, 0x73, 0x20, 0x70,
                    0x72, 0x6F, 0x67, 0x72, 0x61, 0x6D, 0x20, 0x63, 0x61, 0x6E, 0x6E, 0x6F, 0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6E,
                    0x20, 0x69, 0x6E, 0x20, 0x44, 0x4F, 0x53, 0x20, 0x6D, 0x6F, 0x64, 0x65, 0x2E, 0x0D, 0x0D, 0x0A, 0x24, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00
                }
            );
        }

        #endregion
        #region Rich Header

        [TestMethod]
        public void RichHeader_Test()
        {
            TestStruct<RichHeader>(
                v => v.DanS == 1399742788,
                v => v.Padding1 == 0,
                v => v.Padding2 == 0,
                v => v.Padding3 == 0,
                v => v.Items == new[]
                {
                    new ProdItem { BuildId = 0,     Count = 1,   ProdId = 0,    ProductId = "Unknown",          VisualStudioVersion = null },
                    new ProdItem { BuildId = 30795, Count = 1,   ProdId = 256,  ProductId = "Export1400",       VisualStudioVersion = "Visual Studio 2015 14.00" },
                    new ProdItem { BuildId = 30795, Count = 44,  ProdId = 259,  ProductId = "Masm1400",         VisualStudioVersion = "Visual Studio 2015 14.00" },
                    new ProdItem { BuildId = 30795, Count = 131, ProdId = 260,  ProductId = "Utc1900_C",        VisualStudioVersion = "Visual Studio 2015 14.00" },
                    new ProdItem { BuildId = 30795, Count = 304, ProdId = 269,  ProductId = "Utc1900_POGO_O_C", VisualStudioVersion = null },
                    new ProdItem { BuildId = 30795, Count = 27,  ProdId = 261,  ProductId = "Utc1900_CPP",      VisualStudioVersion = "Visual Studio 2015 14.00" },
                    new ProdItem { BuildId = 30795, Count = 1,   ProdId = 255,  ProductId = "Cvtres1400",       VisualStudioVersion = "Visual Studio 2015 14.00" },
                    new ProdItem { BuildId = 30795, Count = 1,   ProdId = 258,  ProductId = "Linker1400",       VisualStudioVersion = "Visual Studio 2015 14.00" },
                },
                v => v.Rich == 1751345490,
                v => v.XorKey == -1638947558
            );

            TestView<RichHeader>(
                v => v.VerifyStruct(
                    name: "Rich Header", offset: 128, size: 88,
                    c => c.VerifyField(name: "DanS", value: 1399742788),
                    c => c.VerifyField(name: "Padding1", value: 0),
                    c => c.VerifyField(name: "Padding2", value: 0),
                    c => c.VerifyField(name: "Padding3", value: 0),

                    c => c.VerifyStruct(name: "PRODITEM", offset: 144, size: 8,
                        v => v.VerifyField(name: "ProdId", value: (short) 0),
                        v => v.VerifyField(name: "BuildId", value: (short) 0),
                        v => v.VerifyField(name: "Count", value: 1)
                    ),
                                        c => c.VerifyStruct(name: "PRODITEM", offset: 152, size: 8,
                        v => v.VerifyField(name: "ProdId", value: (short) 256),
                        v => v.VerifyField(name: "BuildId", value: (short) 30795),
                        v => v.VerifyField(name: "Count", value: 1)
                    ),
                    c => c.VerifyStruct(name: "PRODITEM", offset: 160, size: 8,
                        v => v.VerifyField(name: "ProdId", value: (short) 259),
                        v => v.VerifyField(name: "BuildId", value: (short) 30795),
                        v => v.VerifyField(name: "Count", value: 44)
                    ),
                    c => c.VerifyStruct(name: "PRODITEM", offset: 168, size: 8,
                        v => v.VerifyField(name: "ProdId", value: (short) 260),
                        v => v.VerifyField(name: "BuildId", value: (short) 30795),
                        v => v.VerifyField(name: "Count", value: 131)
                    ),
                    c => c.VerifyStruct(name: "PRODITEM", offset: 176, size: 8,
                        v => v.VerifyField(name: "ProdId", value: (short) 269),
                        v => v.VerifyField(name: "BuildId", value: (short) 30795),
                        v => v.VerifyField(name: "Count", value: 304)
                    ),
                    c => c.VerifyStruct(name: "PRODITEM", offset: 184, size: 8,
                        v => v.VerifyField(name: "ProdId", value: (short) 261),
                        v => v.VerifyField(name: "BuildId", value: (short) 30795),
                        v => v.VerifyField(name: "Count", value: 27)
                    ),
                    c => c.VerifyStruct(name: "PRODITEM", offset: 192, size: 8,
                        v => v.VerifyField(name: "ProdId", value: (short) 255),
                        v => v.VerifyField(name: "BuildId", value: (short) 30795),
                        v => v.VerifyField(name: "Count", value: 1)
                    ),
                    c => c.VerifyStruct(name: "PRODITEM", offset: 200, size: 8,
                        v => v.VerifyField(name: "ProdId", value: (short) 258),
                        v => v.VerifyField(name: "BuildId", value: (short) 30795),
                        v => v.VerifyField(name: "Count", value: 1)
                    ),

                    c => c.VerifyField(name: "Rich", value: 1751345490),
                    c => c.VerifyField(name: "XorKey", value: -1638947558)
                )
            );
        }

        [TestMethod]
        public void RichHeader_ProdItem_Test()
        {
            TestStruct<ProdItem>(
                v => v.ProdId == 256,
                v => v.BuildId == 30795,
                v => v.Count == 1,
                v => v.ProductId == "Export1400",
                v => v.VisualStudioVersion == "Visual Studio 2015 14.00"
            );

            TestView<ProdItem>(
                v => v.VerifyStruct(
                    name: "PRODITEM", offset: 152, size: 8,
                    c => c.VerifyField(name: "ProdId", value: (short) 256),
                    c => c.VerifyField(name: "BuildId", value: (short) 30795),
                    c => c.VerifyField(name: "Count", value: 1)
                )
            );
        }

        #endregion
        #region NT Headers

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
                v => v.PointerToSymbolTable.ListedAddress == 0,
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
        public void ImageSectionHeader_Test()
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
        }

        [TestMethod]
        public void ImageExportDirectory_NormalAndForwardedExports()
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
                WithIgnores(
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
                    ),
                    after: 2489
                )
            );
        }

        #endregion
        #region Import Table (1)

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
        public void ImageImportDescriptor_ImageThunkData_Test()
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
                WithIgnores(
                    before: 1,
                    v => v.VerifyStruct(
                        name: "IMAGE_IMPORT_DESCRIPTOR ucrtbase_enclave.dll", offset: 236888, size: 20,
                        c => c.VerifyField(name: "OriginalFirstThunk", value: 237080),
                        c => c.VerifyField(name: "TimeDateStamp", value: (uint) 0),
                        c => c.VerifyField(name: "ForwarderChain", value: 0),
                        c => c.VerifyField(name: "Name", value: 237918),
                        c => c.VerifyField(name: "FirstThunk", value: 196224)
                    ),
                    after: 39
                )
            );
        }

        [TestMethod]
        public void ImageImportByName_Test()
        {
            TestStruct<ImageImportByName>(
                v => v.Hint == 18,
                v => v.Name == "BCryptEncrypt"
            );

            TestView<ImageImportByName>(
                v => v.VerifyStruct(
                    name: "IMAGE_IMPORT_BY_NAME", offset: 238218, size: 16,
                    c => c.VerifyField(name: "Hint", value: (short) 18),
                    c => c.VerifyField(name: "Name", value: "BCryptEncrypt")
                )
            );
        }

        #endregion
        #region Resource Directory (2)

        [TestMethod]
        public void ImageResourceDirectory_Test()
        {
            TestStruct<ImageResourceDirectory>(
                v => v.Characteristics == 0,
                v => v.TimeDateStamp == 0,
                v => v.MajorVersion == 0,
                v => v.MinorVersion == 0,
                v => v.NumberOfNamedEntries == 0,
                v => v.NumberOfIdEntries == 1,
                v => v.Entries == IgnoreValue
            );

            TestView<ImageResourceDirectory>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "IMAGE_RESOURCE_DIRECTORY", offset: 1671208, size: 16,
                        c => c.VerifyField(name: "Characteristics", value: (uint) 0),
                        c => c.VerifyField(name: "TimeDateStamp", value: (uint) 0),
                        c => c.VerifyField(name: "MajorVersion", value: (ushort) 0),
                        c => c.VerifyField(name: "MinorVersion", value: (ushort) 0),
                        c => c.VerifyField(name: "NumberOfNamedEntries", value: (ushort) 0),
                        c => c.VerifyField(name: "NumberOfIdEntries", value: (ushort) 1)
                    ),
                    after: 5
                )
            );
        }

        [TestMethod]
        public void ImageResourceDirectoryEntry_Test()
        {
            TestStruct<ImageResourceDirectoryEntry>(
                v => v.Parent == null,
                v => v.NameOrId.NameOffset.Value.NameString == "MUI",
                v => v.OffsetToData.ListedOffset == -2147483608,
                v => v.OffsetToDirectory.ListedOffset == 40,
                v => v.DataIsDirectory == true,
                v => v.Type == null
            );

            TestView<ImageResourceDirectoryEntry>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "IMAGE_RESOURCE_DIRECTORY_ENTRY", offset: 1671184, size: 8,
                        c => c.VerifyField(name: "Name", value: 0xE8),
                        c => c.VerifyField(name: "OffsetToData", value: unchecked((int) 0x80000028))
                    ),
                    after: 7
                )
            );
        }

        [TestMethod]
        public void ImageResourceDataEntry_Test()
        {
            TestStruct<ImageResourceDataEntry>(
                v => v.OffsetToData.ListedOffset == 1704176,
                v => v.Size == 896,
                v => v.CodePage == 0,
                v => v.Reserved == 0,
                v => (ResourceType) v.Type == ResourceType.Version //Note: this comes from the parent
            );

            TestView<ImageResourceDataEntry>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "IMAGE_RESOURCE_DATA_ENTRY", offset: 1671384, size: 16,
                        c => c.VerifyField(name: "OffsetToData", value: 1704176),
                        c => c.VerifyField(name: "Size", value: 896),
                        c => c.VerifyField(name: "CodePage", value: 0),
                        c => c.VerifyField(name: "Reserved", value: 0)
                    ),
                    after: 1 //Ignore the VS_VERSIONINFO thats inside the record
                )
            );
        }

        [TestMethod]
        public void ImageResourceDirStringU_Test()
        {
            TestStruct<ImageResourceDirStringU>(
                v => v.Length == 3,
                v => v.NameString == "MUI"
            );

            TestView<ImageResourceDirStringU>(
                v => v.VerifyStruct(
                    name: "IMAGE_RESOURCE_DIR_STRING_U", offset: 1671400, size: 8,
                    c => c.VerifyField(name: "Length", value: (short) 3),
                    c => c.VerifyField(name: "NameString", value: "MUI")
                )
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

        [TestMethod]
        public unsafe void ClrDebugResource_Test()
        {
            TestStruct<ClrDebugResource>(
                v => v.Version == 0,
                v => v.Signature == new Guid("b1ee760d-6c4a-4533-ba41-6f4f661fabaf"),
                v => v.DacTimeStamp == 1733245982,
                v => v.DacSizeOfImage == 1347584,
                v => v.DbiTimeStamp == 1733245977,
                v => v.DbiSizeOfImage == 1249280
            );

            TestView<ClrDebugResource>(
                v => v.VerifyStruct(
                    name: "CLR_DEBUG_RESOURCE", offset: 5004280, size: 36,
                    c => c.VerifyField(name: "dwVersion", value: 0),
                    c => c.VerifyField(name: "signature", value: new Guid("b1ee760d-6c4a-4533-ba41-6f4f661fabaf")),
                    c => c.VerifyField(name: "dwDacTimeStamp", value: 1733245982),
                    c => c.VerifyField(name: "dwDacSizeOfImage", value: 1347584),
                    c => c.VerifyField(name: "dwDbiTimeStamp", value: 1733245977),
                    c => c.VerifyField(name: "dwDbiSizeOfImage", value: 1249280)
                )
            );
        }

        #endregion
        #region Exception Table (3)

        [TestMethod]
        public void RuntimeFunction_Test()
        {
            TestStruct<RuntimeFunction>(
                v => v.BeginAddress == 4104,
                v => v.EndAddress == 4354,
                v => v.UnwindData.ListedOffset == 1424960
            );

            TestView<RuntimeFunction>(
                WithIgnores(
                    before: 1,
                    v => v.VerifyStruct(
                        name: "RUNTIME_FUNCTION", offset: 1589248, size: 12,
                        c => c.VerifyField(name: "BeginAddress", value: 4104),
                        c => c.VerifyField(name: "EndAddress", value: 4354),
                        c => c.VerifyField(name: "UnwindData", value: 1424960)
                    )
                )
            );
        }

        [TestMethod]
        public void UnwindInfo_Test()
        {
            TestStruct<UnwindInfo>(
                v => v.Version == 1,
                v => v.Flags == UNW_FLAG.FHANDLER,
                v => v.SizeOfProlog == 44,
                v => v.CountOfCodes == 11,
                v => v.FrameRegister == 0,
                v => v.FrameOffset == 0,
                v => v.UnwindCode == IgnoreValue,
                v => v.ExceptionHandler == 650796
            );

            TestView<UnwindInfo>(
                v => v.VerifyStruct(
                    name: "UNWIND_INFO", offset: 1425908, size: 68,
                    WithIgnores(
                        new Action<IView>[]
                        {
                            c => c.VerifyBitField(name: "Version", value: (byte) 1, bits: 3),
                            c => c.VerifyBitField(name: "Flags", value: UNW_FLAG.FHANDLER, bits: 5),
                            c => c.VerifyField(name: "SizeOfProlog", value: (byte) 44),
                            c => c.VerifyField(name: "CountOfCodes", value: (byte) 11),
                            c => c.VerifyBitField(name: "FrameRegister", value: (byte) 0, 4),
                            c => c.VerifyBitField(name: "FrameOffset", value: (byte) 0, 4),
                        },
                        after: 11
                    )
                )
            );
        }
        [TestMethod]
        public void ScopeRecord_Test()
        {
            TestStruct<ScopeRecord>(
                v => v.BeginAddress == 12717,
                v => v.EndAddress == 12744,
                v => v.HandlerAddress == 1,
                v => v.JumpTarget == 12744
            );

            TestView<ScopeRecord>(
                v => v.VerifyStruct(
                    name: "ScopeRecord", offset: 1425944, size: 16,
                    c => c.VerifyField(name: "BeginAddress", value: 12717),
                    c => c.VerifyField(name: "EndAddress", value: 12744),
                    c => c.VerifyField(name: "HandlerAddress", value: 1),
                    c => c.VerifyField(name: "JumpTarget", value: 12744)
                )
            );
        }
        }
        }
        }
        }
        }
        #region Security Table (4)

        [TestMethod]
        public void WinCertificate_Test()
        {
            TestStruct<WinCertificate>(
                v => v.Length == 28680,
                v => v.Revision == WinCertRevision.WIN_CERT_REVISION_2_0,
                v => v.CertificateType == WinCertType.SignedData,
                v => v.Certificate.ToString() == @"[Subject]
  CN=Microsoft Windows, O=Microsoft Corporation, L=Redmond, S=Washington, C=US

[Issuer]
  CN=Microsoft Windows Production PCA 2011, O=Microsoft Corporation, L=Redmond, S=Washington, C=US

[Serial Number]
  330000045C3D5672666CB7541700000000045C

[Not Before]
  15/09/2023 4:20:38 AM

[Not After]
  5/09/2024 4:20:38 AM

[Thumbprint]
  58DA14F4C5941747B995956FDC89B4E3AAE47B8F
");

            TestView<WinCertificate>(
                v => v.VerifyStruct(
                    name: "WIN_CERTIFICATE", offset: 2158592, size: 28680,
                    c => c.VerifyField(name: "dwLength", value: 28680),
                    c => c.VerifyField(name: "wRevision", value: WinCertRevision.WIN_CERT_REVISION_2_0),
                    c => c.VerifyField(name: "wCertificateType", value: WinCertType.SignedData),
                    c => c.VerifyStruct(
                        name: "SignedData", offset: 2158600, size: 28672,
                        v => v.VerifyFieldIgnoreValue(
                            name: "Bytes"
                        )
                    )
                )
            );
        }

        [TestMethod]
        public void SignedData_Test()
        {
            TestStruct<SignedData>(
                v => v.Bytes == IgnoreValue,
                v => v.Certificate.ToString() == @"[Subject]
  CN=Microsoft Windows, O=Microsoft Corporation, L=Redmond, S=Washington, C=US

[Issuer]
  CN=Microsoft Windows Production PCA 2011, O=Microsoft Corporation, L=Redmond, S=Washington, C=US

[Serial Number]
  330000045C3D5672666CB7541700000000045C

[Not Before]
  15/09/2023 4:20:38 AM

[Not After]
  5/09/2024 4:20:38 AM

[Thumbprint]
  58DA14F4C5941747B995956FDC89B4E3AAE47B8F
");

            TestView<SignedData>(
                v => v.VerifyStruct(
                    name: "SignedData", offset: 2158600, size: 28672,
                    c => c.VerifyFieldIgnoreValue(name: "Bytes")
                )
            );
        }

        #endregion
        #region Base Relocation Table (5)

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

            TestView<ImageDebugDirectory>(
                v => v.VerifyStruct(
                    name: "IMAGE_DEBUG_DIRECTORY", offset: 1296312, size: 28,
                    c => c.VerifyField(name: "Characteristics", value: 0),
                    c => c.VerifyField(name: "TimeDateStamp", value: (uint) 3169667970),
                    c => c.VerifyField(name: "MajorVersion", value: (ushort) 0),
                    c => c.VerifyField(name: "MinorVersion", value: (ushort) 0),
                    c => c.VerifyField(name: "Type", value: ImageDebugType.CodeView),
                    c => c.VerifyField(name: "SizeOfData", value: 34),
                    c => c.VerifyField(name: "AddressOfRawData", value: 1423344),
                    c => c.VerifyField(name: "PointerToRawData", value: 1423344)
                ),
                v => v.VerifyStructIgnoreChildren("RSDSI", offset: 1423344, size: 34)
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
                v => v.Reserved == new byte[] { 0, 0, 0 },
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
        #region Reproducible (16)

        [TestMethod]
        public void ImageDebugDirectory_Reproducible_Test()
        {
            TestStruct<Reproducible>(
                v => v.Size == 32,
                v => v.Hash == new byte[] { 194, 130, 162, 88, 238, 74, 3, 126, 168, 207, 140, 176, 167, 130, 206, 12, 133, 209, 145, 14, 91, 214, 192, 89, 70, 53, 121, 182, 130, 75, 237, 188 }
            );

            TestView<Reproducible>(
                v => v.VerifyStruct(
                    name: "Reproducible", offset: 1424920, size: 36,
                    c => c.VerifyField(name: "Size", value: 32),
                    c => c.VerifyField(name: "Hash", value: new byte[] { 194, 130, 162, 88, 238, 74, 3, 126, 168, 207, 140, 176, 167, 130, 206, 12, 133, 209, 145, 14, 91, 214, 192, 89, 70, 53, 121, 182, 130, 75, 237, 188 })
                )
            );
        }
            Assert.Inconclusive();
        }
        #region ExDllCharacteristics (20)

        [TestMethod]
        public void ImageDebugDirectory_ExDllCharacteristics_Test()
        {
            TestStruct<ImageDllCharacteristicsEx, ImageDebugDirectory>(
                v => ((RawValue<ImageDllCharacteristicsEx>) v.Data).Value == ImageDllCharacteristicsEx.CET_COMPAT
            );

            TestView<ImageDllCharacteristicsEx, ImageDebugDirectory>(
                v => v.VerifyStruct(
                    name: "IMAGE_DEBUG_DIRECTORY", offset: 1296396, size: 28,
                    c => c.VerifyField(name: "Characteristics", value: 0),
                    c => c.VerifyField(name: "TimeDateStamp", value: (uint) 3169667970),
                    c => c.VerifyField(name: "MajorVersion", value: (ushort) 0),
                    c => c.VerifyField(name: "MinorVersion", value: (ushort) 0),
                    c => c.VerifyField(name: "Type", value: ImageDebugType.ExDllCharacteristics),
                    c => c.VerifyField(name: "SizeOfData", value: 4),
                    c => c.VerifyField(name: "AddressOfRawData", value: 1424956),
                    c => c.VerifyField(name: "PointerToRawData", value: 1424956)
                ),
                v => v.VerifyValue(1424956, ImageDllCharacteristicsEx.CET_COMPAT)
            );
        }

        #endregion
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
                v => v.LockPrefixTable.ListedAddress == 0,
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

            TestView<ImageLoadConfigDirectory>(
                WithIgnores(
                    before: 1579, //The first 1579 views are XFG
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
                ),
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
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "IMAGE_ENCLAVE_CONFIG", offset: 206864, size: 80,
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
                    ),
                    after: 7
                )
            );
        }

        [TestMethod]
        public void ImageEnclaveImport_Test()
        {
            TestStruct<ImageEnclaveImport>(
                v => v.MatchType == IMAGE_ENCLAVE_IMPORT_MATCH.NONE,
                v => v.MinimumSecurityVersion == 0,
                v => v.UniqueOrAuthorID == new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                v => v.FamilyID == new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                v => v.ImageID == new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                v => v.ImportName.ListedOffset == 65535,
                v => v.Reserved == 0
            );

            TestView<ImageEnclaveImport>(
                v => v.VerifyStruct(
                    name: "IMAGE_ENCLAVE_IMPORT", offset: 222852, size: 80,
                    c => c.VerifyField(name: "MatchType", value: IMAGE_ENCLAVE_IMPORT_MATCH.NONE),
                    c => c.VerifyField(name: "MinimumSecurityVersion", value: 0),
                    c => c.VerifyField(name: "UniqueOrAuthorID", value: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }),
                    c => c.VerifyField(name: "FamilyID", value: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }),
                    c => c.VerifyField(name: "ImageID", value: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }),
                    c => c.VerifyField(name: "ImportName", value: 65535),
                    c => c.VerifyField(name: "Reserved", value: 0)
                )
            );
        [TestMethod]
        public void GuardCFFunctionTable_Test()
        {
            TestStruct<GuardCFFunctionTable>(
                v => v.Entries == IgnoreValue
            );

            TestView<GuardCFFunctionTable>(
                WithIgnores(
                    before: 1579, //The first 1579 views are XFG
                    v => v.VerifyStructIgnoreChildren(
                        name: "GuardCFFunctionTable", offset: 1261292, size: 11085
                    )
                )
            );
        }

        [TestMethod]
        public void GuardCFFunctionTable_Entry_Test()
        {
            var str = GenerateTest<GuardCFFunctionTable.Entry>();

            TestStruct<GuardCFFunctionTable.Entry>(
                v => v.Function == 4368,
                v => v.Flags == IMAGE_GUARD_FLAG.FID_XFG,
                v => v.XFG.Value.ListedOffset == 4360
            );

            TestView<GuardCFFunctionTable.Entry>(
                v => v.VerifyValue(0x1108, (ulong) 0xbeac2e06165fc871), //XFG
                v => v.VerifyStruct(
                    name: "GFIDS Entry", offset: 1261292, size: 5,
                    c => c.VerifyField(name: "Function", value: 4368),
                    c => c.VerifyField(name: "Flags", value: IMAGE_GUARD_FLAG.FID_XFG)
                )
            );
        }

        [TestMethod]
        public void GuardEHContinuationTable_Test()
        {
            TestStruct<GuardEHContinuationTable>(
                v => v.Entries == IgnoreValue
            );

            TestView<GuardEHContinuationTable>(
                v => v.VerifyStructIgnoreChildren(
                    name: "GuardEHContinuationTable", offset: 1260432, size: 860
                )
            );
        }

        [TestMethod]
        public void GuardEHContinuationTable_Entry_Test()
        {
            TestStruct<GuardEHContinuationTable.Entry>(
                v => v.Function == 4831,
                v => v.Flags == 0
            );

            TestView<GuardEHContinuationTable.Entry>(
                v => v.VerifyStruct(
                    name: "EHCONT Entry", offset: 1260432, size: 5,
                    c => c.VerifyField(name: "Function", value: 4831),
                    c => c.VerifyField(name: "Flags", value: (IMAGE_GUARD_FLAG) 0)
                )
            );
        }
        public void ImageDynamicRelocation_Test()
        {
            TestStruct<ImageDynamicRelocation>(
                v => v.Symbol == ImageDynamicRelocationKind.FUNCTION_OVERRIDE,
                v => v.BaseRelocSize == 180,
                v => v.Data == IgnoreValue
            );

            TestView<ImageDynamicRelocation>(
                v => v.VerifyStruct(
                    name: "IMAGE_DYNAMIC_RELOCATION", offset: 2155876, size: 192,
                    c => c.VerifyField(name: "Symbol", value: ImageDynamicRelocationKind.FUNCTION_OVERRIDE),
                    c => c.VerifyField(name: "BaseRelocSize", value: 180),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_FUNCTION_OVERRIDE_HEADER", offset: 2155888, size: 180)
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
                    name: "IMAGE_DYNAMIC_RELOCATION_TABLE", offset: 2155868, size: 200,
                    c => c.VerifyField(name: "Version", value: 1),
                    c => c.VerifyField(name: "Size", value: 192),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_DYNAMIC_RELOCATION", offset: 2155876, size: 192) //There's a massive hierarchy of children; test each of these in their own test
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
                    name: "IMAGE_FUNCTION_OVERRIDE_HEADER", offset: 2155888, size: 180,
                    c => c.VerifyField(name: "FuncOverrideSize", value: 48),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_FUNCTION_OVERRIDE_DYNAMIC_RELOCATION", offset: 2155892, size: 48),
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

        [TestMethod]
        public void ImageBoundImportDescriptor_Test()
        {
            TestStruct<ImageBoundImportDescriptor>(
                v => v.TimeDateStamp == (uint) 835408115,
                v => v.OffsetModuleName == (ushort) 56,
                v => v.NumberOfModuleForwarderRefs == (ushort) 1,
                v => v.Name.Value == "MSVCRT40.dll"
            );

            TestView<ImageBoundImportDescriptor>(
                v => v.VerifyStruct(
                    name: "IMAGE_BOUND_IMPORT_DESCRIPTOR", offset: 616, size: 16,
                    c => c.VerifyField("TimeDateStamp", (uint) 835408115),
                    c => c.VerifyField("OffsetModuleName", (ushort) 56),
                    c => c.VerifyField("NumberOfModuleForwarderRefs", (ushort) 1),
                    s => s.VerifyStruct(
                        name: "IMAGE_BOUND_FORWARDER_REF", offset: 624, size: 8,
                        c => c.VerifyField(name: "TimeDateStamp", value: (uint) 998112782),
                        c => c.VerifyField(name: "OffsetModuleName", value: (ushort) 69),
                        c => c.VerifyField(name: "Reserved", value: (ushort) 0)
                    )
                ),
                v => v.VerifyValue(offset: 672, value: "MSVCRT40.dll"),
                v => v.VerifyValue(offset: 685, value: "msvcrt.DLL")
            );
        }
        }

        #endregion
        }

        #endregion
        #region Cor Header (14)

        [TestMethod]
        public void ImageCor20Header_Test()
        {
            TestStruct<ImageCor20Header>(
                v => v.ByteCount == 72,
                v => v.MajorRuntimeVersion == 2,
                v => v.MinorRuntimeVersion == 5,
                v => v.Metadata == IgnoreValue,
                v => v.Flags == (COMIMAGE_FLAGS.ILONLY | COMIMAGE_FLAGS._32BITREQUIRED | COMIMAGE_FLAGS.STRONGNAMESIGNED),
                v => v.EntryPointTokenOrRVA == 0,
                v => (object) v.Resources               == IgnoreValue,
                v => (object) v.StrongNameSignature     == IgnoreValue,
                v => (object) v.CodeManagerTable        == IgnoreValue,
                v => (object) v.VTableFixups            == IgnoreValue,
                v => (object) v.ExportAddressTableJumps == IgnoreValue,
                v => (object) v.ManagedNativeHeader     == IgnoreValue
            );

            TestView<ImageCor20Header>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "IMAGE_COR20_HEADER", offset: 520, size: 72,
                        c => c.VerifyField(name: "cb", value: 72),
                        c => c.VerifyField(name: "MajorRuntimeVersion", value: (ushort) 2),
                        c => c.VerifyField(name: "MinorRuntimeVersion", value: (ushort) 5),
                        c => c.VerifyFieldIgnoreValue(name: "MetaData"),
                        c => c.VerifyField(name: "Flags", value: (COMIMAGE_FLAGS.ILONLY | COMIMAGE_FLAGS._32BITREQUIRED | COMIMAGE_FLAGS.STRONGNAMESIGNED)),
                        c => c.VerifyField(name: "EntryPointTokenOrRVA", value: 0),
                        c => c.VerifyFieldIgnoreValue(name: "Resources"),
                        c => c.VerifyFieldIgnoreValue(name: "StrongNameSignature"),
                        c => c.VerifyFieldIgnoreValue(name: "CodeManagerTable"),
                        c => c.VerifyFieldIgnoreValue(name: "VTableFixups"),
                        c => c.VerifyFieldIgnoreValue(name: "ExportAddressTableJumps"),
                        c => c.VerifyFieldIgnoreValue(name: "ManagedNativeHeader")
                    ),
                    after: 8
                )
            );
        }

        [TestMethod]
        public void StorageSignature_Test()
        {
            TestStruct<StorageSignature>(
                v => v.Signature == 1112167234,
                v => v.MajorVersion == 1,
                v => v.MinorVersion == 1,
                v => v.ExtraData == 0,
                v => v.VersionStringLength == 12,
                v => v.Version == "v4.0.30319"
            );

            TestView<StorageSignature>(
                v => v.VerifyStruct(
                    name: "STORAGESIGNATURE", offset: 1604584, size: 28,
                    c => c.VerifyField(name: "iSignature", value: (uint) 1112167234),
                    c => c.VerifyField(name: "iMajorVer", value: (short) 1),
                    c => c.VerifyField(name: "iMinorVer", value: (short) 1),
                    c => c.VerifyField(name: "iExtraData", value: 0),
                    c => c.VerifyField(name: "iVersionString", value: 12),
                    c => c.VerifyField(name: "pVersion", value: "v4.0.30319")
                )
            );
        }

        [TestMethod]
        public void StorageHeader_Test()
        {
            TestStruct<StorageHeader>(
                v => v.Flags == STGHDR.STGHDR_NORMAL,
                v => v.Padding == 0,
                v => v.Streams == 5,
                v => v.StreamHeaders == IgnoreValue
            );

            TestView<StorageHeader>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "STORAGEHEADER", offset: 1604612, size: 80,
                        WithIgnores(
                            new Action<IView>[]
                            {
                                c => c.VerifyField(name: "fFlags", value: STGHDR.STGHDR_NORMAL),
                                c => c.VerifyField(name: "pad", value: (byte) 0),
                                c => c.VerifyField(name: "iStreams", value: (short) 5)
                            },
                            after: 5
                        )
                    ),
                    after: 6
                )
            );
        }

        [TestMethod]
        public void StorageStream_Test()
        {
            TestStruct<StorageStream>(
                v => v.iOffset == 108,
                v => v.Size == 1569152,
                v => v.Name == "#~"
            );

            TestView<StorageStream>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "STORAGESTREAM", offset: 1604616, size: 12,
                        c => c.VerifyField(name: "iOffset", value: 108),
                        c => c.VerifyField(name: "iSize", value: 1569152),
                        c => c.VerifyField(name: "rcName", value: "#~"),
                        c => c.VerifyByteBlob(1604627, new byte[] { 0 })
                    ),
                    after: 2
                )
            );
        }
        }
        }

        [TestMethod]
        public void ImageCorILMethod_Test()
        {
            TestStruct<ImageCorILMethod>(
                v => v.Flags == (CorILMethodFlags.TinyFormat1 | CorILMethodFlags.MoreSects | CorILMethodFlags.InitLocals),
                v => v.CodeSize == 7,
                v => v.Size == 1,
                v => v.MaxStack == 8,
                v => v.ILBytes == new byte[] { 2, 123, 63, 0, 0, 10, 42 },
                v => v.LocalVarSigTok == 0x0
            );

            TestView<ImageCorILMethod>(
                v => v.VerifyStruct(
                    name: "IMAGE_COR_ILMETHOD", offset: 592, size: 0,
                    c => c.VerifyField(name: "Flags", value: (CorILMethodFlags.TinyFormat1 | CorILMethodFlags.MoreSects | CorILMethodFlags.InitLocals)),
                    c => c.VerifyField(name: "CodeSize", value: 7),
                    c => c.VerifyField(name: "Size", value: (byte) 1),
                    c => c.VerifyField(name: "MaxStack", value: (short) 8),
                    c => c.VerifyField(name: "ILBytes", value: new byte[] { 2, 123, 63, 0, 0, 10, 42 }),
                    c => c.VerifyField(name: "LocalVarSigTok", value: 0x0)
                )
            );
        }
        private void TestStruct<TSelector, TVerifier>(params Expression<Func<TVerifier, bool>>[] asserts)
        {
            FileStream fs = null;
            object rawValue;

            rawValue = GetStruct<TSelector, TVerifier>(out fs);

            using var fs1 = fs;

            var results = new List<string>();

            var propertiesAndFieldsTouched = new List<MemberInfo>();

            bool IsEqual(object first, object second)
            {
                if (IgnoreValue.Equals(first))
                    return true;

                if (first == null)
                {
                    if (second == null)
                        return true;

                    return false;
                }
                else
                {
                    //First is not null

                    if (second == null)
                        return false;
                }

                if (first.Equals(second) || second.Equals(first)) //PCSTR -> string
                    return true;

                var firstType = first.GetType();
                var secondType = second.GetType();

                if (firstType.IsArray && secondType.IsArray)
                {
                    var firstArr = (Array) first;
                    var secondArr = (Array) second;

                    if (firstArr.Length != secondArr.Length)
                        return false;

                    for (var i = 0; i < firstArr.Length; i++)
                    {
                        var v1 = firstArr.GetValue(i);
                        var v2 = secondArr.GetValue(i);

                        if (v1 is ProdItem p1)
                        {
                            var p2 = (ProdItem) v2;

                            if (!(p1.BuildId == p2.BuildId && p1.Count == p2.Count && p1.ProdId == p2.ProdId && p1.ProductId == p2.ProductId && p1.VisualStudioVersion == p2.VisualStudioVersion))
                                return false;

                            continue;
                        }

                        if (!v1.Equals(v2))
                            return false;
                    }

                    return true;
                }

                return false;
            }

            foreach (var assert in asserts)
            {
                var local = rawValue;
                var body = (BinaryExpression) assert.Body;

                MemberInfo memberInfo;
                Type memberType;
                object actual;

                if (body.Left is MethodCallExpression c)
                {
                    if (c.Method.Name == "GetSpan")
                    {
                        var propertyName = ((ConstantExpression) c.Arguments[1]).Value.ToString();
                        memberInfo = rawValue.GetType().GetProperty(propertyName);
                        memberType = ((PropertyInfo) memberInfo).PropertyType;

                        var lambda = Expression.Lambda(c, assert.Parameters).Compile();

                        actual = lambda.DynamicInvoke(rawValue);
                    }
                    else if (c.Method.Name == "ToString")
                    {
                        memberInfo = GetPropertyInfo(c.Object, ref local);
                        memberType = ((PropertyInfo) memberInfo).PropertyType;

                        var lambda = Expression.Lambda(c, assert.Parameters).Compile();

                        actual = lambda.DynamicInvoke(rawValue);
                    }
                    else
                else
                {
                    memberInfo = GetPropertyInfo(body.Left, ref local);

                    if (memberInfo is PropertyInfo p)
                    {
                        actual = p.GetValue(local);
                        memberType = p.PropertyType;
                    }
                    else
                    {
                        var f = (FieldInfo) memberInfo;

                        actual = f.GetValue(local);
                        memberType = f.FieldType;
                    }
                }

                //If a field is of type "object" but can store a value of type "enum", an expression that compares against an enum will be Convert Int32 -> Convert Enum -> object property == Int32.
                //We double unwrap the converts to get to the underlying property, but we need to convert the right hand side from Int32 to enum if the inner-most convert was an enum type

                if (memberType == typeof(object))
                {
                    if (body.Left is UnaryExpression { NodeType: ExpressionType.Convert } c1 && c1.Operand is UnaryExpression { NodeType: ExpressionType.Convert } c2 && c2.Type.IsEnum)
                        memberType = c2.Type;
                    else if (body.Left is UnaryExpression { NodeType: ExpressionType.Convert } cc1 && cc1.Operand is MemberExpression mm1 && mm1.Type.IsEnum)
                        memberType = mm1.Type;
                }

                var underlying = Nullable.GetUnderlyingType(memberType);

                if (underlying != null)
        private void TestView<T>(params Action<IView>[] verify) => TestView<T>(verify, true);

        private void TestView<TSelector, TVerifier>(params Action<IView>[] verify) =>
            TestView<TSelector, TVerifier>(verify, true);

        private void TestView<T>(Action<IView>[] verify, bool assertChildCount) =>
            TestView<T, T>(verify, assertChildCount);

        private void TestView<TSelector, TVerifier>(Action<IView>[] verify, bool assertChildCount)
        {
            if (verify == null)
                throw new ArgumentNullException(nameof(verify));

            FileStream fs = null;

            try
            {
                var rawValue = GetStruct<TSelector, TVerifier>(out fs);

                fs.Seek(0, SeekOrigin.Begin);
                var peFile = PEFile.FromStream(fs, false);
                var reader = new StreamFileReader(fs, false);

                var viewWriter = new PEViewWriter(peFile, reader, ViewMode.Default);
                ((IViewable) rawValue).WriteView(viewWriter);
                var current = viewWriter.Current.OrderBy(v => v.Offset).ToArray();

                if (assertChildCount)
                    Assert.AreEqual(verify.Length, current.Length, "Number of views was different from expected");
                else
                    Assert.IsFalse(verify.Length > current.Length, $"Must have at most {current.Length} verifiers");

                var visitor = new ViewAlignmentVerifier();

                for (var i = 0; i < verify.Length; i++)
                {
                    verify[i](current[i]);

                    //Are all of the children properly aligned?
                    current[i].Accept(visitor);
                }
            }
            finally
            {
                fs?.Dispose();
            }
        }

        private string GenerateTest<T>() where T : IValue
        {
            FileStream fs = null;

            try
            {
                var rawValue = GetStruct<T, T>(out fs);

                Debug.Assert(rawValue != null);

                var builder = new StringBuilder();
                builder.Append("TestStruct<").Append(typeof(T).Name).AppendLine(">(");

                var properties = typeof(T).GetProperties().Where(p => p.Name != "Offset").ToArray();

                string GetValue(PropertyInfo propertyInfo, bool cast)
                {
                    var value = propertyInfo.GetValue(rawValue);

                    if (value == null)
                        return "null";

                    if (value is IRVA r)
                        value = r.ListedOffset;

                    if (value is IVA v)
                        value = v.ListedAddress;

                    if (value is bool b)
                    {
                        if (cast)
                            return "(byte) " + (b ? 1 : 0);

                        return b ? "true" : "false";
                    }

                    if (propertyInfo.PropertyType.IsEnum)
                    {
                        var items = value.ToString().Split(", ").Select(v => $"{propertyInfo.PropertyType.Name}.{v}").ToArray();

                        if (items.Length == 1)
                            return items[0];

                        return "(" + string.Join(" | ", items) + ")";
                    }

                    var typeCode = Type.GetTypeCode(propertyInfo.PropertyType);

                    string GetTypeName(TypeCode typeCode)
                    {
#pragma warning disable CS8509
                        return typeCode switch
#pragma warning restore CS8509
                        {
                            TypeCode.SByte => "sbyte",
                            TypeCode.Byte => "byte",
                            TypeCode.Int16 => "short",
                            TypeCode.UInt16 => "ushort",
                            TypeCode.Int32 => "int",
                            TypeCode.UInt32 => "uint",
                            TypeCode.Int64 => "long",
                            TypeCode.UInt64 => "ulong",
                        };
                    }

                    switch (typeCode)
                    {
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                            if (cast && typeCode != TypeCode.Int32)
                                return "(" + GetTypeName(typeCode) + ") " + value;

                            return value.ToString();
                    }

                    if (value is string s)
                        return $"\"{s}\"";

                    if (value is Guid g)
                        return $"new Guid(\"{g}\")";

                    if (propertyInfo.PropertyType.IsArray)
                    {
                        var arr = (Array) value;

                        var elementType = propertyInfo.PropertyType.GetElementType();

                        var tc = Type.GetTypeCode(elementType);

                        if (tc == TypeCode.Object)
                            return "null";

                        var typeName = GetTypeName(tc);

                        var arrayBuilder = new StringBuilder();
                        arrayBuilder.Append("new ").Append(typeName).Append("[]{");

                        for (var i = 0; i < arr.Length; i++)
                        {
                            var item = arr.GetValue(i);

                            arrayBuilder.Append(item);

                            if (i < arr.Length - 1)
                                arrayBuilder.Append(", ");
                        }

                        arrayBuilder.Append("}");

                        return arrayBuilder.ToString();
                    }

                    return value.ToString();
                }

                for (var i = 0; i < properties.Length; i++)
                {
                    var value = GetValue(properties[i], false);

                    builder.Append("    v => v.").Append(properties[i].Name).Append(" == ").Append(value);

                    if (i < properties.Length - 1)
                        builder.Append(",");

                    builder.AppendLine();
                }

                builder.AppendLine(");");
                builder.AppendLine();

                string CalculateName(string name)
                {
                    var chunks = new List<string>();

                    var chars = name.ToCharArray();

                    var chunkStart = 0;

                    for (var i = 0; i < chars.Length; i++)
                    {
                        chunkStart = i;

                        //Find the first lowercase letter

                        int j = i + 1;

                        for (; j < chars.Length; j++)
                        {
                            if (char.IsLower(chars[j]))
                                break;
                        }

                        //Read characters until we either reach the end, or reach another uppercase letter

                        for (; j < chars.Length; j++)
                        {
                            if (char.IsUpper(chars[j]))
                                break;
                        }

                        var str = name.Substring(chunkStart, j - chunkStart);

                        chunks.Add(str.ToUpper());

                        i = j - 1;
                    }

                    return string.Join("_", chunks);
                }

                builder.Append("TestView<").Append(typeof(T).Name).AppendLine(">(");

                builder.AppendLine("    v => v.VerifyStruct(");
                builder.AppendLine($"        name: \"{CalculateName(typeof(T).Name)}\", offset: {rawValue.Offset}, size: 0,");

                for (var i = 0; i < properties.Length; i++)
                {
                    var value = GetValue(properties[i], true);

                    builder.Append($"        c => c.VerifyField(name: \"{properties[i].Name}\", value: {value})");

                    if (i < properties.Length - 1)
                        builder.Append(",");

                    builder.AppendLine();
                }

                builder.AppendLine("    )");
                builder.Append(");");

                return builder.ToString();
            }
            finally
            {
                fs?.Dispose();
            }
        }

        private TVerifier GetStruct<TSelector, TVerifier>(out FileStream fs)
        {
            var t = typeof(TSelector);

            string name;

            if (t.IsGenericType)
                name = t.GetGenericArguments()[0].Name;
            else
            {
                name = typeof(TSelector).Name;

                if (t.DeclaringType != null)
                    name = t.DeclaringType.Name + "." + name;
            }

            object rawValue = name switch
            {
                #region DOS Header

                nameof(ImageDosHeader) => (object) GetFile(WellKnownTestModule.Ntdll, out fs).DosHeader,

                #endregion
                #region Rich Header

                nameof(RichHeader) => (object) GetFile(WellKnownTestModule.Ntdll, out fs).RichHeader,
                nameof(ProdItem) => (object) GetFile(WellKnownTestModule.Ntdll, out fs).RichHeader.Items[1], //The first item is empty

                #endregion
                #region NT Headers

                nameof(ImageNtHeaders) => GetFile(WellKnownTestModule.Ntdll, out fs).NtHeaders,
                nameof(ImageFileHeader) => GetFile(WellKnownTestModule.Ntdll, out fs).NtHeaders.FileHeader,
                nameof(ImageOptionalHeader) => GetFile(WellKnownTestModule.Ntdll, out fs).OptionalHeader,
                nameof(ImageDataDirectory) => GetFile(WellKnownTestModule.Ntdll, out fs).OptionalHeader.ExportTableDirectory,

                #endregion
                #region Section Headers

                nameof(ImageSectionHeader) => GetFile(WellKnownTestModule.Ntdll, out fs).SectionHeaders[0],

                #endregion
                #region Export Table (0)

                nameof(ImageExportDirectory) => GetFile(WellKnownTestModule.Ntdll, out fs).ExportTable,

                #endregion
                #region Import Table (1)

                nameof(ImageImportDescriptor) => GetFile(WellKnownTestModule.AzureAttest, out fs).ImportTable[0],
                nameof(ImageImportByName)     => GetFile(WellKnownTestModule.AzureAttest, out fs).ImportTable[1].OriginalFirstThunk.Value[0].Name.Value,

                #endregion
                #region Resource Directory (2)

                nameof(ImageResourceDirectory)       => GetFile(WellKnownTestModule.Ntdll, out fs).ResourceDirectory!.Entries[0].OffsetToDirectory.Value,
                nameof(ImageResourceDirectoryEntry)  => GetFile(WellKnownTestModule.Ntdll, out fs).ResourceDirectory!.Entries[0],
                nameof(ImageResourceDataEntry)       => GetFile(WellKnownTestModule.Ntdll, out fs).ResourceDirectory!.Entries[2].OffsetToDirectory.Value.Entries[0].OffsetToDirectory.Value.Entries[0].OffsetToData.Value,
                nameof(ImageResourceDirStringU)      => GetFile(WellKnownTestModule.Ntdll, out fs).ResourceDirectory!.Entries[0].NameOrId.NameOffset.Value,

                nameof(ClrDebugResource)             => GetFile(WellKnownTestModule.coreclr, out fs).ResourceDirectory!.EnumerateResources<ClrDebugResource>().First(),

                nameof(VsVersionInfo)                => GetFile(WellKnownTestModule.Ntdll, out fs).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First(),
                nameof(VsFixedFileInfo)              => GetFile(WellKnownTestModule.Ntdll, out fs).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First().Value,
                nameof(VsVersionInfo.StringFileInfo) => GetFile(WellKnownTestModule.Ntdll, out fs).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First().Children[0],
                nameof(VsVersionInfo.StringTable)    => ((VsVersionInfo.StringFileInfo) GetFile(WellKnownTestModule.Ntdll, out fs).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First().Children[0]).Children[0],
                nameof(VsVersionInfo.String)         => ((VsVersionInfo.StringFileInfo) GetFile(WellKnownTestModule.Ntdll, out fs).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First().Children[0]).Children[0].Children[0],
                nameof(VsVersionInfo.VarFileInfo)    => GetFile(WellKnownTestModule.Ntdll, out fs).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First().Children[1],
                nameof(VsVersionInfo.Var)            => ((VsVersionInfo.VarFileInfo) GetFile(WellKnownTestModule.Ntdll, out fs).ResourceDirectory!.EnumerateResources<VsVersionInfo>().First().Children[1]).Children[0],

                #endregion
                #region Exception Table (3)

                nameof(RuntimeFunction)              => GetFile(WellKnownTestModule.Ntdll, out fs).ExceptionTable[0],
                nameof(UnwindInfo)                   => GetFile(WellKnownTestModule.Ntdll, out fs).ExceptionTable[28].UnwindData.Value,
                nameof(ScopeTable)                   => GetFile(WellKnownTestModule.Ntdll, out fs).ExceptionTable[28].UnwindData.Value.ExceptionData,
                nameof(ScopeTable.ScopeRecord)       => ((ScopeTable) GetFile(WellKnownTestModule.Ntdll, out fs).ExceptionTable[28].UnwindData.Value.ExceptionData).Records[0],

                nameof(FuncInfo)                     => ((RVA<FuncInfo>) GetFile(WellKnownTestModule.AuthExt, out fs).ExceptionTable[74].UnwindData.Value.ExceptionData).Value,

                #endregion
                #region Security Table (4)

                nameof(WinCertificate)               => GetFile(WellKnownTestModule.Ntdll, out fs).SecurityTable[0],
                nameof(SignedData)                   => GetFile(WellKnownTestModule.Ntdll, out fs).SecurityTable[0].Certificate,

                #endregion
                #region Base Relocation Table (5)

                nameof(ImageBaseRelocation) => GetFile(WellKnownTestModule.Ntdll, out fs).BaseRelocationTable?[6],

                #endregion
                #region Debug Table (6)

                nameof(ImageDebugDirectory)       => GetFile(WellKnownTestModule.Ntdll, out fs).DebugTable?[0],
                nameof(RSDSI)                     => (RSDSI?)                     GetFile(WellKnownTestModule.Ntdll, out fs).DebugTable?.First(t => t.Type == ImageDebugType.CodeView).Data,
                nameof(NB10I)                     => (NB10I?)                     GetFile(WellKnownTestModule.crtdll, out fs).DebugTable?.First(t => t.Type == ImageDebugType.CodeView).Data,
                nameof(FpoData)                   => ((FpoData[])                 GetFile(WellKnownTestModule.ctl3d32, out fs).DebugTable?.First(t => t.Type == ImageDebugType.FPO).Data)?[0],
                nameof(ImageDebugMisc)            => (ImageDebugMisc)             GetFile(WellKnownTestModule.mfc40, out fs).DebugTable?.First(t => t.Type == ImageDebugType.Misc).Data,
                //nameof(VCFeature)                 => (VCFeature)                  GetFile(WellKnownTestModule.Ntdll, out fs).DebugTable?.First(t => t.Type == ImageDebugType.VCFeature).Data,
                //POGO
                nameof(Reproducible)              => (Reproducible?)              GetFile(WellKnownTestModule.Ntdll, out fs).DebugTable?.First(t => t.Type == ImageDebugType.Reproducible).Data,
                //nameof(EmbeddedPortablePdb)       => (EmbeddedPortablePdb?)       GetFile(WellKnownTestModule.Ntdll, out fs).DebugTable?.First(t => t.Type == ImageDebugType.EmbeddedPortablePdb).Data,
                //nameof(PdbChecksum)               => (PdbChecksum?)               GetFile(WellKnownTestModule.Ntdll, out fs).DebugTable?.First(t => t.Type == ImageDebugType.PdbChecksum).Data,
                nameof(ImageDllCharacteristicsEx) => (GetFile(WellKnownTestModule.Ntdll, out fs).DebugTable?.First(t => t.Type == ImageDebugType.ExDllCharacteristics)),

                #endregion
                #region Copyright Table (7)
                #endregion
                #region Global Pointer Table (8)
                #endregion
                #region Thread Local Storage Table (9)

                nameof(ImageTlsDirectory) => GetFile(WellKnownTestModule.AzureAttest, out fs).TlsDirectory,

                #endregion
                #region Load Config Table (10)

                nameof(ImageLoadConfigDirectory)       => GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable,
                nameof(ImageLoadConfigCodeIntegrity)   => GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable?.CodeIntegrity,

                nameof(ImageEnclaveConfig)             => GetFile(WellKnownTestModule.AzureAttest, out fs).LoadConfigTable!.EnclaveConfigurationPointer.Value,
                nameof(ImageEnclaveImport)             => GetFile(WellKnownTestModule.AzureAttest, out fs).LoadConfigTable!.EnclaveConfigurationPointer.Value.ImportList.Value[0],

                //nameof(GuardAddressTakenIatEntryTable) => GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable?.GuardAddressTakenIatEntryTable.Value,
                //"GuardAddressTakenIatEntryTable.Entry" => GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable?.GuardAddressTakenIatEntryTable.Value.Entries[0],
                nameof(GuardCFFunctionTable)           => GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable?.GuardCFFunctionTable.Value,
                "GuardCFFunctionTable.Entry"           => GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable?.GuardCFFunctionTable.Value.Entries[0],
                nameof(GuardEHContinuationTable)       => GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable?.GuardEHContinuationTable.Value,
                "GuardEHContinuationTable.Entry"       => GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable?.GuardEHContinuationTable.Value.Entries[0],
                //nameof(GuardLongJumpTargetTable)       => GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable?.GuardLongJumpTargetTable.Value,
                //"ameof(GuardLongJumpTargetTable.Entry" => GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable?.GuardLongJumpTargetTable.Value.Entries[0],

                nameof(ImageDynamicRelocationTable)    => GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable?.DynamicValueRelocTableOffset.Value,
                nameof(ImageDynamicRelocation)         => GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable?.DynamicValueRelocTableOffset.Value.DynamicRelocations[0],
                //ImageDynamicRelocationV2

                #region Symbol 1

                //ImagePrologueDynamicRelocationHeader

                #endregion
                #region Symbol 2

                //ImageEpilogueDynamicRelocationHeader

                #endregion
                #region Symbol 3

                nameof(ImageImportControlTransferDynamicRelocation) => ((ImageBaseRelocation<ImageImportControlTransferDynamicRelocation>[]) GetFile(WellKnownTestModule.cdd, out fs).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[0].Data)[0].Entries[0],

                #endregion
                #region Symbol 4

                nameof(ImageIndirControlTransferDynamicRelocation) => ((ImageBaseRelocation<ImageIndirControlTransferDynamicRelocation>[]) GetFile(WellKnownTestModule.kdstub, out fs).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[0].Data)[0].Entries[0],

                #endregion
                #region Symbol 5

                nameof(ImageSwitchTableBranchDynamicRelocation)    => (((ImageBaseRelocation<ImageSwitchTableBranchDynamicRelocation>[]) GetFile(WellKnownTestModule.kd_02_15b3, out fs).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[1].Data)[0]).Entries[0],

                #endregion
                #region Symbol 7

                nameof(ImageFunctionOverrideHeader)            => (ImageFunctionOverrideHeader)  GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[0].Data,
                nameof(ImageFunctionOverrideDynamicRelocation) => ((ImageFunctionOverrideHeader) GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[0].Data).FuncOverrides[0],
                nameof(ImageBDDInfo)                           => ((ImageFunctionOverrideHeader) GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[0].Data).BDDInfo,
                nameof(ImageBDDDynamicRelocation)              => ((ImageFunctionOverrideHeader) GetFile(WellKnownTestModule.Ntdll, out fs).LoadConfigTable!.DynamicValueRelocTableOffset.Value.DynamicRelocations[0].Data).BDDInfo.BDDNodes[0],

                #endregion
                #endregion
                #region Bound Import Table (11)

                nameof(ImageBoundImportDescriptor) => GetFile(WellKnownTestModule.mfc40u, out fs).BoundImportTable?[0],

                #endregion
                #region Import Address Table (12)
                #endregion
                #region Delay Import Table (13)
                #endregion
                #region Cor Header (14)

                nameof(ImageCor20Header) => GetFile(WellKnownTestModule.mscorlib, out fs).Cor20Header,
                nameof(StorageSignature) => GetFile(WellKnownTestModule.mscorlib, out fs).Cor20Header!.Metadata.Data.Signature,
                nameof(StorageHeader)    => GetFile(WellKnownTestModule.mscorlib, out fs).Cor20Header!.Metadata.Data.Header,
                //nameof(ImageCorILMethod) => GetFile(WellKnownTestModule.mscorlib, out fs).ILMethods.First(),
                nameof(StorageStream)    => GetFile(WellKnownTestModule.mscorlib, out fs).Cor20Header!.Metadata.Data.Header.StreamHeaders[0],

                #endregion
                //_ => throw new NotImplementedException($"Don't know how to handle type '{typeof(T).Name}'")
                _ => throw new AssertInconclusiveException($"Don't know how to handle type '{typeof(TSelector).Name}'")
            };

            return (TVerifier) rawValue;
        }

        static PEFile GetFile(SymbolStoreKey key, out FileStream fs)
        {
            var path = WellKnownTestModule.GetStoreFile(key);

            fs = File.OpenRead(path);

            var peFile = PEFile.FromStream(fs, false);

            return peFile;
        }

        private void TestBytes(string fieldName, byte[] expected)
        {
            FileStream fs;

            ByteBlob rawValue = fieldName switch
            {
                nameof(PEFile.DosStub) => GetFile(WellKnownTestModule.Ntdll, out fs).DosStub
            };

            try
            {
                var expectedStr = string.Join(", ", expected.Select(v => "0x" + v.ToString("X2")));
                var actualStr = string.Join(", ", rawValue.Bytes.Select(v => "0x" + v.ToString("X2")));

                Assert.AreEqual(expectedStr, actualStr);
            }
            finally
            {
                fs.Dispose();
            }
        }

        private MemberInfo GetPropertyInfo(Expression expression, ref object rawValue)
        {
            while (expression is UnaryExpression e)
                expression = e.Operand;

            if (expression is not MemberExpression m)
            {
            }

            var exprs = new List<MemberExpression>();

            var mm = m.Expression;

            while (mm is MemberExpression m2)
            {
                exprs.Add(m2);
                mm = m2.Expression;
            }

            exprs.Reverse();

            foreach (var expr in exprs)
                rawValue = ((PropertyInfo) expr.Member).GetValue(rawValue);

            if (mm is UnaryExpression)
            {
                var unary = GetPropertyInfo(mm, ref rawValue);

                rawValue = ((PropertyInfo) unary).GetValue(rawValue);
            }

            return m.Member;
        }

        private object GetConstantValue(Expression expression, Type type)
        {
            object value;

            if (expression is ConstantExpression c)
                value = c.Value;
            else
            {
                //Compile it
                value = Expression.Lambda(expression).Compile().DynamicInvoke();
            }

            var typeCode = Type.GetTypeCode(type);

            value = typeCode switch
            {
                TypeCode.SByte => Convert.ToSByte(value),
                TypeCode.Byte => Convert.ToByte(value),
                TypeCode.Int16 => Convert.ToInt16(value),
                TypeCode.UInt16 => Convert.ToUInt16(value),
                _ => value
            };

            if (type.IsEnum && value != null)
                value = Enum.Parse(type, value.ToString());

            return value;
        }

        private IEnumerable<string> FindPEFile(Predicate<PEFile> predicate, string path = "C:\\Windows")
        {
            var files = Directory.EnumerateFiles(path, "*.dll", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true
            });

            foreach (var file in files)
            {
                using var fs = File.OpenRead(file);

                if (fs.Length == 0)
                    continue;

                bool result;

                try
                {
                    using var peFile = PEFile.FromStream(fs, false);

                    result = predicate(peFile);
                }
                catch (BadImageFormatException)
                {
                    continue;
                }

                if (result)
                    yield return file;
            }
        }

        private T[] GetSpan<T>(object value, string field)
        {
            //I tried using reflection; the trick where you try and create a delegate out of the PropertyInfo
            //Getter (thereby preventing the Span from being boxed) did not work for me

            if (value is ImageDosHeader a)
            {
                return field switch
                {
                    "ReservedWords" => Unsafe.As<T[]>(a.ReservedWords.ToArray()),
                    "ReservedWords2" => Unsafe.As<T[]>(a.ReservedWords2.ToArray())
                };
            }
        }

        private Action<IView>[] WithIgnores(Action<IView> action, int after) => WithIgnores(0, action, after);

        private Action<IView>[] WithIgnores(Action<IView>[] action, int after) =>
            WithIgnores(0, action, after);

        private Action<IView>[] WithIgnores(int before, Action<IView> action, int after = 0) =>
            WithIgnores(before, new[] { action }, after);

        private Action<IView>[] WithIgnores(int before, Action<IView>[] action, int after = 0)
        {
            var list = new List<Action<IView>>();

            for (var i = 0; i < before; i++)
                list.Add(v => { });

            list.AddRange(action);

            for (var i = 0; i < after; i++)
                list.Add(v => { });

            return list.ToArray();
        }

        private Action<IView>[] IgnoreValues(int count)
        {
            var result = new Action<IView>[count];

            for (var i = 0; i < count; i++)
                result[i] = v => { };

            return result;
        }
#pragma warning restore HAA0101 // Array allocation for params parameter
}
