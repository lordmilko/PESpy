using System;
using ClrDebug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.Ecma335;
using PESpy.View;

namespace PESpy.Tests
{
    [TestClass]
    public class PEFileTests : BaseTest
    {
        #region DOS Header

        [TestMethod]
        public void ImageDosHeader_Test()
        {
            TestStruct<ImageDosHeader>(
                v => v.Magic == ImageDosHeader.IMAGE_DOS_SIGNATURE,
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
                    c => c.VerifyField(name: "e_magic", value: (ushort) 23117),
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
                v => v.NumberOfSections == (ushort) 11,
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
                    c => c.VerifyField(name: "NumberOfSections", value: (ushort) 11),
                    c => c.VerifyField(name: "TimeDateStamp", value: (uint) 3169667970),
                    c => c.VerifyField(name: "PointerToSymbolTable", value: 0),
                    c => c.VerifyField(name: "NumberOfSymbols", value: 0),
                    c => c.VerifyField(name: "SizeOfOptionalHeader", value: (short) 240),
                    c => c.VerifyField(name: "Characteristics", value: ImageFile.ExecutableImage | ImageFile.LargeAddressAware | ImageFile.Dll)
                )
            );

            TestXRefs<ImageFileHeader>(
                v => v.Verify(propertyName: "PointerToSymbolTable", index: 0, fieldOffset: ImageFileHeader.PointerToSymbolTableOffset, targetOffset: 61472)
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
        }

        [TestMethod]
        public void ImageOptionalHeader_x86_And_x64()
        {
            //The layout of ImageOptionalHeader is different in x86 and x64. Test that both give expected output

            using var pe32  = PEFile.FromFile($"C:\\Windows\\{(IntPtr.Size == 4 ? "system32" : "SysWow64")}\\ntdll.dll");
            using var pe64  = PEFile.FromFile($"C:\\Windows\\{(IntPtr.Size == 4 ? "sysnative" : "system32")}\\ntdll.dll");

            var base32 = pe32.OptionalHeader.ImageBase;
            var base64 = pe64.OptionalHeader.ImageBase;

            Assert.AreEqual(0x4b280000, base32);
            Assert.AreEqual(0x180000000, base64);
        }

        //It's theoretically possible to have an ImageOptionalHeader with less than 16 directories. In practice, I haven't ever seen this, but
        //once we have a PEFileBuilder we should test for this

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
                v => v.PointerToRelocations.ListedAddress == 0,
                v => v.PointerToLineNumbers.ListedAddress == 0,
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

            //I can't find any files (in my samples or otherwise) that have either of these, so for now I can't test them

            //TestXRefs<ImageSectionHeader>(
            //    v => v.Verify(propertyName: "PointerToRelocations", index: -1, fieldOffset: ImageSectionHeader.PointerToRelocationsOffset, targetOffset: 0),
            //    v => v.Verify(propertyName: "PointerToLineNumbers", index: -1, fieldOffset: ImageSectionHeader.PointerToLineNumbersOffset, targetOffset: 0)
            //);
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

            TestXRefs<ImageExportDirectory>(
                v => v.Verify(propertyName: "Name",                  index: 0, fieldOffset: ImageExportDirectory.NameOffset,                  targetOffset: 1514622),
                v => v.Verify(propertyName: "AddressOfFunctions",    index: 1, fieldOffset: ImageExportDirectory.AddressOfFunctionsOffset,    targetOffset: 1489768),
                v => v.Verify(propertyName: "AddressOfNames",        index: 2, fieldOffset: ImageExportDirectory.AddressOfNamesOffset,        targetOffset: 1499712),
                v => v.Verify(propertyName: "AddressOfNameOrdinals", index: 3, fieldOffset: ImageExportDirectory.AddressOfNameOrdinalsOffset, targetOffset: 1509652)
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
                        name: "IMAGE_IMPORT_DESCRIPTOR", offset: 236888, size: 20,
                        c => c.VerifyField(name: "OriginalFirstThunk", value: 237080),
                        c => c.VerifyField(name: "TimeDateStamp", value: (uint) 0),
                        c => c.VerifyField(name: "ForwarderChain", value: 0),
                        c => c.VerifyField(name: "Name", value: 237918),
                        c => c.VerifyField(name: "FirstThunk", value: 196224)
                    )
                },
                false
            );

            TestXRefs<ImageImportDescriptor>(
                v => v.Verify(propertyName: "OriginalFirstThunk", index: 0, fieldOffset: ImageImportDescriptor.OriginalFirstThunkOffset, targetOffset: 237080),
                v => v.Verify(propertyName: "Name",               index: 1, fieldOffset: ImageImportDescriptor.NameOffset, targetOffset: 237918),
                v => v.Verify(propertyName: "FirstThunk",         index: 2, fieldOffset: ImageImportDescriptor.FirstThunkOffset, targetOffset: 196224)
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
                        name: "IMAGE_IMPORT_DESCRIPTOR", offset: 236888, size: 20,
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

            TestXRefs<ImageResourceDirectoryEntry>(
                v => v.Verify(propertyName: "OffsetToData", index: -1, fieldOffset: ImageResourceDirectoryEntry.DataAndDirectoryOffset, targetOffset: 0),
                v => v.Verify(propertyName: "OffsetToDirectory", index: -1, fieldOffset: ImageResourceDirectoryEntry.DataAndDirectoryOffset, targetOffset: 0)
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

            TestXRefs<ImageResourceDataEntry>(
                v => v.Verify(propertyName: "OffsetToData", index: -1, fieldOffset: 0, targetOffset: 0)
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
                v => v.Value == IgnoreValue
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

            TestXRefs<RuntimeFunction>(
                v => v.Verify(propertyName: "UnwindData", index: 0, fieldOffset: RuntimeFunction.UnwindDataOffset, targetOffset: 1424960)
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

        #region UnwindCode Types

        [TestMethod]
        public void UnwindCode_PushNonVolatile_Test()
        {
            TestStruct<UnwindCode.PushNonVolatile>(
                v => v.Register == UnwindInfo.X64Register.R15,
                v => v.OpInfo == 15,
                v => v.CodeOffset == 11,
                v => v.UnwindOp == UWOP.PUSH_NONVOL,
                v => v.StructSize == 2
            );

            TestView<UnwindCode.PushNonVolatile>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1424968, size: 2,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 11),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.PUSH_NONVOL, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: (byte) 15, bits: 4)
                )
            );
        }

        [TestMethod]
        public void UnwindCode_AllocLarge_Test()
        {
            TestStruct<UnwindCode.AllocLarge>(
                v => v.Size == 57353,
                v => v.StructSize == 4,
                v => v.CodeOffset == 26,
                v => v.UnwindOp == UWOP.ALLOC_LARGE,
                v => v.OpInfo == 0
            );

            TestView<UnwindCode.AllocLarge>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1424964, size: 4,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 26),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.ALLOC_LARGE, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: (byte) 0, bits: 4),
                    c => c.VerifyField(name: "Size", value: (ushort) 57353)
                )
            );
        }

        [TestMethod]
        public void UnwindCode_AllocSmall_Test()
        {
            TestStruct<UnwindCode.AllocSmall>(
                v => v.Size == 48,
                v => v.OpInfo == 5,
                v => v.CodeOffset == 6,
                v => v.UnwindOp == UWOP.ALLOC_SMALL,
                v => v.StructSize == 2
            );

            TestView<UnwindCode.AllocSmall>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1425036, size: 2,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 6),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.ALLOC_SMALL, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: (byte) 5, bits: 4)
                )
            );
        }

        [TestMethod]
        public void UnwindCode_SetFpReg_Test()
        {
            TestStruct<UnwindCode.SetFpReg>(
                v => v.FrameRegister == UnwindInfo.X64Register.RBP,
                v => v.FrameOffset == 96,
                v => v.CodeOffset == 22,
                v => v.UnwindOp == UWOP.SET_FPREG,
                v => v.OpInfo == 6,
                v => v.StructSize == 2
            );

            TestView<UnwindCode.SetFpReg>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1433948, size: 2,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 22),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.SET_FPREG, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: (byte) 6, bits: 4)
                )
            );
        }

        [TestMethod]
        public void UnwindCode_SaveNonVolatile_Test()
        {
            TestStruct<UnwindCode.SaveNonVolatile>(
                v => v.Register == UnwindInfo.X64Register.RBP,
                v => v.OpInfo == 5,
                v => v.StackOffset == 59,
                v => v.StructSize == 4,
                v => v.CodeOffset == 21,
                v => v.UnwindOp == UWOP.SAVE_NONVOL
            );

            TestView<UnwindCode.SaveNonVolatile>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1425012, size: 4,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 21),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.SAVE_NONVOL, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: (byte) 5, bits: 4),
                    c => c.VerifyField(name: "StackOffset", value: (ushort) 59)
                )
            );
        }

        [TestMethod]
        public void UnwindCode_Epilog_Test()
        {
            TestStruct<UnwindCode.Epilog>(
                v => v.CodeOffset == 12,
                v => v.UnwindOp == UWOP.UWOP_EPILOG,
                v => v.OpInfo == 1,
                v => v.StructSize == 2
            );

            TestView<UnwindCode.Epilog>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1474012, size: 2,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 12),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.UWOP_EPILOG, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: (byte) 1, bits: 4)
                )
            );
        }

        [TestMethod]
        public void UnwindCode_SaveXmm128_Test()
        {
            TestStruct<UnwindCode.SaveXmm128>(
                v => v.Register == UnwindInfo.X64Register.RSI,
                v => v.OpInfo == 6,
                v => v.StackOffset == 2,
                v => v.StructSize == 4,
                v => v.CodeOffset == 36,
                v => v.UnwindOp == UWOP.SAVE_XMM128
            );

            TestView<UnwindCode.SaveXmm128>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1453140, size: 4,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 36),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.SAVE_XMM128, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: (byte) 6, bits: 4),
                    c => c.VerifyField(name: "StackOffset", value: (ushort) 2)
                )
            );
        }

        [TestMethod]
        public void UnwindCode_PushMachFrame_Test()
        {
            TestStruct<UnwindCode.PushMachFrame>(
                v => v.CodeOffset == 0,
                v => v.UnwindOp == UWOP.PUSH_MACHFRAME,
                v => v.OpInfo == 0,
                v => v.StructSize == 2
            );

            TestView<UnwindCode.PushMachFrame>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1473504, size: 2,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 0),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.PUSH_MACHFRAME, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: (byte) 0, bits: 4)
                )
            );
        }

        //Various NVIDIA assemblies have SaveNonVolatileFar and SaveXmm128Far, but I didn't see it in any Microsoft
        //assemblies in Windows 11

        //Don't test the "Null" unwind code; it just represents alignment

        #endregion

        [TestMethod]
        public void ScopeTable_Test()
        {
            TestStruct<ScopeTable>(
                v => v.Count == 2,
                v => v.Records == IgnoreValue
            );

            TestView<ScopeTable>(
                v => v.VerifyStruct(
                    name: "SCOPE_TABLE", offset: 1425940, size: 36,
                    WithIgnores(
                        c => c.VerifyField(name: "Count", value: 2),
                        after: 2
                    )
                )
            );
        }

        [TestMethod]
        public void ScopeRecord_Test()
        {
            TestStruct<ScopeTable.ScopeRecord>(
                v => v.BeginAddress == 12717,
                v => v.EndAddress == 12744,
                v => v.HandlerAddress == 1,
                v => v.JumpTarget == 12744
            );

            TestView<ScopeTable.ScopeRecord>(
                v => v.VerifyStruct(
                    name: "ScopeRecord", offset: 1425944, size: 16,
                    c => c.VerifyField(name: "BeginAddress", value: 12717),
                    c => c.VerifyField(name: "EndAddress", value: 12744),
                    c => c.VerifyField(name: "HandlerAddress", value: 1),
                    c => c.VerifyField(name: "JumpTarget", value: 12744)
                )
            );
        }

        [TestMethod]
        public void FuncInfo_Test()
        {
            //The headers for FuncInfo can be conditionally compiled with pointers,
            //so ideally we want to check if we can parse both x86 and x64, however
            //the x86 AuthExt doesn't have an ExceptionTable! Perhaps that's expected

            TestStruct<FuncInfo>(
                v => v.MagicNumber == 429065506,
                v => v.BBTFlags == 0,
                v => v.MaxState == 2,
                v => v.UnwindMap.ListedOffset == 67060,
                v => v.nTryBlocks == 1,
                v => v.TryBlockMap.ListedOffset == 67076,
                v => v.nIPMapEntries == 1,
                v => v.IPToStateMap.ListedOffset == 67120,
                v => v.DispUnwindHelp == 32,
                v => v.DispESTypeList == 0,
                v => v.EHFlags == 5
            );

            TestView<FuncInfo>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "FuncInfo", offset: 60816, size: 40,
                        c => c.VerifyBitField(name: "magicNumber", value: 429065506, bits: 29),
                        c => c.VerifyBitField(name: "bbtFlags", value: 0, bits: 3),
                        c => c.VerifyField(name: "maxState", value: 2),
                        c => c.VerifyField(name: "dispUnwindMap", value: 67060),
                        c => c.VerifyField(name: "nTryBlocks", value: 1),
                        c => c.VerifyField(name: "dispTryBlockMap", value: 67076),
                        c => c.VerifyField(name: "nIPMapEntries", value: 1),
                        c => c.VerifyField(name: "dispIPtoStateMap", value: 67120),
                        c => c.VerifyField(name: "dispUnwindHelp", value: 32),
                        c => c.VerifyField(name: "dispESTypeList", value: 0),
                        c => c.VerifyField(name: "EHFlags", value: 5)
                    ),
                    after: 5
                )
            );

            //There's 4 xrefs: the third one is the TryBlockMap.HandlerArray
            TestXRefs<FuncInfo>(
                v => v.Verify(propertyName: "UnwindMap",    index: 0, fieldOffset: FuncInfo.UnwindMapOffset, targetOffset: 67060),
                v => v.Verify(propertyName: "TryBlockMap",  index: 1, fieldOffset: FuncInfo.TryBlockMapOffset, targetOffset: 67076),
                v => v.Verify(propertyName: "IPToStateMap", index: 3, fieldOffset: FuncInfo.IPToStateMapOffset, targetOffset: 67120)
            );
        }

        [TestMethod]
        public void TryBlockMapEntry_Test()
        {
            TestStruct<TryBlockMapEntry>(
                v => v.TryLow == 0,
                v => v.TryHigh == 0,
                v => v.CatchHigh == 1,
                v => v.nCatches == 1,
                v => v.HandlerArray.ListedOffset == 67096
            );

            TestView<TryBlockMapEntry>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "TryBlockMapEntry", offset: 67076, size: 20,
                        c => c.VerifyField(name: "tryLow", value: 0),
                        c => c.VerifyField(name: "tryHigh", value: 0),
                        c => c.VerifyField(name: "catchHigh", value: 1),
                        c => c.VerifyField(name: "nCatches", value: 1),
                        c => c.VerifyField(name: "dispHandlerArray", value: 67096)
                    ),
                    after: 1
                )
            );

            TestXRefs<TryBlockMapEntry>(
                v => v.Verify(propertyName: "HandlerArray", index: 0, fieldOffset: TryBlockMapEntry.HandlerArrayOffset, targetOffset: 1)
            );
        }

        [TestMethod]
        public void HandlerType_Test()
        {
            TestStruct<HandlerType>(
                v => v.Adjectives == 64,
                v => v.Type.ListedOffset == 0,
                v => v.CatchObj == 0,
                v => v.Handler == 39879,
                v => v.Frame == 56
            );

            TestView<HandlerType>(
                v => v.VerifyStruct(
                    name: "HandlerType", offset: 67096, size: 20,
                    c => c.VerifyField(name: "adjectives", value: 64),
                    c => c.VerifyField(name: "dispType", value: 0),
                    c => c.VerifyField(name: "dispCatchObj", value: 0),
                    c => c.VerifyField(name: "dispOfHandler", value: 39879),
                    c => c.VerifyField(name: "dispFrame", value: 56)
                )
            );

            TestXRefs<HandlerType>(
                v => v.Verify(propertyName: "Type", index: 0, fieldOffset: HandlerType.TypeOffset, targetOffset: 1527960)
            );
        }

        [TestMethod]
        public void TypeDescriptor_Test()
        {
            TestStruct<TypeDescriptor>(
                v => v.pVFTable == 269732080,
                v => v.Spare == 0,
                v => v.Name == ".?AUCInBufferException@@"
            );

            TestView<TypeDescriptor>(
                v => v.VerifyStruct(
                    name: "TypeDescriptor", offset: 1524376, size: 41,
                    c => c.VerifyField(name: "pVFTable", value: (ulong) 269732080),
                    c => c.VerifyField(name: "spare", value: (ulong) 0),
                    c => c.VerifyField(name: "name", value: ".?AUCInBufferException@@")
                )
            );
        }

        [TestMethod]
        public void IptoStateMapEntry_Test()
        {
            TestStruct<IptoStateMapEntry>(
                v => v.Ip == 15737,
                v => v.State == 0
            );

            TestView<IptoStateMapEntry>(
                v => v.VerifyStruct(
                    name: "IptoStateMapEntry", offset: 133657, size: 8,
                    c => c.VerifyField(name: "Ip", value: 15737),
                    c => c.VerifyField(name: "State", value: 0)
                )
            );
        }

        [TestMethod]
        public void UnwindMapEntry_Test()
        {
            TestStruct<UnwindMapEntry>(
                v => v.ToState == -1,
                v => v.Action == 0
            );

            TestView<UnwindMapEntry>(
                v => v.VerifyStruct(
                    name: "UnwindMapEntry", offset: 67060, size: 8,
                    c => c.VerifyField(name: "toState", value: -1),
                    c => c.VerifyField(name: "action", value: 0)
                )
            );
        }

        [TestMethod]
        public void FuncInfoV1_Test()
        {
            TestStruct<FuncInfoV1>(
                v => v.MagicNumber == 429065504,
                v => v.BBTFlags == 0,
                v => v.MaxState == 2,
                v => v.UnwindMap.ListedOffset == 1297860,
                v => v.nTryBlocks == 0,
                v => v.TryBlockMap.ListedOffset == 0,
                v => v.nIPMapEntries == 5,
                v => v.IPToStateMap.ListedOffset == 1297784
            );

            TestView<FuncInfoV1>(
                WithIgnores(
                    before: 5,
                    v => v.VerifyStruct(
                        name: "FuncInfoV1", offset: 1198144, size: 28,
                        c => c.VerifyBitField(name: "magicNumber", value: 429065504, bits: 29),
                        c => c.VerifyBitField(name: "bbtFlags", value: 0, bits: 3),
                        c => c.VerifyField(name: "maxState", value: 2),
                        c => c.VerifyField(name: "dispUnwindMap", value: 1297860),
                        c => c.VerifyField(name: "nTryBlocks", value: 0),
                        c => c.VerifyField(name: "dispTryBlockMap", value: 0),
                        c => c.VerifyField(name: "nIPMapEntries", value: 5),
                        c => c.VerifyField(name: "dispIPtoStateMap", value: 1297784)
                    ),
                    after: 2
                )
            );

            //There's xrefs; the first, second and last xrefs are ours
            TestXRefs<FuncInfoV1>(
                v => v.Verify(propertyName: "UnwindMap",    index: 0, fieldOffset: FuncInfoV1.UnwindMapOffset, targetOffset: 1305096),
                v => v.Verify(propertyName: "TryBlockMap",  index: 1, fieldOffset: FuncInfoV1.TryBlockMapOffset, targetOffset: 1305016),
                v => v.Verify(propertyName: "IPToStateMap", index: 5, fieldOffset: FuncInfoV1.IPToStateMapOffset, targetOffset: 1304840)
            );
        }

        //Parsing FuncInfo4 is not implemented

        #endregion
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
                v => v.SizeOfBlock == 16
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

        //I haven't been able to find anything that has ImageRelocation

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
            TestStruct<ImageCoffSymbolsHeader>(
                v => v.NumberOfSymbols == 1123,
                v => v.LvaToFirstSymbol.ListedOffset == 32,
                v => v.NumberOfLinenumbers == 0,
                v => v.LvaToFirstLinenumber == 0,
                v => v.RvaToFirstByteOfCode == 4096,
                v => v.RvaToLastByteOfCode == 49152,
                v => v.RvaToFirstByteOfData == 49152,
                v => v.RvaToLastByteOfData == 20480
            );

            TestView<ImageCoffSymbolsHeader>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "IMAGE_COFF_SYMBOLS_HEADER", offset: 61440, size: 32,
                        c => c.VerifyField(name: "NumberOfSymbols", value: 1123),
                        c => c.VerifyField(name: "LvaToFirstSymbol", value: 32),
                        c => c.VerifyField(name: "NumberOfLinenumbers", value: 0),
                        c => c.VerifyField(name: "LvaToFirstLinenumber", value: 0),
                        c => c.VerifyField(name: "RvaToFirstByteOfCode", value: 4096),
                        c => c.VerifyField(name: "RvaToLastByteOfCode", value: 49152),
                        c => c.VerifyField(name: "RvaToFirstByteOfData", value: 49152),
                        c => c.VerifyField(name: "RvaToLastByteOfData", value: 20480)
                    ),
                    after: 1
                )
            );

            TestXRefs<ImageCoffSymbolsHeader>(
                v => v.Verify(propertyName: "LvaToFirstSymbol", index: 0, fieldOffset: ImageCoffSymbolsHeader.LvaToFirstSymbolOffset, targetOffset: 61472)
            );
        }

        #endregion
        #region CodeView (2)

        [TestMethod]
        public void ImageDebugDirectory_CodeView_RSDSI_Test()
        {
            TestStruct<RSDSI>(
                v => v.Signature == CodeViewSig.RSDS,
                v => v.Guid == new Guid("58a282c2-4aee-7e03-a8cf-8cb0a782ce0c"),
                v => v.Age == 1,
                v => v.Path == "ntdll.pdb"
            );

            TestView<RSDSI>(
                v => v.VerifyStruct(
                    name: "RSDSI", offset: 1423344, size: 34,
                    c => c.VerifyField(name: "dwSig", value: CodeViewSig.RSDS),
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
                v => v.Signature == CodeViewSig.NB10,
                v => v.dwOffset == 0,
                v => v.PdbSignature == 988769516,
                v => v.Age == 1,
                v => v.Path == "crtdll.pdb"
            );

            TestView<NB10I>(
                v => v.VerifyStruct(
                    name: "NB10I", offset: 148992, size: 27,
                    c => c.VerifyField(name: "dwSig", value: CodeViewSig.NB10),
                    c => c.VerifyField(name: "dwOffset", value: 0),
                    c => c.VerifyField(name: "sig", value: (uint) 988769516),
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

        //Exception (5)
        //Fixup (6)
        //OmapToSrc (7)
        //OmapFromSrc (8)
        //Borland (9)
        //BBT (10) (also known as Reserved10) always has a size of 4. Format always seems to be BB 00 + 2 more bytes
        //Clsid (11)

        #region VCFeature (12)

        [TestMethod]
        public void ImageDebugDirectory_VCFeature_Test()
        {
            TestStruct<VCFeature>(
                v => v.PreVC11 == 0,
                v => v.C_CPP == 761,
                v => v.GS == 761,
                v => v.SDL == 0,
                v => v.GuardN == 761
            );

            TestView<VCFeature>(
                v => v.VerifyStruct(
                    name: "VCFeature", offset: 7684844, size: 20,
                    c => c.VerifyField(name: "PreVC11", value: 0),
                    c => c.VerifyField(name: "C_CPP", value: 761),
                    c => c.VerifyField(name: "GS", value: 761),
                    c => c.VerifyField(name: "SDL", value: 0),
                    c => c.VerifyField(name: "GuardN", value: 761)
                )
            );
        }

        #endregion
        #region Pogo (13)

        [TestMethod]
        public void ImageDebugDirectory_PogoData_Test()
        {
            TestStruct<PogoData>(
                v => v.Signature == PogoSignatureKind.LCTG,
                v => v.Entries == IgnoreValue
            );

            TestView<PogoData>(
                v => v.VerifyStruct(
                    name: "PogoData", offset: 7684864, size: 1316,
                    WithIgnores(
                        c => c.VerifyField(name: "Signature", value: PogoSignatureKind.LCTG),
                        after: 131
                    )
                )
            );
        }

        [TestMethod]
        public void ImageDebugDirectory_PogoItem_Test()
        {
            TestStruct<PogoItem>(
                v => v.RVA == 4096,
                v => v.Size == 1616,
                v => v.Name == ".text$di"
            );

            TestView<PogoItem>(
                v => v.VerifyStruct(
                    name: "PogoItem", offset: 7684868, size: 17,
                    c => c.VerifyField(name: "RVA", value: 4096),
                    c => c.VerifyField(name: "Size", value: 1616),
                    c => c.VerifyField(name: "Name", value: ".text$di")
                )
            );
        }

        #endregion

        //ILTCG (14)
        //MPX (15)

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

        #endregion
        #region EmbeddedPortablePdb (17)

        [TestMethod]
        public void ImageDebugDirectory_EmbeddedPortablePdb_Test()
        {
            TestStruct<EmbeddedPortablePdb>(
                v => v.Signature == 1111773261,
                v => v.UncompressedSize == 10368
            );

            TestView<EmbeddedPortablePdb>(
                v => v.VerifyStruct(
                    name: "Embedded Portable PDB", offset: 1971, size: 6048,
                    c => c.VerifyField(name: "Signature", value: 1111773261),
                    c => c.VerifyField(name: "UncompressedSize", value: 10368),
                    c => c.VerifyFieldIgnoreValue(name: "PortablePdbImage")
                )
            );
        }

        #endregion

        //SPGO (18)

        #region PdbChecksum (19)

        [TestMethod]
        public void ImageDebugDirectory_PdbChecksum_Test()
        {
            TestStruct<PdbChecksum>(
                v => v.AlgorithmName == "SHA256"
            );

            TestView<PdbChecksum>(
                v => v.VerifyStruct(
                    name: "PdbChecksum", offset: 5752, size: 39,
                    c => c.VerifyField(name: "AlgorithmName", value: "SHA256"),
                    c => c.VerifyFieldIgnoreValue(name: "Checksum")
                )
            );
        }

        #endregion
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

        //R2RPerfMap (21)

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
                    c => c.VerifyField(name: "StartAddressOfRawData", value: (ulong) 6442675092),
                    c => c.VerifyField(name: "EndAddressOfRawData", value: (ulong) 6442675100),
                    c => c.VerifyField(name: "AddressOfIndex", value: (ulong) 6442697760),
                    c => c.VerifyField(name: "AddressOfCallBacks", value: (ulong) 6442651784),
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
                v => v.GuardMemcpyFunctionPointer.ListedAddress == (long) 0
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

            using var peFile32 = PEFile.FromKey(WellKnownTestModule.aadauthhelper);
            using var peFile64 = PEFile.FromKey(WellKnownTestModule.ntdll);

            var cfg32 = peFile32.LoadConfigTable;
            var cfg64 = peFile64.LoadConfigTable;

            TestXRefs<ImageLoadConfigDirectory>(

                //Haven't been able to find anything with LockPrefixTable
                //v => v.Verify(propertyName: "LockPrefixTable",                          index: -1, fieldOffset: cfg32.LockPrefixTableOffset,                          targetOffset: 0),

                v => v.Verify(propertyName: "SecurityCookie",                           index: 0, fieldOffset: cfg64.SecurityCookieOffset,                           targetOffset: 1664304), //ntdll
                v => v.Verify(propertyName: "SEHandlerTable",                           index: 1, fieldOffset: cfg32.SEHandlerTableOffset,                           targetOffset: 83648), //aadauthhelper
                v => v.Verify(propertyName: "GuardCFCheckFunctionPointer",              index: 1, fieldOffset: cfg64.GuardCFCheckFunctionPointerOffset,              targetOffset: 1651168), //ntdll
                v => v.Verify(propertyName: "GuardCFDispatchFunctionPointer",           index: 2, fieldOffset: cfg64.GuardCFDispatchFunctionPointerOffset,           targetOffset: 1667072), //ntdll
                v => v.Verify(propertyName: "GuardCFFunctionTable",                     index: 3, fieldOffset: cfg64.GuardCFFunctionTableOffset,                     targetOffset: 1261292), //ntdll
                v => v.Verify(propertyName: "GuardAddressTakenIatEntryTable",           index: 4, fieldOffset: cfg64.GuardAddressTakenIatEntryTableOffset,           targetOffset: 6629368), //SingleFileApp_EXE
                v => v.Verify(propertyName: "GuardLongJumpTargetTable",                 index: 4, fieldOffset: cfg64.GuardLongJumpTargetTableOffset,                 targetOffset: 58236), //ntoskrnl
                v => v.Verify(propertyName: "GuardRFFailureRoutineFunctionPointer",     index: 4, fieldOffset: cfg64.GuardRFFailureRoutineFunctionPointerOffset,     targetOffset: 4155656), //DbgEng
                v => v.Verify(propertyName: "DynamicValueRelocTableOffset",             index: 4, fieldOffset: cfg64.DynamicValueRelocTableOffsetOffset,             targetOffset: 2155868), //ntdll
                v => v.Verify(propertyName: "GuardRFVerifyStackPointerFunctionPointer", index: 5, fieldOffset: cfg64.GuardRFVerifyStackPointerFunctionPointerOffset, targetOffset: 4155664), //DbgEng
                v => v.Verify(propertyName: "EnclaveConfigurationPointer",              index: 5, fieldOffset: cfg64.EnclaveConfigurationPointerOffset,              targetOffset: 206864), //AzureAttest
                v => v.Verify(propertyName: "GuardEHContinuationTable",                 index: 5, fieldOffset: cfg64.GuardEHContinuationTableOffset,                 targetOffset: 1260432), //ntdll
                v => v.Verify(propertyName: "GuardXFGCheckFunctionPointer",             index: 6, fieldOffset: cfg64.GuardXFGCheckFunctionPointerOffset,             targetOffset: 1667080), //ntdll
                v => v.Verify(propertyName: "GuardXFGDispatchFunctionPointer",          index: 7, fieldOffset: cfg64.GuardXFGDispatchFunctionPointerOffset,          targetOffset: 1667088), //ntdll
                v => v.Verify(propertyName: "GuardXFGTableDispatchFunctionPointer",     index: 8, fieldOffset: cfg64.GuardXFGTableDispatchFunctionPointerOffset,     targetOffset: 1667096), //ntdll
                v => v.Verify(propertyName: "GuardMemcpyFunctionPointer",               index: 9, fieldOffset: cfg64.GuardMemcpyFunctionPointerOffset,               targetOffset: 3917864) //coreclr
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

            //There's 4 xrefs: the ImageEnclaveConfig.ImportList, and all of the ImageEnclaveImport.ImportName items
            TestXRefs<ImageEnclaveConfig>(
                v => v.Verify(propertyName: "ImportList", index: 0, fieldOffset: ImageEnclaveConfig.ImportListOffset, targetOffset: 222852)
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

            TestXRefs<ImageEnclaveImport>(
                v => v.Verify(propertyName: "ImportName", index: 0, fieldOffset: ImageEnclaveImport.ImportNameOffset, targetOffset: 237918)
            );
        }

        [TestMethod]
        public void GuardAddressTakenIatEntryTable_Test()
        {
            TestStruct<GuardAddressTakenIatEntryTable>(
                v => v.Count == 3
            );

            TestView<GuardAddressTakenIatEntryTable>(
                v => v.VerifyStructIgnoreChildren(name: "GuardAddressTakenIatEntryTable", offset: 6629368, size: 15)
            );
        }

        [TestMethod]
        public void GuardAddressTakenIatEntryTable_Entry_Test()
        {
            TestStruct<GuardAddressTakenIatEntryTable.Entry>(
                v => v.Function == 6607680,
                v => v.Flags == 0
            );

            TestView<GuardAddressTakenIatEntryTable.Entry>(
                v => v.VerifyStruct(
                    name: "Entry", offset: 6629368, size: 5,
                    c => c.VerifyField(name: "Function", value: 6607680),
                    c => c.VerifyField(name: "Flags", value: (IMAGE_GUARD_FLAG) 0)
                )
            );
        }

        [TestMethod]
        public void GuardCFFunctionTable_Test()
        {
            TestStruct<GuardCFFunctionTable>(
                v => v.Count == 2217
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
                v => v.Count == 172
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

        [TestMethod]
        public void GuardLongJumpTargetTable_Test()
        {
            TestStruct<GuardLongJumpTargetTable>(
                v => v.Count == 1
            );

            TestView<GuardLongJumpTargetTable>(
                v => v.VerifyStructIgnoreChildren(name: "GuardLongJumpTargetTable", offset: 58236, size: 5)
            );
        }

        [TestMethod]
        public void GuardLongJumpTargetTable_Entry_Test()
        {
            TestStruct<GuardLongJumpTargetTable.Entry>(
                v => v.Target == 3897986,
                v => v.Flags == 0
            );

            TestView<GuardLongJumpTargetTable.Entry>(
                v => v.VerifyStruct(
                    name: "Entry", offset: 58236, size: 5,
                    c => c.VerifyField(name: "Target", value: 3897986),
                    c => c.VerifyField(name: "Flags", value: (IMAGE_GUARD_FLAG) 0)
                )
            );
        }

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
                    name: "IMAGE_DYNAMIC_RELOCATION", offset: 2155876, size: 192,
                    c => c.VerifyField(name: "Symbol", value: ImageDynamicRelocationKind.FUNCTION_OVERRIDE),
                    c => c.VerifyField(name: "BaseRelocSize", value: 180),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_FUNCTION_OVERRIDE_HEADER", offset: 2155888, size: 180)
                )
            );
        }

        [TestMethod]
        public void ImageDynamicRelocation_ntoskrnl()
        {
            //ntoskrnl has special PXE/PPE/PDE/PTE symbols that we want to confirm we can parse.

            using var peFile = PEFile.FromKey(WellKnownTestModule.ntoskrnl);

            var dynamicRelocations = peFile.LoadConfigTable.DynamicValueRelocTableOffset.Value.DynamicRelocations;

            dynamicRelocations.Verify(
               "GUARD_IMPORT_CONTROL_TRANSFER",
                "GUARD_INDIR_CONTROL_TRANSFER",
                "GUARD_SWITCHTABLE_BRANCH",
                "FFFFDE0000000000",
                "PTE_BASE",
                "PDE_BASE",
                "PPE_BASE",
                "PXE_BASE",
                "PXE_SELFMAP",
                "PXE_TOP",
                "PDE_TOP",
                "PTE_TOP"
            );
        }

        [TestMethod]
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
        }

        //Can't find any files on Windows 11 that use ImageDynamicRelocationV2

        #region Symbol 1

        //Can't find any files on Windows 11 that use ImagePrologueDynamicRelocationHeader

        #endregion
        #region Symbol 2

        //Can't find any files on Windows 11 that use ImageEpilogueDynamicRelocationHeader

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
                v => v.FuncOverrides == IgnoreValue
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

            //Both the ImageBoundImportDescriptor and ImageBoundForwarderRef xrefs are written
            TestXRefs<ImageBoundImportDescriptor>(
                v => v.Verify(propertyName: "Name", index: 0, fieldOffset: ImageBoundImportDescriptor.OffsetModuleNameOffset, targetOffset: 672)
            );
        }

        [TestMethod]
        public void ImageBoundForwarderRef_Test()
        {
            TestStruct<ImageBoundForwarderRef>(
                v => v.TimeDateStamp.ToString() == "18/08/2001 3:33:02 PM",
                v => v.OffsetModuleName == 69,
                v => v.Reserved == 0,
                v => v.Name.ListedOffset == 685
            );

            TestView<ImageBoundForwarderRef>(
                v => v.VerifyStruct(
                    name: "IMAGE_BOUND_FORWARDER_REF", offset: 624, size: 8,
                    c => c.VerifyField(name: "TimeDateStamp", value: "18/08/2001 3:33:02 PM"),
                    c => c.VerifyField(name: "OffsetModuleName", value: (ushort) 69),
                    c => c.VerifyField(name: "Reserved", value: (ushort) 0)
                ),

                v => v.VerifyValue(offset: 0x2AD, value: "msvcrt.DLL")
            );

            TestXRefs<ImageBoundForwarderRef>(
                v => v.Verify(propertyName: "Name", index: 0, fieldOffset: ImageBoundForwarderRef.OffsetModuleNameOffset, targetOffset: 685)
            );
        }

        #endregion
        #region Import Address Table (12)

        [TestMethod]
        public void ImportAddressTable_ImageThunkData_Test()
        {
            using var peFile = PEFile.FromKey(WellKnownTestModule.coreclr);

            var importAddressTable = peFile.ImportAddressTable;

            Assert.AreEqual(383, importAddressTable.Length);
        }

        #endregion
        #region Delay Import Table (13)

        [TestMethod]
        public void ImageDelayLoadDescriptor_Test()
        {
            TestStruct<ImageDelayLoadDescriptor>(
                v => v.Attributes == 1,
                v => v.DllNameRVA.ListedOffset == 3988976,
                v => v.ModuleHandleRVA.ListedOffset == 0,
                v => v.ImportAddressTableRVA.ListedOffset == 5021696,
                v => v.ImportNameTableRVA.ListedOffset == 4723208,
                v => v.BoundImportAddressTableRVA == 4723368,
                v => v.UnloadInformationTable.ListedOffset == 0,
                v => v.TimeDateStamp == 0
            );

            TestView<ImageDelayLoadDescriptor>(
                WithIgnores(
                    before: 1,
                    v => v.VerifyStruct(
                        name: "IMAGE_DELAYLOAD_DESCRIPTOR", offset: 4713892, size: 32,
                        c => c.VerifyField(name: "Attributes", value: 1),
                        c => c.VerifyField(name: "DllNameRVA", value: 3988976),
                        c => c.VerifyField(name: "ModuleHandleRVA", value: 0),
                        c => c.VerifyField(name: "ImportAddressTableRVA", value: 5021696),
                        c => c.VerifyField(name: "ImportNameTableRVA", value: 4723208),
                        c => c.VerifyField(name: "BoundImportAddressTableRVA", value: 4723368),
                        c => c.VerifyField(name: "UnloadInformationTable", value: 0),
                        c => c.VerifyField(name: "TimeDateStamp", value: (Timestamp) 0)
                    ),
                    after: 5
                )
            );

            TestXRefs<ImageDelayLoadDescriptor>(
                v => v.Verify(propertyName: "DllNameRVA",             index: 0, fieldOffset: ImageDelayLoadDescriptor.DllNameRVAOffset, targetOffset: 60976),
                v => v.Verify(propertyName: "ModuleHandleRVA",        index: 1, fieldOffset: ImageDelayLoadDescriptor.ModuleHandleRVAOffset, targetOffset: 79536),
                v => v.Verify(propertyName: "ImportAddressTableRVA",  index: 2, fieldOffset: ImageDelayLoadDescriptor.ImportAddressTableRVAOffset, targetOffset: 90200),
                v => v.Verify(propertyName: "ImportNameTableRVA",     index: 3, fieldOffset: ImageDelayLoadDescriptor.ImportNameTableRVAOffset, targetOffset: 69752)

                //I don't think this is present in on-disk modules
                //v => v.Verify(propertyName: "UnloadInformationTable", index: -1, fieldOffset: ImageDelayLoadDescriptor.UnloadInformationTableOffset, targetOffset: 0)
            );
        }

        [TestMethod]
        public void ImageDelayLoadDescriptor_ImageThunkData_Test()
        {
            using var peFile = PEFile.FromKey(WellKnownTestModule.coreclr);

            var delayLoad = peFile.DelayImportTable[0];

            Assert.AreEqual(4, delayLoad.ImportNameTableRVA.Value.Length);
            Assert.AreEqual("VerQueryValueW", delayLoad.ImportNameTableRVA.Value[0].ToString());
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
                    c => c.VerifyField(name: "pVersion", value: "v4.0.30319"),
                    c => c.VerifyByteBlob(offset: 1604610, value: new byte[] {0, 0})
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

        [TestMethod]
        public void ImageCorVTableFixup_Test()
        {
            TestStruct<ImageCorVTableFixup>(
                v => v.RVA == 90176,
                v => v.Count == 1,
                v => v.Type == (COR_VTABLE.COR_VTABLE_64BIT | COR_VTABLE.COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN)
            );

            TestView<ImageCorVTableFixup>(
                v => v.VerifyStruct(
                    name: "IMAGE_COR_VTABLEFIXUP", offset: 77328, size: 8,
                    c => c.VerifyField(name: "RVA", value: 90176),
                    c => c.VerifyField(name: "Count", value: (short) 1),
                    c => c.VerifyField(name: "Type", value: (COR_VTABLE.COR_VTABLE_64BIT | COR_VTABLE.COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN))
                )
            );
        }

        //The EcmaMetadata type just provides access to the StorageSignature/StorageHeader and provides easy to use properties
        //for accessing the various heaps. It doesn't otherwise do anything

        [TestMethod]
        public void Ecma335_BlobEntry_Test()
        {
            TestStruct<BlobEntry>(
                v => v.CompressedSize == new byte[]{4},
                v => v.Value == new byte[] {32,1,1,8}
            );

            TestView<BlobEntry>(
                v => v.VerifyStruct(
                    name: "BlobEntry", offset: 1757, size: 5,
                    c => c.VerifyField(name: "Size", value: new byte[]{4}),
                    c => c.VerifyField(name: "Value", value: new byte[]{32,1,1,8})
                )
            );
        }

        [TestMethod]
        public void Ecma335_CompressedModelHeader_Test()
        {
            TestStruct<CompressedModelHeader>(
                v => v.Reserved1 == 0,
                v => v.MajorVersion == 2,
                v => v.MinorVersion == 0,
                v => v.HeapSizes == 0,
                v => v.Reserved2 == 1,
                v => v.Valid == (TableMask.Module | TableMask.TypeRef | TableMask.TypeDef | TableMask.MethodDef | TableMask.Param | TableMask.MemberRef | TableMask.CustomAttribute | TableMask.Assembly | TableMask.AssemblyRef),
                v => v.Sorted == (TableMask.InterfaceImpl | TableMask.Constant | TableMask.CustomAttribute | TableMask.FieldMarshal | TableMask.DeclSecurity | TableMask.ClassLayout | TableMask.FieldLayout | TableMask.MethodSemantics | TableMask.MethodImpl | TableMask.ImplMap | TableMask.FieldRva | TableMask.NestedClass | TableMask.GenericParam | TableMask.GenericParamConstraint),
                v => v.RowCounts == new int[] { 1, 16, 2, 2, 1, 15, 14, 1, 1 }
            );

            TestView<CompressedModelHeader>(
                v => v.VerifyStruct(
                    name: "Metadata Header", offset: 712, size: 60,
                    c => c.VerifyField(name: "Reserved1", value: 0),
                    c => c.VerifyField(name: "MajorVersion", value: (byte) 2),
                    c => c.VerifyField(name: "MinorVersion", value: (byte) 0),
                    c => c.VerifyField(name: "HeapSizes", value: (HeapSizes) 0),
                    c => c.VerifyField(name: "Reserved2", value: (byte) 1),
                    c => c.VerifyField(name: "Valid", value: (TableMask.Module | TableMask.TypeRef | TableMask.TypeDef | TableMask.MethodDef | TableMask.Param | TableMask.MemberRef | TableMask.CustomAttribute | TableMask.Assembly | TableMask.AssemblyRef)),
                    c => c.VerifyField(name: "Sorted", value: (TableMask.InterfaceImpl | TableMask.Constant | TableMask.CustomAttribute | TableMask.FieldMarshal | TableMask.DeclSecurity | TableMask.ClassLayout | TableMask.FieldLayout | TableMask.MethodSemantics | TableMask.MethodImpl | TableMask.ImplMap | TableMask.FieldRva | TableMask.NestedClass | TableMask.GenericParam | TableMask.GenericParamConstraint)),
                    c => c.VerifyField(name: "RowCounts", value: new int[] { 1, 16, 2, 2, 1, 15, 14, 1, 1 })
                )
            );
        }

        //CompressedModelHeap doesn't really "store" anything; it's just a type that holds the unpacked metadata

        [TestMethod]
        public void Ecma335_UserString_Test()
        {
            TestStruct<UserString>(
                v => v.Value == "{{ message = {0} }}",
                v => v.UnicodeByte == 0
            );

            TestView<UserString>(
                v => v.VerifyStruct(
                    name: "UserString", offset: 3610313, size: 40,
                    c => c.VerifyField(name: "Size", value: new byte[] {39}),
                    c => c.VerifyField(name: "Value", value: "{{ message = {0} }}"),
                    c => c.VerifyField(name: "UnicodeByte", value: (byte) 0)
                )
            );
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
                    name: "IMAGE_COR_ILMETHOD_TINY", offset: 592, size: 8,
                    c => c.VerifyBitField(name: "Flags", value: (CorILMethodFlags.TinyFormat1 | CorILMethodFlags.MoreSects | CorILMethodFlags.InitLocals), bits: 2),
                    c => c.VerifyBitField(name: "CodeSize", value: 7, bits: 6),
                    c => c.VerifyField(name: "ILBytes", value: new byte[] { 2, 123, 63, 0, 0, 10, 42 })
                )
            );
        }

        [TestMethod]
        public void ImageCorILMethodSect_Test()
        {
            TestStruct<ImageCorILMethodSect>(
                v => v.Kind == CorILMethodSect.EHTable,
                v => v.DataSize == 16
            );

            TestView<ImageCorILMethodSect>(
                v => v.VerifyStruct(
                    name: "IMAGE_COR_ILMETHOD_SECT_SMALL", offset: 2552, size: 2,
                    c => c.VerifyField(name: "Kind", value: CorILMethodSect.EHTable),
                    c => c.VerifyField(name: "DataSize", value: (byte) 16)
                )
            );
        }

        [TestMethod]
        public void ImageCorILMethodSectEH_Test()
        {
            TestStruct<ImageCorILMethodSectEH>(
                v => v.Reserved == 0,
                v => v.Clauses == IgnoreValue
            );

            TestView<ImageCorILMethodSectEH>(
                v => v.VerifyStruct(
                    name: "IMAGE_COR_ILMETHOD_SECT_EH_SMALL", offset: 2552, size: 16,
                    c => c.VerifyStructField(name: "SectSmall", type: "IMAGE_COR_ILMETHOD_SECT_SMALL", offset: 2552, size: 2,
                        c1 => c1.VerifyField(name: "Kind", value: CorILMethodSect.EHTable),
                        c1 => c1.VerifyField(name: "DataSize", value: (byte) 16)
                    ),
                    c => c.VerifyField(name: "Reserved", value: (short) 0),
                    c => c.VerifyStructFieldArray(name: "Clauses",
                        c1 => c1.VerifyStructIgnoreChildren(name: "IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL", offset: 2556, size: 12)
                    )
                )
            );
        }

        [TestMethod]
        public void ImageCorILMethodSectEHClause_Test()
        {
            TestStruct<ImageCorILMethodSectEHClause>(
                v => v.Flags == CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FINALLY,
                v => v.TryOffset == 23,
                v => v.TryLength == 11,
                v => v.HandlerOffset == 34,
                v => v.HandlerLength == 7,
                v => v.ClassToken == 0x0,
                v => v.FilterOffset == 0
            );

            TestView<ImageCorILMethodSectEHClause>(
                v => v.VerifyStruct(
                    name: "IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL", offset: 2556, size: 12,
                    c => c.VerifyField(name: "Flags", value: CorExceptionFlag.COR_ILEXCEPTION_CLAUSE_FINALLY),
                    c => c.VerifyField(name: "TryOffset", value: (short) 23),
                    c => c.VerifyField(name: "TryLength", value: (byte) 11),
                    c => c.VerifyField(name: "HandlerOffset", value: (short) 34),
                    c => c.VerifyField(name: "HandlerLength", value: (byte) 7),
                    c => c.VerifyField(name: "ClassToken", value: 0)
                )
            );
        }

        #endregion
        #region R2R

        [TestMethod]
        public void ReadyToRunCoreHeader_Test()
        {
            TestStruct<ReadyToRunCoreHeader>(
                v => v.Flags == (ReadyToRunFlag.READYTORUN_FLAG_SKIP_TYPE_VALIDATION | ReadyToRunFlag.READYTORUN_FLAG_NONSHARED_PINVOKE_STUBS | ReadyToRunFlag.READYTORUN_FLAG_MULTIMODULE_VERSION_BUBBLE),
                v => v.NumberOfSections == 11,
                v => v.Sections == IgnoreValue
            );

            TestView<ReadyToRunCoreHeader>(
                v => v.VerifyStruct(
                    name: "READYTORUN_CORE_HEADER", offset: 5512, size: 140,
                    c => c.VerifyField(name: "Flags", value: (ReadyToRunFlag.READYTORUN_FLAG_SKIP_TYPE_VALIDATION | ReadyToRunFlag.READYTORUN_FLAG_NONSHARED_PINVOKE_STUBS | ReadyToRunFlag.READYTORUN_FLAG_MULTIMODULE_VERSION_BUBBLE)),
                    c => c.VerifyField(name: "NumberOfSections", value: 11),
                    c => c.VerifyFieldIgnoreValue("Sections")
                )
            );
        }

        [TestMethod]
        public void ReadyToRunHeader_Test()
        {
            TestStruct<ReadyToRunHeader>(
                v => v.Signature == 5395538,
                v => v.MajorVersion == 10,
                v => v.MinorVersion == 1
            );

            TestView<ReadyToRunHeader>(
                v => v.VerifyStruct(
                    name: "READYTORUN_HEADER", offset: 5504, size: 148,
                    c => c.VerifyField(name: "Signature", value: 5395538),
                    c => c.VerifyField(name: "MajorVersion", value: (short) 10),
                    c => c.VerifyField(name: "MinorVersion", value: (short) 1),
                    c => c.VerifyFieldIgnoreValue(name: "CoreHeader")
                )
            );
        }

        [TestMethod]
        public void ReadyToRunImportSection_Test()
        {
            TestStruct<ReadyToRunImportSection>(
                v => v.Flags == ReadyToRunImportSectionFlags.PCode,
                v => v.Type == ReadyToRunImportSectionType.StubDispatch,
                v => v.EntrySize == 8,
                v => v.Signatures == 0,
                v => v.AuxiliaryData == 6088
            );

            TestView<ReadyToRunImportSection>(
                v => v.VerifyStruct(
                    name: "READYTORUN_IMPORT_SECTION", offset: 8212, size: 20,
                    c => c.VerifyStructField(name: "Section", type: "IMAGE_DATA_DIRECTORY", offset: 8212, size: 8,
                        c1 => c1.VerifyField(name: "VirtualAddress", value: 8336),
                        c1 => c1.VerifyField(name: "Size", value: 0)
                    ),
                    c => c.VerifyField(name: "Flags", value: ReadyToRunImportSectionFlags.PCode),
                    c => c.VerifyField(name: "Type", value: ReadyToRunImportSectionType.StubDispatch),
                    c => c.VerifyField(name: "EntrySize", value: (byte) 8),
                    c => c.VerifyField(name: "Signatures", value: 0),
                    c => c.VerifyField(name: "AuxiliaryData", value: 6088)
                )
            );
        }

        [TestMethod]
        public void ReadyToRunSection_Test()
        {
            TestStruct<ReadyToRunSection>(
                v => v.Type == ReadyToRunSectionType.CompilerIdentifier,
                v => v.Data.ToString() == "Crossgen2 9.0.425.16305"
            );

            TestView<ReadyToRunSection>(
                v => v.VerifyStruct(
                    name: "READYTORUN_SECTION", offset: 5520, size: 12,
                    c => c.VerifyField(name: "Type", value: ReadyToRunSectionType.CompilerIdentifier),
                    c => c.VerifyFieldIgnoreValue(name: "Section")
                ),
                verify => verify.VerifyValue(offset: 0x1750, value: "Crossgen2 9.0.425.16305")
            );
        }

        #endregion
        #region CLR

        [TestMethod]
        public void AppHostSignature_Test()
        {
            TestStruct<AppHostSignature>(
                v => v.BundleHeaderOffset.ListedAddress == 12699859,
                v => v.BundleSignature == IgnoreValue
            );

            TestView<AppHostSignature>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "AppHost Signature", offset: 8230120, size: 40,
                        c => c.VerifyField(name: "BundleHeaderOffset", value: (long) 12699859),
                        c => c.VerifyFieldIgnoreValue(name: "BundleSignature")
                    ),
                    after: 15623
                )
            );

            //All of the children of the bundle also write their xrefs
            TestXRefs<AppHostSignature>(
                v => v.Verify(propertyName: "BundleHeaderOffset", index: 0, fieldOffset: AppHostSignature.BundleHeaderOffsetOffset, targetOffset: 12699859)
            );
        }

        //The Bundle.Manifest type just encapsulates the members within it

        [TestMethod]
        public void Bundle_HeaderFixed_Test()
        {
            TestStruct<Bundle.HeaderFixed>(
                v => v.MajorVersion == 6,
                v => v.MinorVersion == 0,
                v => v.NumEmbeddedFiles == 12
            );

            TestView<Bundle.HeaderFixed>(
                v => v.VerifyStruct(
                    name: "header_fixed_t", offset: 12699859, size: 12,
                    c => c.VerifyField(name: "major_version", value: 6),
                    c => c.VerifyField(name: "minor_version", value: 0),
                    c => c.VerifyField(name: "num_embedded_files", value: 12)
                )
            );
        }

        [TestMethod]
        public void Bundle_HeaderFixedV2_Test()
        {
            TestStruct<Bundle.HeaderFixedV2>(
                v => v.Flags == Bundle.header_flags_t.none
            );

            TestView<Bundle.HeaderFixedV2>(
                WithIgnores(
                    before: 2,
                    v => v.VerifyStruct(
                        name: "header_fixed_v2_t", offset: 12699904, size: 40,
                        c => c.VerifyStructField(name: "deps_json_location", type: "location_t", offset: 12699904, size: 16,
                            c1 => c1.VerifyField("offset", "12697600"),
                            c1 => c1.VerifyField("size", "2259")
                        ),
                        c => c.VerifyStructField(name: "runtimeconfig_json_location", type: "location_t", offset: 12699920, size: 16,
                            c1 => c1.VerifyField("offset", "9860096"),
                            c1 => c1.VerifyField("size", "1641")
                        ),
                        c => c.VerifyField(name: "flags", value: Bundle.header_flags_t.none)
                    )
                )
            );
        }

        [TestMethod]
        public void Bundle_FileEntry_Test()
        {
            TestStruct<Bundle.FileEntry>(
                v => v.RelativePath.ToString() == "TestApp.runtimeconfig.json"
            );

            TestView<Bundle.FileEntry>(
                WithIgnores(
                    before: 1,
                    v => v.VerifyStruct(
                        name: "file_entry_t", offset: 12699944, size: 52,
                        c => c.VerifyStructIgnoreChildren(name: "file_entry_fixed_t", offset: 12699944, size: 25),
                        c => c.VerifyStructIgnoreChildren(name: "BundleEncodedString", offset: 12699969, size: 27)
                    )
                )
            );
        }

        [TestMethod]
        public void Bundle_FileEntryFixed_Test()
        {
            TestStruct<Bundle.FileEntryFixed>(
                v => v.Offset == 9860096,
                v => v.Size == 1641,
                v => v.CompressedSize == 0,
                v => v.Type == Bundle.file_type_t.runtime_config_json
            );

            TestView<Bundle.FileEntryFixed>(
                v => v.VerifyStruct(
                    name: "file_entry_fixed_t", offset: 12699944, size: 25,
                    c => c.VerifyField(name: "offset", value: (long) 9860096),
                    c => c.VerifyField(name: "size", value: (long) 1641),
                    c => c.VerifyField(name: "compressedSize", value: (long) 0),
                    c => c.VerifyField(name: "type", value: Bundle.file_type_t.runtime_config_json)
                )
            );
        }

        [TestMethod]
        public void Bundle_Location_Test()
        {
            TestStruct<Bundle.Location>(
                v => v.Size == 2259
            );

            TestView<Bundle.Location>(
                v => v.VerifyStruct(
                    name: "location_t", offset: 12699904, size: 16,
                    c => c.VerifyField(name: "offset", value: (long) 12697600),
                    c => c.VerifyField(name: "size", value: (long) 2259)
                )
            );
        }

        [TestMethod]
        public void BundleEncodedString_Test()
        {
            TestStruct<BundleEncodedString>(
                v => v.Length == 32,
                v => v.Value == "V_i__Fifk2ERvzJZLk2Y1iFP03KlUWk="
            );

            TestView<BundleEncodedString>(
                v => v.VerifyStruct(
                    name: "BundleEncodedString", offset: 12699871, size: 33,
                    c => c.VerifyField(name: "Length", value: 32),
                    c => c.VerifyField(name: "Value", value: "V_i__Fifk2ERvzJZLk2Y1iFP03KlUWk=")
                )
            );
        }

        [TestMethod]
        public void ClrEngineMetrics_Test()
        {
            TestStruct<ClrEngineMetrics>(
                v => v.Size == 16,
                v => v.DbiVersion == 4,
                v => v.ContinueStartupEvent == 5376933008
            );

            TestView<ClrEngineMetrics>(
                v => v.VerifyStruct(
                    name: "CLR_ENGINE_METRICS", offset: 8213632, size: 16,
                    c => c.VerifyField(name: "Size", value: 16),
                    c => c.VerifyField(name: "DbiVersion", value: 4),
                    c => c.VerifyField(name: "ContinueStartupEvent", value: (ulong) 5376933008)
                )
            );
        }

        [TestMethod]
        public void DotNetRuntimeDebugHeader_Test()
        {
            TestStruct<DotNetRuntimeDebugHeader>(
                v => v.Cookie == 0x48444E44,
                v => v.MajorVersion == 4,
                v => v.MinorVersion == 0,
                v => v.Flags == 1,
                v => v.ReservedPadding1 == 0
            );

            TestView<DotNetRuntimeDebugHeader>(
                WithIgnores(
                    before: 190,
                    v => v.VerifyStruct(
                        name: "DotNetRuntimeDebugHeader", offset: 1444064, size: 32,
                        c => c.VerifyField(name: "Cookie", value: 1212436036),
                        c => c.VerifyField(name: "MajorVersion", value: (short) 4),
                        c => c.VerifyField(name: "MinorVersion", value: (short) 0),
                        c => c.VerifyField(name: "Flags", value: 1),
                        c => c.VerifyField(name: "ReservedPadding1", value: 0),
                        c => c.VerifyFieldIgnoreValue(name: "DebugTypeEntries"),
                        c => c.VerifyFieldIgnoreValue(name: "GlobalValueEntries")
                    ),
                    after: 2
                )
            );

            //All of the child entities have their own xrefs; these two are the first and 186th records
            TestXRefs<DotNetRuntimeDebugHeader>(
                v => v.Verify(propertyName: "DebugTypeEntries", index: 0, fieldOffset: 16, targetOffset: 1466672),
                v => v.Verify(propertyName: "GlobalValueEntries", index: 185, fieldOffset: 24, targetOffset: 1469072)
            );
        }

        [TestMethod]
        public void DebugTypeEntry_Test()
        {
            TestStruct<DebugTypeEntry>(
                v => v.TypeName.Value == "GcDacVars",
                v => v.FieldName.Value == "SIZEOF",
                v => v.FieldOffset == 320,
                v => v.ReservedPadding == 0
            );

            TestView<DebugTypeEntry>(
                v => v.VerifyValue(0x13ECB0, "GcDacVars"),
                v => v.VerifyValue(0x13ECBC, "SIZEOF"),
                v => v.VerifyStruct(
                    name: "DebugTypeEntry", offset: 1466672, size: 24,
                    c => c.VerifyFieldIgnoreValue(name: "TypeName"),
                    c => c.VerifyFieldIgnoreValue(name: "FieldName"),
                    c => c.VerifyField(name: "FieldOffset", value: 320),
                    c => c.VerifyField(name: "ReservedPadding", value: 0)
                )
            );

            TestXRefs<DebugTypeEntry>(
                v => v.Verify(propertyName: "TypeName",  index: 0, fieldOffset: 0, targetOffset: 1305776),
                v => v.Verify(propertyName: "FieldName", index: 1, fieldOffset: 8, targetOffset: 1305788)
            );
        }

        [TestMethod]
        public void GlobalValueEntry_Test()
        {
            TestStruct<GlobalValueEntry>(
                v => v.Name.Value == "g_CrashInfoBuffer"
            );

            TestView<GlobalValueEntry>(
                v => v.VerifyValue(0x13F210, "g_CrashInfoBuffer"),
                v => v.VerifyStruct(
                    name: "GlobalValueEntry", offset: 1469072, size: 16,
                    c => c.VerifyFieldIgnoreValue(name: "Name"),
                    c => c.VerifyFieldIgnoreValue(name: "Address")
                )
            );

            TestXRefs<GlobalValueEntry>(
                v => v.Verify(propertyName: "Name", index: 0, fieldOffset: GlobalValueEntry.NameOffset, targetOffset: 1307152)
            );
        }

        [TestMethod]
        public void RuntimeInfo_Test()
        {
            TestStruct<RuntimeInfo>(
                v => v.Signature == "DotNetRuntimeInfo",
                v => v.Version == 2,
                v => v.RuntimeModuleIndex.Size == 8,
                v => v.RuntimeModuleIndex.TimeStamp == 1747419391,
                v => v.RuntimeModuleIndex.ImageSize == 4902912,

                v => v.DacModuleIndex.Size == 8,
                v => v.DacModuleIndex.TimeStamp == 1747419262,
                v => v.DacModuleIndex.ImageSize == 1368064,

                v => v.DbiModuleIndex.Size == 8,
                v => v.DbiModuleIndex.TimeStamp == 1747419253,
                v => v.DbiModuleIndex.ImageSize == 1249280,

                v => v.RuntimeVersion.ToString() == "9.0.625.26613"
            );

            TestView<RuntimeInfo>(
                v => v.VerifyStruct(
                    name: "RuntimeInfo", offset: 8215696, size: 112,
                    c => c.VerifyField(name: "Signature", value: "DotNetRuntimeInfo"),
                    c => c.VerifyByteBlob(offset: 0x7D5CA2, new byte[] {0, 0}),
                    c => c.VerifyField(name: "Version", value: 2),

                    c => c.VerifyStructField(name: "RuntimeModuleIndex", type: "Module Index", offset: 8215720, size: 24,
                        c1 => c1.VerifyField(name: "Size", value: (byte) 8),
                        c1 => c1.VerifyField(name: "TimeStamp", value: (uint) 1747419391),
                        c1 => c1.VerifyField(name: "ImageSize", value: 4902912),
                        c1 => c1.VerifyField(name: "Extra", value: new byte[] {0,0,0,0,0, 0,0,0,0,0, 0,0,0,0,0})
                    ),

                    c => c.VerifyStructField(name: "DacModuleIndex", type: "Module Index", offset: 8215744, size: 24,
                        c1 => c1.VerifyField(name: "Size", value: (byte) 8),
                        c1 => c1.VerifyField(name: "TimeStamp", value: (uint) 1747419262),
                        c1 => c1.VerifyField(name: "ImageSize", value: 1368064),
                        c1 => c1.VerifyField(name: "Extra", value: new byte[] {0,0,0,0,0, 0,0,0,0,0, 0,0,0,0,0})
                    ),

                    c => c.VerifyStructField(name: "DbiModuleIndex", type: "Module Index", offset: 8215768, size: 24,
                        c1 => c1.VerifyField(name: "Size", value: (byte) 8),
                        c1 => c1.VerifyField(name: "TimeStamp", value: (uint) 1747419253),
                        c1 => c1.VerifyField(name: "ImageSize", value: 1249280),
                        c1 => c1.VerifyField(name: "Extra", value: new byte[] {0,0,0,0,0, 0,0,0,0,0, 0,0,0,0,0})
                    ),

                    c => c.VerifyField(name: "RuntimeVersion", value: new[]{9, 0, 625, 26613})
                )
            );
        }

        #endregion
        #region Symbols

        [TestMethod]
        public void CoffSymbolTable_Test()
        {
            TestStruct<CoffSymbolTable>(
                v => v.Symbols == IgnoreValue,
                v => v.StringTableSize == 12264,
                v => v.Strings == IgnoreValue
            );

            TestView<CoffSymbolTable>(
                v => v.VerifyStruct(
                    name: "Coff Symbol Table", offset: 61472, size: 32478,
                    WithIgnores(
                        before: 942,
                        c => c.VerifyField(name: "String Table Size", value: 12264),
                        after: 508
                    )
                )
            );
        }

        [TestMethod]
        public void ImageSymbol_Test()
        {
            TestStruct<ImageSymbol>(
                v => v.Name.ToString() == "@comp.id",
                v => v.Value == 0,
                v => v.SectionNumber == 65535,
                v => v.Type == ImageSymType.Null,
                v => v.BasicType == ImageSymType.Null,
                v => v.DerivedType == ImageSymDType.Null,
                v => v.StorageClass == ImageSymClass.Static,
                v => v.NumberOfAuxSymbols == 0,
                v => v.AuxSymbols == IgnoreValue
            );

            TestView<ImageSymbol>(
                v => v.VerifyStruct(
                    name: "IMAGE_SYMBOL", offset: 61472, size: 18,
                    c => c.VerifyField(name: "Name.Short", value: 0),
                    c => c.VerifyField(name: "Name.Long", value: 0),
                    c => c.VerifyField(name: "Value", value: (uint) 0),
                    c => c.VerifyField(name: "SectionNumber", value: (ushort) 65535),
                    c => c.VerifyField(name: "Type", value: ImageSymType.Null),
                    c => c.VerifyField(name: "StorageClass", value: ImageSymClass.Static),
                    c => c.VerifyField(name: "NumberOfAuxSymbols", value: (byte) 0)
                )
            );
        }


        #endregion
    }
}
