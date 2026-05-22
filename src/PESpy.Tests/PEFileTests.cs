using System;
using System.IO;
using System.Linq;
using ClrDebug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PESpy.Ecma335;
using PESpy.View;
using ClrDebug.OMF;
using ClrDebug.PDB;
using static ClrDebug.COMIMAGE_FLAGS;
using static ClrDebug.IMAGE_FILE_MACHINE;
using static PESpy.IMAGE_FILE;
using static PESpy.IMAGE_DEBUG_TYPE;
using static PESpy.IMAGE_DLLCHARACTERISTICS;
using static PESpy.IMAGE_DLLCHARACTERISTICS_EX;
using static PESpy.IMAGE_DYNAMIC_RELOCATION_KIND;
using static PESpy.IMAGE_SYM_TYPE;
using System.Threading.Tasks;
using System.Diagnostics;

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
                        v => v.VerifyField(name: "ProdId", value: PRODID.prodidUnknown),
                        v => v.VerifyField(name: "BuildId", value: (ushort) 0),
                        v => v.VerifyField(name: "Count", value: 1)
                    ),
                                        c => c.VerifyStruct(name: "PRODITEM", offset: 152, size: 8,
                        v => v.VerifyField(name: "ProdId", value: PRODID.prodidExport1400),
                        v => v.VerifyField(name: "BuildId", value: (ushort) 30795),
                        v => v.VerifyField(name: "Count", value: 1)
                    ),
                    c => c.VerifyStruct(name: "PRODITEM", offset: 160, size: 8,
                        v => v.VerifyField(name: "ProdId", value: PRODID.prodidMasm1400),
                        v => v.VerifyField(name: "BuildId", value: (ushort) 30795),
                        v => v.VerifyField(name: "Count", value: 44)
                    ),
                    c => c.VerifyStruct(name: "PRODITEM", offset: 168, size: 8,
                        v => v.VerifyField(name: "ProdId", value: PRODID.prodidUtc1900_C),
                        v => v.VerifyField(name: "BuildId", value: (ushort) 30795),
                        v => v.VerifyField(name: "Count", value: 131)
                    ),
                    c => c.VerifyStruct(name: "PRODITEM", offset: 176, size: 8,
                        v => v.VerifyField(name: "ProdId", value: PRODID.prodidUtc1900_POGO_O_C),
                        v => v.VerifyField(name: "BuildId", value: (ushort) 30795),
                        v => v.VerifyField(name: "Count", value: 304)
                    ),
                    c => c.VerifyStruct(name: "PRODITEM", offset: 184, size: 8,
                        v => v.VerifyField(name: "ProdId", value: PRODID.prodidUtc1900_CPP),
                        v => v.VerifyField(name: "BuildId", value: (ushort) 30795),
                        v => v.VerifyField(name: "Count", value: 27)
                    ),
                    c => c.VerifyStruct(name: "PRODITEM", offset: 192, size: 8,
                        v => v.VerifyField(name: "ProdId", value: PRODID.prodidCvtres1400),
                        v => v.VerifyField(name: "BuildId", value: (ushort) 30795),
                        v => v.VerifyField(name: "Count", value: 1)
                    ),
                    c => c.VerifyStruct(name: "PRODITEM", offset: 200, size: 8,
                        v => v.VerifyField(name: "ProdId", value: PRODID.prodidLinker1400),
                        v => v.VerifyField(name: "BuildId", value: (ushort) 30795),
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
                //v => v.ProdId == 256,
                v => v.BuildId == 30795,
                v => v.Count == 1
                //v => v.ProductId == "Export1400",
                //v => v.VisualStudioVersion == "Visual Studio 2015 14.00"
            );

            TestView<ProdItem>(
                v => v.VerifyStruct(
                    name: "PRODITEM", offset: 152, size: 8,
                    c => c.VerifyField(name: "ProdId", value: PRODID.prodidExport1400),
                    c => c.VerifyField(name: "BuildId", value: (ushort) 30795),
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
                v => v.Signature == 17744
                //v => v.FileHeader == IgnoreValue,
                //v => v.OptionalHeader == IgnoreValue
            );

            TestView<ImageNtHeaders>(
                v => v.VerifyStruct(
                    name: "IMAGE_NT_HEADERS", offset: 224, size: 264,
                    c => c.VerifyField(name: "Signature", value: 17744),
                    c => c.VerifyStructFieldIgnoreChildren(name: "FileHeader", type: "IMAGE_FILE_HEADER", offset: 228, size: 20),
                    c => c.VerifyStructFieldIgnoreChildren(name: "OptionalHeader", type: "IMAGE_OPTIONAL_HEADER", offset: 248, size: 240)
                )
            );
        }

        [TestMethod]
        public void ImageFileHeader_Test()
        {
            TestStruct<ImageFileHeader>(
                v => v.Machine == IMAGE_FILE_MACHINE_AMD64,
                v => v.NumberOfSections == (ushort) 11,
                v => v.TimeDateStamp == 3169667970,
                v => v.PointerToSymbolTable.ListedAddress == 0,
                v => v.NumberOfSymbols == 0,
                v => v.SizeOfOptionalHeader == 240,
                v => v.Characteristics == (IMAGE_FILE_EXECUTABLE_IMAGE | IMAGE_FILE_LARGE_ADDRESS_AWARE | IMAGE_FILE_DLL)
            );

            TestView<ImageFileHeader>(
                v => v.VerifyStruct(
                    name: "IMAGE_FILE_HEADER", offset: 228, size: 20,
                    c => c.VerifyField(name: "Machine", value: IMAGE_FILE_MACHINE_AMD64),
                    c => c.VerifyField(name: "NumberOfSections", value: (ushort) 11),
                    c => c.VerifyField(name: "TimeDateStamp", value: (uint) 3169667970),
                    c => c.VerifyField(name: "PointerToSymbolTable", value: 0),
                    c => c.VerifyField(name: "NumberOfSymbols", value: 0),
                    c => c.VerifyField(name: "SizeOfOptionalHeader", value: (short) 240),
                    c => c.VerifyField(name: "Characteristics", value: IMAGE_FILE_EXECUTABLE_IMAGE | IMAGE_FILE_LARGE_ADDRESS_AWARE | IMAGE_FILE_DLL)
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
                v => v.Magic == PEMagic.IMAGE_NT_OPTIONAL_HDR64_MAGIC,
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
                v => v.CheckSum == 2216261,
                v => v.Subsystem == IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI,
                v => v.DllCharacteristics == (IMAGE_DLLCHARACTERISTICS_HIGH_ENTROPY_VA | IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE | IMAGE_DLLCHARACTERISTICS_NX_COMPAT | IMAGE_DLLCHARACTERISTICS_GUARD_CF),
                v => v.SizeOfStackReserve == 262144,
                v => v.SizeOfStackCommit == 4096,
                v => v.SizeOfHeapReserve == 1048576,
                v => v.SizeOfHeapCommit == 4096,
                v => v.LoaderFlags == (IMAGE_LOADER_FLAGS) 0,
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
                    c => c.VerifyField(name: "Magic", value: PEMagic.IMAGE_NT_OPTIONAL_HDR64_MAGIC),
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
                    c => c.VerifyField(name: "CheckSum", value: (uint) 2216261),
                    c => c.VerifyField(name: "Subsystem", value: IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_WINDOWS_CUI),
                    c => c.VerifyField(name: "DllCharacteristics", value: (IMAGE_DLLCHARACTERISTICS_HIGH_ENTROPY_VA | IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE | IMAGE_DLLCHARACTERISTICS_NX_COMPAT | IMAGE_DLLCHARACTERISTICS_GUARD_CF)),
                    c => c.VerifyField(name: "SizeOfStackReserve", value: (ulong) 262144),
                    c => c.VerifyField(name: "SizeOfStackCommit", value: (ulong) 4096),
                    c => c.VerifyField(name: "SizeOfHeapReserve", value: (ulong) 1048576),
                    c => c.VerifyField(name: "SizeOfHeapCommit", value: (ulong) 4096),
                    c => c.VerifyField(name: "LoaderFlags", value: (IMAGE_LOADER_FLAGS) 0),
                    c => c.VerifyField(name: "NumberOfRvaAndSizes", value: 16),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_EXPORT (0)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_IMPORT (1)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_RESOURCE (2)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_EXCEPTION (3)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_SECURITY (4)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_BASERELOC (5)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_DEBUG (6)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_COPYRIGHT (7)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_GLOBALPTR (8)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_TLS (9)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG (10)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT (11)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_IAT (12)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT (13)"),
                    c => c.VerifyFieldIgnoreValue(name: "IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR (14)"),
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
                v => (uint) v.TimeDateStamp == 1748474400,
                v => v.MajorVersion == 0,
                v => v.MinorVersion == 0,
                v => v.Name.ListedOffset == 665710,
                v => v.Base == 1,
                v => v.NumberOfFunctions == 1671,
                v => v.NumberOfNames == 1671,
                v => v.AddressOfFunctions.ListedOffset == 649000,
                v => v.AddressOfNames.ListedOffset == 655684,
                v => v.AddressOfNameOrdinals.ListedOffset == 662368,
                v => v.Exports == IgnoreValue
            );

            TestView<ImageExportDirectory>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "IMAGE_EXPORT_DIRECTORY", offset: 648960, size: 40,
                        c => c.VerifyField(name: "Characteristics", value: 0),
                        c => c.VerifyFieldIgnoreValue(name: "TimeDateStamp"), //Attempting to compare datetimes will cause issues in CI
                        c => c.VerifyField(name: "MajorVersion", value: (ushort) 0),
                        c => c.VerifyField(name: "MinorVersion", value: (ushort) 0),
                        c => c.VerifyField(name: "Name", value: 665710),
                        c => c.VerifyField(name: "Base", value: 1),
                        c => c.VerifyField(name: "NumberOfFunctions", value: 1671),
                        c => c.VerifyField(name: "NumberOfNames", value: 1671),
                        c => c.VerifyField(name: "AddressOfFunctions", value: 649000),
                        c => c.VerifyField(name: "AddressOfNames", value: 655684),
                        c => c.VerifyField(name: "AddressOfNameOrdinals", value: 662368)
                    ),
                    after: 1881
                )
            );

            TestXRefs<ImageExportDirectory>(
                v => v.Verify(propertyName: "Name",                  index: 0, fieldOffset: ImageExportDirectory.NameOffset,                  targetOffset: 0xa286e),
                v => v.Verify(propertyName: "AddressOfFunctions",    index: 1, fieldOffset: ImageExportDirectory.AddressOfFunctionsOffset,    targetOffset: 0x9e728),
                v => v.Verify(propertyName: "AddressOfNames",        index: 1673, fieldOffset: ImageExportDirectory.AddressOfNamesOffset,        targetOffset: 0xa0144),
                v => v.Verify(propertyName: "AddressOfNameOrdinals", index: 3345, fieldOffset: ImageExportDirectory.AddressOfNameOrdinalsOffset, targetOffset: 0xa1b60)
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
                v => v.Verify(propertyName: "OffsetToData", index: 0, fieldOffset: ImageResourceDirectoryEntry.DataAndDirectoryOffset, targetOffset: 0x1980d8),
                v => v.Verify(propertyName: "OffsetToDirectory", index: 0, fieldOffset: ImageResourceDirectoryEntry.DataAndDirectoryOffset, targetOffset: 0x1980a0)
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
                v => (RT) v.Type == RT.RT_VERSION //Note: this comes from the parent
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
                v => v.Verify(propertyName: "OffsetToData", index: 0, fieldOffset: 0, targetOffset: 0x1980f0)
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
                v => v.FileVersionLS == 1482493011,
                v => v.FileVersionRevision == 3155,
                v => v.FileVersionBuild == 22621,
                v => v.ProductVersionMS == 655360,
                v => v.ProductVersionMinor == 0,
                v => v.ProductVersionMajor == 10,
                v => v.ProductVersionLS == 1482493011,
                v => v.ProductVersionRevision == 3155,
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
                    c => c.VerifyField(name: "dwFileVersionLS", value: 1482493011),
                    c => c.VerifyField(name: "dwProductVersionMS", value: 655360),
                    c => c.VerifyField(name: "dwProductVersionLS", value: 1482493011),
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
                    c => c.VerifyField(name: "dwDacTimeStamp", value: (uint) 1733245982),
                    c => c.VerifyField(name: "dwDacSizeOfImage", value: 1347584),
                    c => c.VerifyField(name: "dwDbiTimeStamp", value: (uint) 1733245977),
                    c => c.VerifyField(name: "dwDbiSizeOfImage", value: 1249280)
                )
            );
        }

        [TestMethod]
        public void MessageResourceData_Test()
        {
            TestStruct<MessageResourceData>(
                v => v.NumberOfBlocks == 3
            );

            TestView<MessageResourceData>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "MESSAGE_RESOURCE_DATA", offset: 6095192, size: 40,
                        c => c.VerifyField(name: "NumberOfBlocks", value: 3),
                        c => c.VerifyStructIgnoreChildren("MESSAGE_RESOURCE_BLOCK", offset: 0x5d015c, size: 12),
                        c => c.VerifyStructIgnoreChildren("MESSAGE_RESOURCE_BLOCK", offset: 0x5d0168, size: 12),
                        c => c.VerifyStructIgnoreChildren("MESSAGE_RESOURCE_BLOCK", offset: 0x5d0174, size: 12)
                    ),
                    after: 5
                )
            );
        }

        [TestMethod]
        public void MessageResourceBlock_Test()
        {
            TestStruct<MessageResourceBlock>(
                v => v.LowId == 805306369,
                v => v.HighId == 805306370,
                v => v.OffsetToEntries.ListedOffset == 40
            );

            TestView<MessageResourceBlock>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "MESSAGE_RESOURCE_BLOCK", offset: 6095196, size: 12,
                        c => c.VerifyField(name: "LowId", value: (uint) 805306369),
                        c => c.VerifyField(name: "HighId", value: (uint) 805306370),
                        c => c.VerifyField(name: "OffsetToEntries", value: 40)
                    ),
                    after: 2
                )
            );

            TestXRefs<MessageResourceBlock>(
                v => v.Verify(propertyName: "OffsetToEntries", index: 0, fieldOffset: MessageResourceBlock.OffsetToEntriesOffset, targetOffset: 0x5d0180)
            );
        }

        [TestMethod]
        public void MessageResourceEntry_Test()
        {
            TestStruct<MessageResourceEntry>(
                v => v.Length == 20,
                v => v.Flags == MessageResourceFlags.Unicode,
                v => v.Text == "Start\r\n"
            );

            TestView<MessageResourceEntry>(
                v => v.VerifyStruct(
                    name: "MESSAGE_RESOURCE_ENTRY", offset: 6095232, size: 20,
                    c => c.VerifyField(name: "Length", value: (short) 20),
                    c => c.VerifyField(name: "Flags", value: MessageResourceFlags.Unicode),
                    c => c.VerifyField(name: "Text", value: "Start\r\n")
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
                v => v.Verify(propertyName: "UnwindData", index: 2, fieldOffset: RuntimeFunction.UnwindDataOffset, targetOffset: 1424960)
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
                v => v.ExceptionHandler == 650796
            );

            TestView<UnwindInfo>(
                v => v.VerifyStruct(
                    name: "UNWIND_INFO", offset: 1425908, size: 72,
                    c => c.VerifyBitField(name: "Version", value: (byte) 1, bits: 3),
                    c => c.VerifyBitField(name: "Flags", value: UNW_FLAG.FHANDLER, bits: 5),
                    c => c.VerifyField(name: "SizeOfProlog", value: (byte) 44),
                    c => c.VerifyField(name: "CountOfCodes", value: (byte) 11),
                    c => c.VerifyBitField(name: "FrameRegister", value: UnwindInfo.Register.RAX, 4),
                    c => c.VerifyBitField(name: "FrameOffset", value: (byte) 0, 4),
                    c => c.VerifyStructIgnoreChildren(name: "UNWIND_CODE", offset: 0x15c1f8, size: 4),
                    c => c.VerifyStructIgnoreChildren(name: "UNWIND_CODE", offset: 0x15c1fc, size: 4),
                    c => c.VerifyStructIgnoreChildren(name: "UNWIND_CODE", offset: 0x15c200, size: 4),
                    c => c.VerifyStructIgnoreChildren(name: "UNWIND_CODE", offset: 0x15c204, size: 2),
                    c => c.VerifyStructIgnoreChildren(name: "UNWIND_CODE", offset: 0x15c206, size: 2),
                    c => c.VerifyStructIgnoreChildren(name: "UNWIND_CODE", offset: 0x15c208, size: 2),
                    c => c.VerifyStructIgnoreChildren(name: "UNWIND_CODE", offset: 0x15c20a, size: 2),
                    c => c.VerifyStructIgnoreChildren(name: "UNWIND_CODE", offset: 0x15c20c, size: 2),
                    c => c.VerifyStructIgnoreChildren(name: "UNWIND_CODE", offset: 0x15c20e, size: 2),
                    c => c.VerifyField(name: "ExceptionHandler", value: 0x9EE2C),
                    c => c.VerifyStructIgnoreChildren(name: "SCOPE_TABLE", offset: 0x15c214, size: 36),
                    c => c.VerifyStructIgnoreChildren(name: "_GS_HANDLER_DATA", offset: 0x15c238, size: 4)
                )
            );

            //We have tests for RVA<FuncInfo> and RVA<FuncInfo4> in the tests specific to them
            TestXRefs<UnwindInfo>(
                v => v.Verify(propertyName: nameof(UnwindInfo.ExceptionHandler), index: 0, fieldOffset: 28, targetOffset: 0x9ee2c)
            );
        }

        #region UnwindCode Types

        [TestMethod]
        public void UnwindCode_PushNonVolatile_Test()
        {
            TestStruct<UnwindCode.PushNonVolatile>(
                v => v.OpInfo == UnwindInfo.Register.R15,
                v => v.CodeOffset == 11,
                v => v.UnwindOp == UWOP.UWOP_PUSH_NONVOL
            );

            Assert.AreEqual(2, UnwindCode.PushNonVolatile.StructSize);

            TestView<UnwindCode.PushNonVolatile>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1424968, size: 2,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 11),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.UWOP_PUSH_NONVOL, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: UnwindInfo.Register.R15, bits: 4)
                )
            );
        }

        [TestMethod]
        public void UnwindCode_AllocLarge_Test()
        {
            TestStruct<UnwindCode.AllocLarge>(
                v => v.Size == 624,
                v => v.StructSize == 4,
                v => v.CodeOffset == 26,
                v => v.UnwindOp == UWOP.UWOP_ALLOC_LARGE,
                v => v.OpInfo == 0
            );

            TestView<UnwindCode.AllocLarge>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1424964, size: 4,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 26),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.UWOP_ALLOC_LARGE, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: (byte) 0, bits: 4),
                    c => c.VerifyField(name: "Size", value: (ushort) 624 / 8) //The serialized size has not been multiplied by 8
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
                v => v.UnwindOp == UWOP.UWOP_ALLOC_SMALL
            );

            Assert.AreEqual(2, UnwindCode.AllocSmall.StructSize);

            TestView<UnwindCode.AllocSmall>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1425036, size: 2,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 6),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.UWOP_ALLOC_SMALL, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: (byte) 5, bits: 4)
                )
            );
        }

        [TestMethod]
        public void UnwindCode_SetFpReg_Test()
        {
            TestStruct<UnwindCode.SetFpReg>(
                v => v.FrameRegister == UnwindInfo.Register.RBP,
                v => v.FrameOffset == 96,
                v => v.CodeOffset == 22,
                v => v.UnwindOp == UWOP.UWOP_SET_FPREG,
                v => v.OpInfo == 6
            );

            Assert.AreEqual(2, UnwindCode.SetFpReg.StructSize);

            TestView<UnwindCode.SetFpReg>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1433948, size: 2,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 22),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.UWOP_SET_FPREG, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: (byte) 6, bits: 4)
                )
            );
        }

        [TestMethod]
        public void UnwindCode_SaveNonVolatile_Test()
        {
            TestStruct<UnwindCode.SaveNonVolatile>(
                v => v.OpInfo == UnwindInfo.Register.RBP,
                v => v.StackOffset == 59,
                v => v.CodeOffset == 21,
                v => v.UnwindOp == UWOP.UWOP_SAVE_NONVOL
            );

            Assert.AreEqual(4, UnwindCode.SaveNonVolatile.StructSize);

            TestView<UnwindCode.SaveNonVolatile>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1425012, size: 4,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 21),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.UWOP_SAVE_NONVOL, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: UnwindInfo.Register.RBP, bits: 4),
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
                v => v.OpInfo == 1
            );

            Assert.AreEqual(2, UnwindCode.Epilog.StructSize);

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
                v => v.OpInfo == UnwindInfo.Register.RSI,
                v => v.StackOffset == 2,
                v => v.CodeOffset == 36,
                v => v.UnwindOp == UWOP.UWOP_SAVE_XMM128
            );

            Assert.AreEqual(4, UnwindCode.SaveXmm128.StructSize);

            TestView<UnwindCode.SaveXmm128>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1453140, size: 4,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 36),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.UWOP_SAVE_XMM128, bits: 4),
                    c => c.VerifyBitField(name: "OpInfo", value: UnwindInfo.Register.RSI, bits: 4),
                    c => c.VerifyField(name: "StackOffset", value: (ushort) 2)
                )
            );
        }

        [TestMethod]
        public void UnwindCode_PushMachFrame_Test()
        {
            TestStruct<UnwindCode.PushMachFrame>(
                v => v.CodeOffset == 0,
                v => v.UnwindOp == UWOP.UWOP_PUSH_MACHFRAME,
                v => v.OpInfo == 0
            );

            Assert.AreEqual(2, UnwindCode.PushMachFrame.StructSize);

            TestView<UnwindCode.PushMachFrame>(
                v => v.VerifyStruct(
                    name: "UNWIND_CODE", offset: 1473530, size: 2,
                    c => c.VerifyField(name: "CodeOffset", value: (byte) 0),
                    c => c.VerifyBitField(name: "UnwindOp", value: UWOP.UWOP_PUSH_MACHFRAME, bits: 4),
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
                v => v.Count == 2
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

            TestXRefs<ScopeTable.ScopeRecord>(
                v => v.Verify(propertyName: nameof(ScopeTable.ScopeRecord.BeginAddress), index: 0, fieldOffset: 0, targetOffset: 0x12d2),
                v => v.Verify(propertyName: nameof(ScopeTable.ScopeRecord.EndAddress), index: 1, fieldOffset: 4, targetOffset: 0x12df),
                v => v.Verify(propertyName: nameof(ScopeTable.ScopeRecord.HandlerAddress), index: 2, fieldOffset: 8, targetOffset: 0xa6d40),
                v => v.Verify(propertyName: nameof(ScopeTable.ScopeRecord.JumpTarget), index: 3, fieldOffset: 12, targetOffset: 0x12df)
            );
        }

        #region FuncInfo

        [TestMethod]
        public void FuncInfo_Test()
        {
            TestStruct<FuncInfo>(
                nameof(EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3),
                v => v.magicNumber == EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3,
                v => v.bbtFlags == 0,
                v => v.maxState == 2,
                v => v.dispUnwindMap.ListedOffset == 67060,
                v => v.nTryBlocks == 1,
                v => v.dispTryBlockMap.ListedOffset == 67076,
                v => v.nIPMapEntries == 1,
                v => v.dispIPtoStateMap.ListedOffset == 67120,
                v => v.dispUnwindHelp == 32,
                v => v.dispESTypeList == 0,
                v => v.EHFlags == 5
            );

            TestView<FuncInfo>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "FuncInfo", offset: 60816, size: 40,
                        c => c.VerifyBitField(name: "magicNumber", value: EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3, bits: 29),
                        c => c.VerifyBitField(name: "bbtFlags", value: (BBT) 0, bits: 3),
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
                ),
                scenario: nameof(EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3)
            );

            //There's 4 xrefs: the third one is the TryBlockMap.HandlerArray
            TestXRefs<FuncInfo>(
                nameof(EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3),
                v => v.Verify(propertyName: nameof(FuncInfo.dispUnwindMap),    index: 0, fieldOffset: FuncInfo.UnwindMapOffset, targetOffset: 67060),
                v => v.Verify(propertyName: nameof(FuncInfo.dispTryBlockMap),  index: 3, fieldOffset: FuncInfo.TryBlockMapOffset, targetOffset: 67076),
                v => v.Verify(propertyName: nameof(FuncInfo.dispIPtoStateMap), index: 6, fieldOffset: FuncInfo.IPToStateMapOffset, targetOffset: 67120)
            );
        }

        [TestMethod]
        public void TryBlockMapEntry_Test()
        {
            TestStruct<TryBlockMapEntry>(
                v => v.tryLow == 0,
                v => v.tryHigh == 0,
                v => v.catchHigh == 1,
                v => v.nCatches == 1,
                v => v.dispHandlerArray.ListedOffset == 67096
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
                v => v.Verify(propertyName: nameof(TryBlockMapEntry.dispHandlerArray), index: 0, fieldOffset: TryBlockMapEntry.dispHandlerArrayOffset, targetOffset: 67096)
            );
        }

        [TestMethod]
        public void HandlerType_Test()
        {
            TestStruct<HandlerType>(
                v => v.adjectives == HT.HT_IsStdDotDot,
                v => v.dispType.ListedOffset == 0,
                v => v.dispCatchObj == 0,
                v => v.dispOfHandler == 39879,
                v => v.dispFrame == 56
            );

            TestView<HandlerType>(
                v => v.VerifyStruct(
                    name: "HandlerType", offset: 67096, size: 20,
                    c => c.VerifyField(name: "adjectives", value: HT.HT_IsStdDotDot),
                    c => c.VerifyField(name: "dispType", value: 0),
                    c => c.VerifyField(name: "dispCatchObj", value: 0),
                    c => c.VerifyField(name: "dispOfHandler", value: 39879),
                    c => c.VerifyField(name: "dispFrame", value: 56)
                )
            );

            TestXRefs<HandlerType>(
                v => v.Verify(propertyName: nameof(HandlerType.dispType), index: 0, fieldOffset: HandlerType.dispTypeOffset, targetOffset: 1527960)
            );
        }

        [TestMethod]
        public void TypeDescriptor_Test()
        {
            TestStruct<TypeDescriptor>(
                v => v.pVFTable == 269732080,
                v => v.spare == 0,
                v => v.name == ".?AUCInBufferException@@"
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
                    name: "IptoStateMapEntry", offset: 67120, size: 8,
                    c => c.VerifyField(name: "Ip", value: 15737),
                    c => c.VerifyField(name: "State", value: 0)
                )
            );

            TestXRefs<IptoStateMapEntry>(
                v => v.Verify(propertyName: nameof(IptoStateMapEntry.Ip), index: 0, fieldOffset: 0, targetOffset: 0x3d79)
            );
        }

        [TestMethod]
        public void UnwindMapEntry_Test()
        {
            TestStruct<UnwindMapEntry>(
                v => v.toState == -1,
                v => v.action == 0
            );

            TestView<UnwindMapEntry>(
                v => v.VerifyStruct(
                    name: "UnwindMapEntry", offset: 67060, size: 8,
                    c => c.VerifyField(name: "toState", value: -1),
                    c => c.VerifyField(name: "action", value: 0)
                )
            );

            TestXRefs<UnwindMapEntry>(
                v => v.Verify(propertyName: nameof(UnwindMapEntry.action), index: 0, fieldOffset: 4, targetOffset: 0x3580e8)
            );
        }

        [TestMethod]
        public void FuncInfoV1_Test()
        {
            TestStruct<FuncInfo>(
                scenario: nameof(EH_MAGIC_NUMBER.EH_MAGIC_NUMBER1),
                v => v.magicNumber == EH_MAGIC_NUMBER.EH_MAGIC_NUMBER1,
                v => v.bbtFlags == 0,
                v => v.maxState == 2,
                v => v.dispUnwindMap.ListedOffset == 1297860,
                v => v.nTryBlocks == 0,
                v => v.dispTryBlockMap.ListedOffset == 0,
                v => v.nIPMapEntries == 5,
                v => v.dispIPtoStateMap.ListedOffset == 1297784
            );

            TestView<FuncInfo>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "FuncInfo", offset: 1198144, size: 28,
                        c => c.VerifyBitField(name: "magicNumber", value: EH_MAGIC_NUMBER.EH_MAGIC_NUMBER1, bits: 29),
                        c => c.VerifyBitField(name: "bbtFlags", value: (BBT) 0, bits: 3),
                        c => c.VerifyField(name: "maxState", value: 2),
                        c => c.VerifyField(name: "dispUnwindMap", value: 1297860),
                        c => c.VerifyField(name: "nTryBlocks", value: 0),
                        c => c.VerifyField(name: "dispTryBlockMap", value: 0),
                        c => c.VerifyField(name: "nIPMapEntries", value: 5),
                        c => c.VerifyField(name: "dispIPtoStateMap", value: 1297784)
                    ),
                    after: 7
                ),
                scenario: nameof(EH_MAGIC_NUMBER.EH_MAGIC_NUMBER1)
            );

            //There's xrefs; the first, second and last xrefs are ours
            TestXRefs<FuncInfo>(
                nameof(EH_MAGIC_NUMBER.EH_MAGIC_NUMBER1),
                v => v.Verify(propertyName: nameof(FuncInfo.dispUnwindMap), index: 0, fieldOffset: FuncInfo.UnwindMapOffset, targetOffset: 1305096),
                v => v.Verify(propertyName: nameof(FuncInfo.dispTryBlockMap), index: 4, fieldOffset: FuncInfo.TryBlockMapOffset, targetOffset: 1305016),
                v => v.Verify(propertyName: nameof(FuncInfo.dispIPtoStateMap), index: 11, fieldOffset: FuncInfo.IPToStateMapOffset, targetOffset: 1304840)
            );
        }

        #endregion
        #region FuncInfo4

        [TestMethod]
        public void FuncInfo4_Test()
        {
            TestStruct<FuncInfo4>(
                //v => v.header == PESpy.FuncInfoHeader,
                v => v.bbtFlags == 0,
                v => v.dispUnwindMap.ListedOffset == 225013,
                v => v.dispTryBlockMap.ListedOffset == 0,
                v => v.dispIPtoStateMap.ListedOffset == 226945,
                v => v.dispToSegMap.ListedOffset == 0,
                v => v.dispFrame == 0
            );

            TestView<FuncInfo4>(
                WithIgnores(
                    before: 1,
                    v => v.VerifyStruct(
                        name: "FuncInfo4", offset: 226936, size: 9,
                        c => c.VerifyStructFieldIgnoreChildren(name: "header", type: "FuncInfoHeader", offset: 226936, size: 1),
                        c => c.VerifyField(name: "dispUnwindMap", value: 225013),
                        c => c.VerifyField(name: "dispIPtoStateMap", value: 226945)
                    ),
                    after: 1
                )
            );

            //FuncInfo4 is variable length hence many fields can report to exist at the same fieldOffset
            TestXRefs<FuncInfo4>(
                v => v.Verify(propertyName: nameof(FuncInfo4.dispUnwindMap), index: 0, fieldOffset: 1, targetOffset: 0x36ef5),
                v => v.Verify(propertyName: nameof(FuncInfo4.dispTryBlockMap), index: 1, fieldOffset: 5, targetOffset: 0x36ce4),
                v => v.Verify(propertyName: nameof(FuncInfo4.dispIPtoStateMap), index: 2, fieldOffset: 5, targetOffset: 0x37681),
                v => v.Verify(propertyName: nameof(FuncInfo4.dispToSegMap), index: 6, fieldOffset: 5, targetOffset: 0x413b13)
            );
        }

        [TestMethod]
        public void FuncInfoHeader_Test()
        {
            TestStruct<FuncInfoHeader>(
                v => v.isCatch == false,
                v => v.isSeparated == false,
                v => v.BBT == false,
                v => v.UnwindMap == true,
                v => v.TryBlockMap == false,
                v => v.EHs == true,
                v => v.NoExcept == false,
                v => v.reserved == 0,
                v => v.Value == 40
            );

            TestView<FuncInfoHeader>(
                v => v.VerifyStruct(
                    name: "FuncInfoHeader", offset: 226936, size: 1,
                    c => c.VerifyBitField(name: "isCatch", value: (byte) 0, bits: 1),
                    c => c.VerifyBitField(name: "isSeparated", value: (byte) 0, bits: 1),
                    c => c.VerifyBitField(name: "BBT", value: (byte) 0, bits: 1),
                    c => c.VerifyBitField(name: "UnwindMap", value: (byte) 1, bits: 1),
                    c => c.VerifyBitField(name: "TryBlockMap", value: (byte) 0, bits: 1),
                    c => c.VerifyBitField(name: "EHs", value: (byte) 1, bits: 1),
                    c => c.VerifyBitField(name: "NoExcept", value: (byte) 0, bits: 1),
                    c => c.VerifyBitField(name: "reserved", value: (byte) 0, bits: 1)
                )
            );
        }

        [TestMethod]
        public void HandlerMap4_Test()
        {
            TestStruct<HandlerMap4>(
                v => v.NumEntries == 2
            );

            TestView<HandlerMap4>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "HandlerMap4", offset: 224492, size: 0,
                        c => c.VerifyField(name: "NumEntries", value: 2),
                        c => c.VerifyStructIgnoreChildren(name: "HandlerType4", offset: 0x36CED, size: 12),
                        c => c.VerifyStructIgnoreChildren(name: "HandlerType4", offset: 0x36CF9, size: 7)
                    ),
                    after: 1
                )
            );
        }

        [TestMethod]
        public void HandlerType4_Test()
        {
            TestStruct<HandlerType4>(
                //v => v.header == PESpy.HandlerTypeHeader,
                v => v.adjectives == HT.HT_IsReference,
                v => v.dispType.ListedOffset == 242472,
                v => v.dispCatchObj == 32,
                v => v.dispOfHandler == 143084,
                v => GetSpan(v, "continuationAddresses") == new[] { 0x84c0 }
            );

            TestView<HandlerType4>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "HandlerType4", offset: 224493, size: 12,
                        c => c.VerifyStructFieldIgnoreChildren(name: "header", type: "HandlerTypeHeader", offset: 224493, size: 1),
                        c => c.VerifyField(name: "adjectives", value: HT.HT_IsReference),
                        c => c.VerifyField(name: "dispType", value: 242472),
                        c => c.VerifyField(name: "dispCatchObj", value: 32),
                        c => c.VerifyField(name: "dispOfHandler", value: 143084),
                        c => c.VerifyField(name: "continuationAddresses", value: new[] { 56 })
                    ),
                    after: 1
                )
            );

            TestXRefs<HandlerType4>(
                v => v.Verify(propertyName: nameof(HandlerType4.dispType), index: 0, fieldOffset: 2, targetOffset: 0x3b328),

                //Don't know how to convert this to an RVA yet
                //v => v.Verify(propertyName: nameof(HandlerType4.dispCatchObj), index: -1, fieldOffset: -1, targetOffset: -1),

                v => v.Verify(propertyName: nameof(HandlerType4.dispOfHandler), index: 1, fieldOffset: 7, targetOffset: 0x22eec),
                v => v.Verify(propertyName: nameof(HandlerType4.continuationAddresses), index: 2, fieldOffset: 11, targetOffset: 0x84c0)
            );
        }

        [TestMethod]
        public void HandlerTypeHeader_Test()
        {
            TestStruct<HandlerTypeHeader>(
                v => v.adjectives == true,
                v => v.dispType == true,
                v => v.dispCatchObj == true,
                v => v.contIsRVA == false,
                v => v.contAddr == HandlerTypeHeader.contType.ONE,
                v => v.unused == 0,
                v => v.Value == 23
            );

            TestView<HandlerTypeHeader>(
                v => v.VerifyStruct(
                    name: "HandlerTypeHeader", offset: 224493, size: 1,
                    c => c.VerifyField(name: "adjectives", value: (byte) 1),
                    c => c.VerifyField(name: "dispType", value: (byte) 1),
                    c => c.VerifyField(name: "dispCatchObj", value: (byte) 1),
                    c => c.VerifyField(name: "contIsRVA", value: (byte) 0),
                    c => c.VerifyField(name: "contAddr", value: HandlerTypeHeader.contType.ONE),
                    c => c.VerifyField(name: "unused", value: (byte) 0)
                )
            );
        }

        [TestMethod]
        public void IPtoStateMap4_Test()
        {
            TestStruct<IPtoStateMap4>(
                v => v.NumEntries == 2
            );

            TestView<IPtoStateMap4>(
                v => v.VerifyStruct(
                    name: "IPtoStateMap4", offset: 226945, size: 0,
                    c => c.VerifyField(name: "NumEntries", value: 2),
                    c => c.VerifyStructIgnoreChildren(name: "IPtoStateMapEntry4", offset: 0x37682, size: 2),
                    c => c.VerifyStructIgnoreChildren(name: "IPtoStateMapEntry4", offset: 0x37684, size: 2)
                )
            );
        }

        [TestMethod]
        public void IPtoStateMapEntry4_Test()
        {
            TestStruct<HandlerType4>(
                //v => v.header == PESpy.HandlerTypeHeader,
                v => v.adjectives == HT.HT_IsReference,
                v => v.dispType.ListedOffset == 242472,
                v => v.dispCatchObj == 32,
                v => v.dispOfHandler == 143084,
                v => GetSpan(v, "continuationAddresses") == new[] { 0x84C0 }
            );

            TestView<HandlerType4>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "HandlerType4", offset: 224493, size: 12,
                        c => c.VerifyStructFieldIgnoreChildren(name: "header", type: "HandlerTypeHeader", offset: 224493, size: 1),
                        c => c.VerifyField(name: "adjectives", value: HT.HT_IsReference),
                        c => c.VerifyField(name: "dispType", value: 242472),
                        c => c.VerifyField(name: "dispCatchObj", value: 32),
                        c => c.VerifyField(name: "dispOfHandler", value: 143084),
                        c => c.VerifyField(name: "continuationAddresses", value: new[] { 56 })
                    ),
                    after: 1
                )
            );

            TestXRefs<IPtoStateMapEntry4>(
                v => v.Verify(propertyName: nameof(IPtoStateMapEntry4.Ip), index: 0, fieldOffset: 0, targetOffset: 0x4b99)
            );
        }

        [TestMethod]
        public void SepIPtoStateMap4_Test()
        {
            TestStruct<SepIPtoStateMap4>(
                v => v.NumEntries == 2
            );

            TestView<SepIPtoStateMap4>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "SepIPtoStateMap4", offset: 4274963, size: 17,
                        c => c.VerifyField(name: "NumEntries", value: 2),
                        c => c.VerifyStructIgnoreChildren(name: "SepIPtoStateMapEntry4", offset: 0x413B14, size: 8),
                        c => c.VerifyStructIgnoreChildren(name: "SepIPtoStateMapEntry4", offset: 0x413B1C, size: 8)
                    ),
                    after: 2 //Ignore the global IPtoStateMap4 entries referenced from the SepIPtoStateMapEntry4 entries
                )
            );
        }

        [TestMethod]
        public void SepIPtoStateMapEntry4_Test()
        {
            TestStruct<SepIPtoStateMapEntry4>(
                v => v.addrStartRVA == 5008,
                v => v.dispOfIPMap.ListedOffset == 4284196
            );

            TestView<SepIPtoStateMapEntry4>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "SepIPtoStateMapEntry4", offset: 4274964, size: 8,
                        c => c.VerifyField(name: "addrStartRVA", value: 5008),
                        c => c.VerifyField(name: "dispOfIPMap", value: 4284196)
                    ),
                    after: 1
                )
            );

            TestXRefs<SepIPtoStateMapEntry4>(
                v => v.Verify(propertyName: nameof(SepIPtoStateMapEntry4.addrStartRVA), index: 0, fieldOffset: 0, targetOffset: 0x1390),
                v => v.Verify(propertyName: nameof(SepIPtoStateMapEntry4.dispOfIPMap), index: 1, fieldOffset: 4, targetOffset: 0x413b24)
            );
        }

        [TestMethod]
        public void TryBlockMap4_Test()
        {
            TestStruct<TryBlockMap4>(
                v => v.NumEntries == 1
            );

            TestView<TryBlockMap4>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "TryBlockMap4", offset: 224484, size: 8,
                        c => c.VerifyField(name: "NumEntries", value: 1),
                        c => c.VerifyStructIgnoreChildren(name: "TryBlockMapEntry4", offset: 0x36CE5, size: 7)
                    ),
                    after: 2
                )
            );
        }

        [TestMethod]
        public void TryBlockMapEntry4_Test()
        {
            TestStruct<TryBlockMapEntry4>(
                v => v.tryLow == 0,
                v => v.tryHigh == 0,
                v => v.catchHigh == 1,
                v => v.dispHandlerArray.ListedOffset == 224492
            );

            TestView<TryBlockMapEntry4>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "TryBlockMapEntry4", offset: 224485, size: 7,
                        c => c.VerifyField(name: "tryLow", value: 0),
                        c => c.VerifyField(name: "tryHigh", value: 0),
                        c => c.VerifyField(name: "catchHigh", value: 1),
                        c => c.VerifyField(name: "dispHandlerArray", value: 224492)
                    ),
                    after: 2
                )
            );

            TestXRefs<TryBlockMapEntry4>(
                v => v.Verify(propertyName: nameof(TryBlockMapEntry4.dispHandlerArray), index: 0, fieldOffset: 16, targetOffset: 67096)
            );
        }

        [TestMethod]
        public void UnwindMapEntry4_Test()
        {
            TestStruct<UnwindMapEntry4>(
                v => v.nextOffset == 1,
                v => v.type == UnwindMapEntry4.Type.RVA,
                v => v.action == 141906,
                v => v.@object == 0
            );

            TestView<UnwindMapEntry4>(
                v => v.VerifyStruct(
                    name: "UnwindMapEntry4", offset: 225014, size: 5,
                    c => c.VerifyField(name: "nextOffset", value: 1),
                    c => c.VerifyField(name: "type", value: UnwindMapEntry4.Type.RVA),
                    c => c.VerifyField(name: "action", value: 141906)

                    //object field is not present since our type was RVA
                )
            );

            TestXRefs<UnwindMapEntry4>(
                v => v.Verify(propertyName: nameof(UnwindMapEntry4.action), index: 0, fieldOffset: 1, targetOffset: 141906)

                //Not sure how to convert object into an absolute position, so for now no xref. AzureAttest ExceptionData 551
                //has one of these
                //v => v.Verify(propertyName: nameof(UnwindMapEntry4.@object), index: -1, fieldOffset: -1, targetOffset: -1)
            );
        }

        [TestMethod]
        public void UWMap4_Test()
        {
            TestStruct<UWMap4>(
                v => v.NumEntries == 1
            );

            TestView<UWMap4>(
                v => v.VerifyStruct(
                    name: "UWMap4", offset: 225013, size: 6,
                    c => c.VerifyField(name: "NumEntries", value: 1),
                    c => c.VerifyStructIgnoreChildren(name: "UnwindMapEntry4", offset: 0x36ef6, size: 5)
                )
            );
        }

        #endregion
        #endregion
        #region Security Table (4)

        [TestMethod]
        public void WinCertificate_Test()
        {
            TestStruct<WinCertificate>(
                v => v.Length == 28792,
                v => v.Revision == WIN_CERT_REVISION.WIN_CERT_REVISION_2_0,
                v => v.CertificateType == WIN_CERT_TYPE.WIN_CERT_TYPE_PKCS_SIGNED_DATA
                //I think this string shows dates in the local time format, and so can't easily be validated in CI
//              v => v.Certificate.ToString() == @"[Subject]
//  CN=Microsoft Windows, O=Microsoft Corporation, L=Redmond, S=Washington, C=US

//[Issuer]
//  CN=Microsoft Windows Production PCA 2011, O=Microsoft Corporation, L=Redmond, S=Washington, C=US

//[Serial Number]
//  330000045C3D5672666CB7541700000000045C

//[Not Before]
//  15/09/2023 4:20:38 AM

//[Not After]
//  5/09/2024 4:20:38 AM

//[Thumbprint]
//  58DA14F4C5941747B995956FDC89B4E3AAE47B8F
//"
);

            TestView<WinCertificate>(
                v => v.VerifyStruct(
                    name: "WIN_CERTIFICATE", offset: 2158592, size: 28792,
                    c => c.VerifyField(name: "dwLength", value: 28792),
                    c => c.VerifyField(name: "wRevision", value: WIN_CERT_REVISION.WIN_CERT_REVISION_2_0),
                    c => c.VerifyField(name: "wCertificateType", value: WIN_CERT_TYPE.WIN_CERT_TYPE_PKCS_SIGNED_DATA),
                    c => c.VerifyStruct(
                        name: "SignedData", offset: 2158600, size: 28784,
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
                //I think this string shows dates in the local time format, and so can't easily be validated in CI
//              v => v.Certificate.ToString() == @"[Subject]
//  CN=Microsoft Windows, O=Microsoft Corporation, L=Redmond, S=Washington, C=US

//[Issuer]
//  CN=Microsoft Windows Production PCA 2011, O=Microsoft Corporation, L=Redmond, S=Washington, C=US

//[Serial Number]
//  330000045C3D5672666CB7541700000000045C

//[Not Before]
//  15/09/2023 4:20:38 AM

//[Not After]
//  5/09/2024 4:20:38 AM

//[Thumbprint]
//  58DA14F4C5941747B995956FDC89B4E3AAE47B8F
//"
);

            TestView<SignedData>(
                v => v.VerifyStruct(
                    name: "SignedData", offset: 2158600, size: 28784,
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

                    c => c.VerifyBitField(name: "Type", value: IMAGE_REL_BASED.IMAGE_REL_BASED_DIR64, bits: 4),
                    c => c.VerifyBitField(name: "Offset", value: (short) 0, bits: 12),

                    c => c.VerifyBitField(name: "Type", value: IMAGE_REL_BASED.IMAGE_REL_BASED_DIR64, bits: 4),
                    c => c.VerifyBitField(name: "Offset", value: (short) 8, bits: 12),

                    c => c.VerifyBitField(name: "Type", value: IMAGE_REL_BASED.IMAGE_REL_BASED_DIR64, bits: 4),
                    c => c.VerifyBitField(name: "Offset", value: (short) 16, bits: 12),

                    c => c.VerifyBitField(name: "Type", value: IMAGE_REL_BASED.IMAGE_REL_BASED_DIR64, bits: 4),
                    c => c.VerifyBitField(name: "Offset", value: (short) 24, bits: 12)
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
                v => v.Type == IMAGE_DEBUG_TYPE_CODEVIEW,
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
                    c => c.VerifyField(name: "Type", value: IMAGE_DEBUG_TYPE_CODEVIEW),
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
                    c => c.VerifyField(name: "dwSig", value: (int) CodeViewSig.RSDS), //It's rendered as a HexString, so it's typed as an int
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

        #region NB05

        [TestMethod]
        public void ImageDebugDirectory_CodeView_NB05_OMFDirHeader()
        {
            TestStruct<OMFDirHeader>(
                v => v.cbDirHeader == 16,
                v => v.cbDirEntry == 12,
                v => v.cDir == 293,
                v => v.lfoNextDir == 0,
                v => v.flags == 0
            );

            TestView<OMFDirHeader>(
                v => v.VerifyStruct(
                    name: "OMFDirHeader", offset: 186496, size: 16,
                    c => c.VerifyField(name: "cbDirHeader", value: (ushort) 16),
                    c => c.VerifyField(name: "cbDirEntry", value: (ushort) 12),
                    c => c.VerifyField(name: "cDir", value: 293),
                    c => c.VerifyField(name: "lfoNextDir", value: 0),
                    c => c.VerifyField(name: "flags", value: 0)
                )
            );
        }

        [TestMethod]
        public void ImageDebugDirectory_CodeView_NB05_OMFDirEntry()
        {
            TestStruct<OMFDirEntry>(
                v => v.SubSection == SST.sstModule,
                v => v.iMod == 1,
                v => v.lfo == 8,
                v => v.cb == 49,
                v => v.Data == IgnoreValue
            );

            TestView<OMFDirEntry>(
                v => v.VerifyStruct(
                    name: "OMFDirEntry", offset: 186512, size: 12,
                    c => c.VerifyField(name: "SubSection", value: SST.sstModule),
                    c => c.VerifyField(name: "iMod", value: (ushort) 1),
                    c => c.VerifyField(name: "lfo", value: 8),
                    c => c.VerifyField(name: "cb", value: 49)
                )
            );
        }

        [TestMethod]
        public void ImageDebugDirectory_CodeView_NB05_OMFModule()
        {
            TestStruct<OMFModule>(
                v => v.ovlNumber == 0,
                v => v.iLib == 0,
                v => v.cSeg == 2,
                v => v.Style == "CV",
                v => v.SegInfo == IgnoreValue,
                v => v.Name == ".\\Debug\\main.obj"
            );

            TestView<OMFModule>(
                v => v.VerifyStruct(
                    name: "OMFModule", offset: 56664, size: 49,
                    c => c.VerifyField(name: "ovlNumber", value: (ushort) 0),
                    c => c.VerifyField(name: "iLib", value: (ushort) 0),
                    c => c.VerifyField(name: "cSeg", value: (ushort) 2),
                    c => c.VerifyField(name: "Style", value: "CV"),
                    c => c.VerifyStructFieldArrayIgnoreChildren(name: "SegInfo"),
                    c => c.VerifyField(name: "Name", value: ".\\Debug\\main.obj")
                )
            );
        }

        [TestMethod]
        public void ImageDebugDirectory_CodeView_NB05_OMFSegDesc()
        {
            TestStruct<OMFSegDesc>(
                v => v.Seg == 1,
                v => v.pad == 0,
                v => v.Off == 0,
                v => v.cbSeg == 32
            );

            TestView<OMFSegDesc>(
                v => v.VerifyStruct(
                    name: "OMFSegDesc", offset: 56672, size: 12,
                    c => c.VerifyField(name: "Seg", value: (ushort) 1),
                    c => c.VerifyField(name: "pad", value: (ushort) 0),
                    c => c.VerifyField(name: "Off", value: 0),
                    c => c.VerifyField(name: "cbSeg", value: 32)
                )
            );
        }

        [TestMethod]
        public void ImageDebugDirectory_CodeView_NB05_OMFModuleSymbols()
        {
            TestStruct<OMFModuleSymbols>(
                v => v.Signature == CV_SIGNATURE.C11
                //v => v.List == PESpy.PDB.SymTypeList
            );

            //OMFModuleSymbols is just a wrapper for some global values
            TestView<OMFModuleSymbols>(
                v => v.VerifyValue(offset: 0x1007C, CV_SIGNATURE.C11),
                v => v.VerifyStructIgnoreChildren(name: "SEARCHSYM", offset: 0x10080, size: 12),
                v => v.VerifyStructIgnoreChildren(name: "OBJNAMESYM", offset: 0x1008C, size: 24),
                v => v.VerifyStructIgnoreChildren(name: "CFLAGSYM", offset: 0x100A4, size: 72),
                v => v.VerifyStructIgnoreChildren(name: "PROCSYM32", offset: 0x100EC, size: 44),
                v => v.VerifyStructIgnoreChildren(name: "BPRELSYM32", offset: 0x10118, size: 20),
                v => v.VerifyStructIgnoreChildren(name: "BPRELSYM32", offset: 0x1012C, size: 20),
                v => v.VerifyStructIgnoreChildren(name: "SYMTYPE", offset: 0x10140, size: 4)
            );
        }

        [TestMethod]
        public void ImageDebugDirectory_CodeView_NB05_OMFSourceModule()
        {
            TestStruct<OMFSourceModule>(
                v => v.cFile == 1,
                v => v.cSeg == 1,
                v => v.baseSrcFile == IgnoreValue
            );

            TestView<OMFSourceModule>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "OMFSourceModule", offset: 65860, size: 8,
                        c => c.VerifyField(name: "cFile", value: (ushort) 1),
                        c => c.VerifyField(name: "cSeg", value: (ushort) 1),
                        c => c.VerifyField(name: "baseSrcFile", new[] { 20 })
                    ),
                    after: 2
                )
            );

            TestXRefs<OMFSourceModule>(
                v => v.Verify(propertyName: "baseSrcFile", index: 1, fieldOffset: 4, targetOffset: 0x10158)
            );
        }

        [TestMethod]
        public void ImageDebugDirectory_CodeView_NB05_OMFSourceFile()
        {
            TestStruct<OMFSourceFile>(
                v => v.cSeg == 1,
                v => v.reserved == 0,
                v => v.baseSrcLn == IgnoreValue,
                //v => v.ranges == NativeSpan<RANGE>[1],
                v => v.cFName == 60,
                v => v.Name == "C:\\Program Files (x86)\\DevStudio\\MyProjects\\TestApp\\main.cpp"
            );

            TestView<OMFSourceFile>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "OMFSourceFile", offset: 65880, size: 80,
                        c => c.VerifyField(name: "cSeg", value: (ushort) 1),
                        c => c.VerifyField(name: "reserved", value: (ushort) 0),
                        c => c.VerifyField(name: "baseSrcLn", new[] { 100 }),
                        c => c.VerifyFieldIgnoreValue(name: "ranges"),
                        c => c.VerifyField(name: "cFName", value: (byte) 60),
                        c => c.VerifyField(name: "Name", value: "C:\\Program Files (x86)\\DevStudio\\MyProjects\\TestApp\\main.cpp"),
                        c => c.VerifyByteBlob(offset: 0x101A5, new byte[] {0, 0, 0})
                    ),
                    after: 1
                )
            );

            TestXRefs<OMFSourceFile>(
                v => v.Verify(propertyName: "baseSrcLn", index: 0, fieldOffset: 4, targetOffset: 0x101A8)
            );
        }

        [TestMethod]
        public void ImageDebugDirectory_CodeView_NB05_OMFSourceLine()
        {
            TestStruct<OMFSourceLine>(
                v => v.Seg == 1,
                v => v.cLnOff == 4
                //v => v.offset == NativeSpan<Int32>[4],
                //v => v.lineNbr == NativeSpan<UInt16>[4]
            );

            TestView<OMFSourceLine>(
                v => v.VerifyStruct(
                    name: "OMFSourceLine", offset: 65960, size: 28,
                    c => c.VerifyField(name: "Seg", value: (ushort) 1),
                    c => c.VerifyField(name: "cLnOff", value: (ushort) 4),
                    c => c.VerifyField(name: "offset", value: new[] {0, 3, 16, 18}),
                    c => c.VerifyField(name: "lineNbr", value: new ushort[] {4, 5, 6, 7})
                )
            );
        }

        [TestMethod]
        public void ImageDebugDirectory_CodeView_NB05_OMFHashedSymbols()
        {
            TestStruct<OMFHashedSymbols>(
                //v => v.Hash == PESpy.OMFSymHash,
                //v => v.Symbols == PESpy.PDB.SymTypeList,
                //v => v.SymbolHashTable == PESpy.ByteBlob,
                //v => v.AddressHashTable == PESpy.AddrHash32v12
            );

            var verifiers = new Action<IView>[460];
            verifiers[0] = v => v.VerifyStructIgnoreChildren(name: "OMFSymHash", offset: 0x1fe48, size: 16);

            //Ignore all the public symbols
            for (var i = 1; i < 458; i++)
                verifiers[i] = v => { };

            verifiers[458] = v => v.VerifyStructIgnoreChildren(name: "SymHash32Long", offset: 0x244cc, size: 3996);
            verifiers[459] = v => v.VerifyStructIgnoreChildren(name: "AddrHash32 (v12)", offset: 0x25468, size: 3660);

            TestView<OMFHashedSymbols>(
                verifiers
            );
        }

        //We should test OMFSymHash, OMFGlobalTypes and OMFFileIndex
        //We also have a type OMFModuleTypes, however this requires an sstTypes section which neither our NB09 or NB11 sample has

        #endregion
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
                v => v.DataType == IMAGE_DEBUG_MISC_TYPE.IMAGE_DEBUG_MISC_EXENAME,
                v => v.Length == 272,
                v => v.Unicode == false,
                v => v.Reserved == new byte[] { 0, 0, 0 },
                v => v.Data == "mfc40_opt.DBG"
            );

            TestView<ImageDebugMisc>(
                v => v.VerifyStruct(
                    name: "IMAGE_DEBUG_MISC", offset: 924672, size: 26,
                    c => c.VerifyField(name: "DataType", value: IMAGE_DEBUG_MISC_TYPE.IMAGE_DEBUG_MISC_EXENAME),
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
            TestStruct<IMAGE_DLLCHARACTERISTICS_EX, ImageDebugDirectory>(
                v => ((RawValue<IMAGE_DLLCHARACTERISTICS_EX>) v.Data).Value == IMAGE_DLLCHARACTERISTICS_EX_CET_COMPAT
            );

            TestView<IMAGE_DLLCHARACTERISTICS_EX, ImageDebugDirectory>(
                v => v.VerifyStruct(
                    name: "IMAGE_DEBUG_DIRECTORY", offset: 1296396, size: 28,
                    c => c.VerifyField(name: "Characteristics", value: 0),
                    c => c.VerifyField(name: "TimeDateStamp", value: (uint) 3169667970),
                    c => c.VerifyField(name: "MajorVersion", value: (ushort) 0),
                    c => c.VerifyField(name: "MinorVersion", value: (ushort) 0),
                    c => c.VerifyField(name: "Type", value: IMAGE_DEBUG_TYPE_EX_DLLCHARACTERISTICS),
                    c => c.VerifyField(name: "SizeOfData", value: 4),
                    c => c.VerifyField(name: "AddressOfRawData", value: 1424956),
                    c => c.VerifyField(name: "PointerToRawData", value: 1424956)
                ),
                v => v.VerifyValue(1424956, IMAGE_DLLCHARACTERISTICS_EX_CET_COMPAT)
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
                v => v.Characteristics == IMAGE_SCN_ALIGN.IMAGE_SCN_ALIGN_4BYTES
            );

            TestView<ImageTlsDirectory>(
                v => v.VerifyStruct(
                    name: "IMAGE_TLS_DIRECTORY", offset: 196072, size: 40,
                    c => c.VerifyField(name: "StartAddressOfRawData", value: (ulong) 6442675092),
                    c => c.VerifyField(name: "EndAddressOfRawData", value: (ulong) 6442675100),
                    c => c.VerifyField(name: "AddressOfIndex", value: (ulong) 6442697760),
                    c => c.VerifyField(name: "AddressOfCallBacks", value: (ulong) 6442651784),
                    c => c.VerifyField(name: "SizeOfZeroFill", value: 0),
                    c => c.VerifyField(name: "Characteristics", value: IMAGE_SCN_ALIGN.IMAGE_SCN_ALIGN_4BYTES)
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
                v => v.CastGuardOsDeterminedFailureMode.ListedAddress == 6444150816,
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
                        c => c.VerifyStructFieldIgnoreChildren(name: "CodeIntegrity", type: "IMAGE_LOAD_CONFIG_CODE_INTEGRITY", offset: 1257092, size: 12),
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

            using var peFile32 = PEFileFromKey(WellKnownTestModule.aadauthhelper);
            using var peFile64 = PEFileFromKey(WellKnownTestModule.ntdll);

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
                v => v.Verify(propertyName: "GuardAddressTakenIatEntryTable",           index: 4014, fieldOffset: cfg64.GuardAddressTakenIatEntryTableOffset,           targetOffset: 6629368), //SingleFileApp_EXE
                v => v.Verify(propertyName: "GuardLongJumpTargetTable",                 index: 8284, fieldOffset: cfg64.GuardLongJumpTargetTableOffset,                 targetOffset: 58236), //ntoskrnl
                v => v.Verify(propertyName: "GuardRFFailureRoutineFunctionPointer",     index: 6817, fieldOffset: cfg64.GuardRFFailureRoutineFunctionPointerOffset,     targetOffset: 4155656), //DbgEng
                v => v.Verify(propertyName: "DynamicValueRelocTableOffset",             index: 2221, fieldOffset: cfg64.DynamicValueRelocTableOffsetOffset,             targetOffset: 2155868), //ntdll
                v => v.Verify(propertyName: "GuardRFVerifyStackPointerFunctionPointer", index: 6818, fieldOffset: cfg64.GuardRFVerifyStackPointerFunctionPointerOffset, targetOffset: 4155664), //DbgEng
                v => v.Verify(propertyName: "EnclaveConfigurationPointer",              index: 1162, fieldOffset: cfg64.EnclaveConfigurationPointerOffset,              targetOffset: 206864), //AzureAttest
                v => v.Verify(propertyName: "GuardEHContinuationTable",                 index: 2222, fieldOffset: cfg64.GuardEHContinuationTableOffset,                 targetOffset: 1260432), //ntdll
                v => v.Verify(propertyName: "GuardXFGCheckFunctionPointer",             index: 2223, fieldOffset: cfg64.GuardXFGCheckFunctionPointerOffset,             targetOffset: 1667080), //ntdll
                v => v.Verify(propertyName: "GuardXFGDispatchFunctionPointer",          index: 2224, fieldOffset: cfg64.GuardXFGDispatchFunctionPointerOffset,          targetOffset: 1667088), //ntdll
                v => v.Verify(propertyName: "GuardXFGTableDispatchFunctionPointer",     index: 2225, fieldOffset: cfg64.GuardXFGTableDispatchFunctionPointerOffset,     targetOffset: 1667096), //ntdll
                v => v.Verify(propertyName: "CastGuardOsDeterminedFailureMode",         index: 2226, fieldOffset: cfg64.CastGuardOsDeterminedFailureModeOffset,        targetOffset: 1667104), //ntdll
                v => v.Verify(propertyName: "GuardMemcpyFunctionPointer",               index: 3735, fieldOffset: cfg64.GuardMemcpyFunctionPointerOffset,               targetOffset: 3917864) //coreclr
                
                //Need to do a brute force scan for UmaFunctionPointers
                //v => v.Verify(propertyName: "UmaFunctionPointers",                      index: -1, fieldOffset: -1, targetOffset: -1)
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
                v => v.VerifyStructIgnoreChildren(name: "__guard_iat_table", offset: 6629368, size: 15)
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
                        name: "__guard_fids_table", offset: 1261292, size: 11085
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
                    name: "__guard_eh_cont_table", offset: 1260432, size: 860
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
                v => v.VerifyStructIgnoreChildren(name: "__guard_longjmp_table", offset: 58236, size: 5)
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
                v => v.Symbol == IMAGE_DYNAMIC_RELOCATION_FUNCTION_OVERRIDE,
                v => v.BaseRelocSize == 180,
                v => v.Data == IgnoreValue
            );

            TestView<ImageDynamicRelocation>(
                v => v.VerifyStruct(
                    name: "IMAGE_DYNAMIC_RELOCATION", offset: 2155876, size: 192,
                    c => c.VerifyField(name: "Symbol", value: IMAGE_DYNAMIC_RELOCATION_FUNCTION_OVERRIDE),
                    c => c.VerifyField(name: "BaseRelocSize", value: 180),
                    c => c.VerifyStructIgnoreChildren(name: "IMAGE_FUNCTION_OVERRIDE_HEADER", offset: 2155888, size: 180)
                )
            );
        }

        [TestMethod]
        public void ImageDynamicRelocation_ntoskrnl()
        {
            //ntoskrnl has special PXE/PPE/PDE/PTE symbols that we want to confirm we can parse.

            using var peFile = PEFileFromKey(WellKnownTestModule.ntoskrnl);

            var dynamicRelocations = peFile.LoadConfigTable.DynamicValueRelocTableOffset.Value.DynamicRelocations;

            dynamicRelocations.Verify(
               "IMAGE_DYNAMIC_RELOCATION_GUARD_IMPORT_CONTROL_TRANSFER",
                "IMAGE_DYNAMIC_RELOCATION_GUARD_INDIR_CONTROL_TRANSFER",
                "IMAGE_DYNAMIC_RELOCATION_GUARD_SWITCHTABLE_BRANCH",
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
                    c => c.VerifyFieldIgnoreValue("TimeDateStamp"), //Attempting to compare datetimes will cause issues in CI
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
                v => (uint) v.TimeDateStamp == 998112782,
                v => v.OffsetModuleName == 69,
                v => v.Reserved == 0,
                v => v.Name.ListedOffset == 685
            );

            TestView<ImageBoundForwarderRef>(
                v => v.VerifyStruct(
                    name: "IMAGE_BOUND_FORWARDER_REF", offset: 624, size: 8,
                    c => c.VerifyFieldIgnoreValue(name: "TimeDateStamp"), //Attempting to compare datetimes will cause issues in CI
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
            using var peFile = PEFileFromKey(WellKnownTestModule.coreclr);

            var importAddressTable = peFile.ImportAddressTable;

            Assert.AreEqual(383, importAddressTable.Count);
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
            using var peFile = PEFileFromKey(WellKnownTestModule.coreclr);

            var delayLoad = peFile.DelayImportTable[0];

            Assert.AreEqual(4, delayLoad.ImportNameTableRVA.Value.Count);
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
                v => v.Flags == (COMIMAGE_FLAGS_ILONLY | COMIMAGE_FLAGS_32BITREQUIRED | COMIMAGE_FLAGS_STRONGNAMESIGNED),
                v => v.EntryPointRVA == 0,
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
                    c => c.VerifyStructFieldIgnoreChildren(name: "MetaData", type: "IMAGE_DATA_DIRECTORY", offset: 528, size: 8),
                    c => c.VerifyField(name: "Flags", value: (COMIMAGE_FLAGS_ILONLY | COMIMAGE_FLAGS_32BITREQUIRED | COMIMAGE_FLAGS_STRONGNAMESIGNED)),
                    c => c.VerifyField(name: "EntryPointToken", value: 0),
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
        public void Ecma335_ModelHeader_Test()
        {
            TestStruct<ModelHeader>(
                v => v.Reserved1 == 0,
                v => v.MajorVersion == 2,
                v => v.MinorVersion == 0,
                v => v.HeapSizes == 0,
                v => v.Reserved2 == 1,
                v => v.Valid == (TableMask.Module | TableMask.TypeRef | TableMask.TypeDef | TableMask.MethodDef | TableMask.Param | TableMask.MemberRef | TableMask.CustomAttribute | TableMask.Assembly | TableMask.AssemblyRef),
                v => v.Sorted == (TableMask.InterfaceImpl | TableMask.Constant | TableMask.CustomAttribute | TableMask.FieldMarshal | TableMask.DeclSecurity | TableMask.ClassLayout | TableMask.FieldLayout | TableMask.MethodSemantics | TableMask.MethodImpl | TableMask.ImplMap | TableMask.FieldRva | TableMask.NestedClass | TableMask.GenericParam | TableMask.GenericParamConstraint),
                v => v.RowCounts == new int[] { 1, 16, 2, 2, 1, 15, 14, 1, 1 }
            );

            TestView<ModelHeader>(
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

        //ModelHeap doesn't really "store" anything; it's just a type that holds the unpacked metadata

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
                    name: "IMAGE_COR_ILMETHOD_TINY", offset: 592, size: 1,
                    c => c.VerifyBitField(name: "Flags", value: (CorILMethodFlags.TinyFormat1 | CorILMethodFlags.MoreSects | CorILMethodFlags.InitLocals), bits: 2),
                    c => c.VerifyBitField(name: "CodeSize", value: 7, bits: 6)
                ),
                v => v.VerifyByteBlob(offset: 0x251, new byte[] { 2, 123, 63, 0, 0, 10, 42 })
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
        #region RTTI

        [TestMethod]
        public void RTTIBaseClassArray_Test()
        {
            TestStruct<RTTIBaseClassArray>(
                //v => v.arrayOfBaseClassDescriptors[0].ListedOffset == 4120056
            );

            //Note we do get a duplicate RTTIBaseClassArray in the values we write,
            //because our initial WriteGlobal does not write uniquely, and so
            //a child TTIClassHierarchyDescriptor will try and write the RTTIBaseClassArray
            //again
            TestView<RTTIBaseClassArray>(
                WithIgnores(
                    before: 1,
                    v => v.VerifyStruct(
                        name: "_RTTIBaseClassArray", offset: 4110824, size: 4,
                        c => c.VerifyField(name: "arrayOfBaseClassDescriptors", value: new[] { 4120056 })
                    ),
                    after: 3
                )
            );

            TestXRefs<RTTIBaseClassArray>(
                v => v.Verify(propertyName: "arrayOfBaseClassDescriptors", index: 0, fieldOffset: 0, targetOffset: 0x3eb9f8)
            );
        }

        [TestMethod]
        public void RTTIBaseClassDescriptor_Test()
        {
            TestStruct<RTTIBaseClassDescriptor>(
                v => v.pTypeDescriptor.ListedOffset == 4743008,
                v => v.numContainedBases == 0,
                //v => v.where == IgnoreValue,
                v => v.attributes == BCD.BCD_HASPCHD,
                v => v.pClassDescriptor.ListedOffset == 4120136
            );

            TestView<RTTIBaseClassDescriptor>(
                WithIgnores(
                    before: 2,
                    v => v.VerifyStruct(
                        name: "_RTTIBaseClassDescriptor", offset: 4110960, size: 28,
                        c => c.VerifyField(name: "pTypeDescriptor", value: 4743008),
                        c => c.VerifyField(name: "numContainedBases", value: 0),
                        c => c.VerifyFieldIgnoreValue(name: "where"),
                        c => c.VerifyField(name: "attributes", value: BCD.BCD_HASPCHD),
                        c => c.VerifyField(name: "pClassDescriptor", value: 4120136)
                    ),
                    after: 3
                )
            );

            TestXRefs<RTTIBaseClassDescriptor>(
                v => v.Verify(propertyName: "pTypeDescriptor", index: 0, fieldOffset: 0, targetOffset: 0x483a60),
                v => v.Verify(propertyName: "pClassDescriptor", index: 1, fieldOffset: 24, targetOffset: 0x3eb9d0)
            );
        }

        [TestMethod]
        public void RTTIClassHierarchyDescriptor_Test()
        {
            TestStruct<RTTIClassHierarchyDescriptor>(
                v => v.signature == 0,
                v => v.attributes == (CHD) 0,
                v => v.numBaseClasses == 1,
                v => v.pBaseClassArray.ListedOffset == 4120040
            );

            TestView<RTTIClassHierarchyDescriptor>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "_RTTIClassHierarchyDescriptor", offset: 4110800, size: 16,
                        c => c.VerifyField(name: "signature", value: 0),
                        c => c.VerifyField(name: "attributes", value: (CHD) 0),
                        c => c.VerifyField(name: "numBaseClasses", value: 1),
                        c => c.VerifyField(name: "pBaseClassArray", value: 4120040)
                    ),
                    after: 4
                )
            );

            TestXRefs<RTTIClassHierarchyDescriptor>(
                v => v.Verify(propertyName: "pBaseClassArray", index: 0, fieldOffset: 12, targetOffset: 0x3eb9e8)
            );
        }

        [TestMethod]
        public void RTTICompleteObjectLocator_Test()
        {
            TestStruct<RTTICompleteObjectLocator>(
                v => v.signature == COL_SIG.COL_SIG_REV1,
                v => v.offset == 0,
                v => v.cdOffset == 0,
                v => v.pTypeDescriptor.ListedOffset == 4743776,
                v => v.pClassDescriptor.ListedOffset == 4120016,
                v => v.pSelf == 4119976
            );

            TestView<RTTICompleteObjectLocator>(
                WithIgnores(
                    v => v.VerifyStruct(
                        name: "_RTTICompleteObjectLocator", offset: 4110760, size: 24,
                        c => c.VerifyField(name: "signature", value: COL_SIG.COL_SIG_REV1),
                        c => c.VerifyField(name: "offset", value: 0),
                        c => c.VerifyField(name: "cdOffset", value: 0),
                        c => c.VerifyField(name: "pTypeDescriptor", value: 4743776),
                        c => c.VerifyField(name: "pClassDescriptor", value: 4120016),
                        c => c.VerifyField(name: "pSelf", value: 4119976)
                    ),
                    after: 5
                )
            );

            TestXRefs<RTTICompleteObjectLocator>(
                v => v.Verify(propertyName: "pTypeDescriptor", index: 0, fieldOffset: 12, targetOffset: 0x483a60),
                v => v.Verify(propertyName: "pClassDescriptor", index: 1, fieldOffset: 16, targetOffset: 0x3eb9d0),
                v => v.Verify(propertyName: "pSelf", index: 6, fieldOffset: 20, targetOffset: 0x3edda8)
            );
        }

        #endregion
        #region NGEN
        #region BBT

#if FALSE
        [TestMethod]
        public void CorBBTProfBlobMethodDefEntry_Test()
        {
            var str = GenerateTest<CorBBTProfBlobMethodDefEntry>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfBlobPoolEntry_Test()
        {
            var str = GenerateTest<CorBBTProfBlobPoolEntry>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfBlobSignatureDefEntry_Test()
        {
            var str = GenerateTest<CorBBTProfBlobSignatureDefEntry>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfBlockData_Test()
        {
            var str = GenerateTest<CorBBTProfBlockData>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfBlockEntry_Test()
        {
            var str = GenerateTest<CorBBTProfBlockEntry>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfBlockEntryV1_Test()
        {
            var str = GenerateTest<CorBBTProfBlockEntryV1>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfBlockNamespaceDefEntry_Test()
        {
            var str = GenerateTest<CorBBTProfBlockNamespaceDefEntry>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfBlockTypeDefEntry_Test()
        {
            var str = GenerateTest<CorBBTProfBlockTypeDefEntry>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfFileHeader_Test()
        {
            var str = GenerateTest<CorBBTProfFileHeader>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfFileOptionalHeader_Test()
        {
            var str = GenerateTest<CorBBTProfFileOptionalHeader>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfMethodBlockCountsSectionHeader_Test()
        {
            var str = GenerateTest<CorBBTProfMethodBlockCountsSectionHeader>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfMethodBlockCountsSectionHeaderV1_Test()
        {
            var str = GenerateTest<CorBBTProfMethodBlockCountsSectionHeaderV1>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfMethodDetailHeader_Test()
        {
            var str = GenerateTest<CorBBTProfMethodDetailHeader>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfMethodHeader_Test()
        {
            var str = GenerateTest<CorBBTProfMethodHeader>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfMethodHeaderV1_Test()
        {
            var str = GenerateTest<CorBBTProfMethodHeaderV1>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfMethodInfo_Test()
        {
            var str = GenerateTest<CorBBTProfMethodInfo>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfParamSigEntry_Test()
        {
            var str = GenerateTest<CorBBTProfParamSigEntry>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfScenarioHeader_Test()
        {
            var str = GenerateTest<CorBBTProfScenarioHeader>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfScenarioInfo_Test()
        {
            var str = GenerateTest<CorBBTProfScenarioInfo>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfScenarioInfoSectionHeader_Test()
        {
            var str = GenerateTest<CorBBTProfScenarioInfoSectionHeader>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfScenarioRun_Test()
        {
            var str = GenerateTest<CorBBTProfScenarioRun>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfSectionTableEntry_Test()
        {
            var str = GenerateTest<CorBBTProfSectionTableEntry>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfSectionTableHeader_Test()
        {
            var str = GenerateTest<CorBBTProfSectionTableHeader>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfTokenInfo_Test()
        {
            var str = GenerateTest<CorBBTProfTokenInfo>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfTokenListEntryV1_Test()
        {
            var str = GenerateTest<CorBBTProfTokenListEntryV1>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorBBTProfTokenListSectionHeader_Test()
        {
            var str = GenerateTest<CorBBTProfTokenListSectionHeader>();
            throw new NotImplementedException();
        }

#endif
        #endregion
        #region Directories

        //The tests after this region test individual structs,
        //but we also need to test each of the specific directories

        [TestMethod]
        public void Ngen_HelperTable()
        {
            using var peFile = PEFile.FromFile(Sample.NGEN_NI_DLL);

            var helperTable = peFile.NgenHelperTable;

            //The CLR has various "zap-specific" representations of various data structures
            //These appear to be the in-memory representation. The helper table appears
            //to be represented as ZapHelperThunk

            Assert.AreEqual(4, helperTable.Length);
        }

        [TestMethod]
        public void Ngen_ImportSections()
        {
            using var peFile = PEFile.FromFile(Sample.NGEN_NI_DLL);

            var importSections = peFile.NgenImportSections;

            Assert.AreEqual(3, importSections.Length);
        }

        [TestMethod]
        public void Ngen_ImportTable()
        {
            using var peFile = PEFile.FromFile(Sample.NGEN_NI_DLL);

            var importTable = peFile.NgenImportTable;

            Assert.AreEqual(1, importTable.Length);
        }

        [TestMethod]
        public void Ngen_StubsData()
        {
            using var peFile = PEFile.FromFile("C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\NativeImages\\mscorlib.ni.dll");

            //Don't currently know how to handle stubs data

            Assert.ThrowsException<NotImplementedException>(() => peFile.NgenStubsData);
        }

        [TestMethod]
        public void Ngen_VersionInfo()
        {
            using var peFile = PEFile.FromFile(Sample.NGEN_NI_DLL);

            var versionInfo = peFile.NgenVersionInfo;

            Assert.AreEqual(IMAGE_FILE_MACHINE_AMD64, versionInfo.wMachine);
            Assert.AreEqual(2, versionInfo.runtimeDllInfo.Length);
        }

        [TestMethod]
        public void Ngen_Dependencies()
        {
            using var peFile = PEFile.FromFile(Sample.NGEN_NI_DLL);

            var dependencies = peFile.NgenDependencies;

            Assert.AreEqual(2, dependencies.Length);
        }

        [TestMethod]
        public void Ngen_DebugMap()
        {
            using var peFile = PEFile.FromFile(Sample.NGEN_NI_DLL);

            var debugMap = peFile.NgenDebugMap;

            Assert.AreEqual(1, debugMap.Length);
        }

        [TestMethod]
        public void Ngen_ModuleImage()
        {
            using var peFile = PEFile.FromFile(Sample.NGEN_NI_DLL);

            var moduleImage = peFile.NgenModuleImage;

            //I don't know if the serialization of the "Module" data structure is different based on
            //the .NET version that the file was NGEN'd under, so for now this is not implemented
            Assert.IsInstanceOfType(moduleImage, typeof(ByteBlob));
        }

        [TestMethod]
        public void Ngen_CodeManagerTable()
        {
            using var peFile = PEFile.FromFile(Sample.NGEN_NI_DLL);

            var codeManagerTable = peFile.NgenCodeManagerTable;

            Assert.AreEqual(11744, codeManagerTable.Code.VirtualAddress);
            Assert.AreEqual(50, codeManagerTable.Code.Size);

            //The rest was all 0
        }

        [TestMethod]
        public void Ngen_ProfileDataList()
        {
            //Haven't found a file that has this yet
            Assert.Inconclusive();
        }

        [TestMethod]
        public void Ngen_ManifestMetaData()
        {
            using var peFile = PEFile.FromFile(Sample.NGEN_NI_DLL);

            var manifestMetadata = peFile.NgenManifestMetaData;

            //There are two sets of ECMA-335 metadata in the file; the manifest metadata
            //and the metadata pointed to by the IMAGE_COR20_HEADER. It's only the IMAGE_COR20_HEADER
            //one that contains TestLib.Class1 in it
            var modelHeap = manifestMetadata.ModelHeap;

            Assert.AreEqual("<Module>", modelHeap.TypeDefTable.Single().ToString());
        }

        [TestMethod]
        public void Ngen_VirtualSectionsTable()
        {
            using var peFile = PEFile.FromFile(Sample.NGEN_NI_DLL);

            var virtualSectionsTable = peFile.NgenVirtualSectionsTable;

            Assert.AreEqual(40, virtualSectionsTable.Length);
        }

        [TestMethod]
        public void Ngen_EEInfoTable()
        {
            using var peFile = PEFile.FromFile(Sample.NGEN_NI_DLL);

            //In both our sample file and mscorlib.ni.dll this is all 0, so this isn't implemented yet

            Assert.ThrowsException<NotImplementedException>(() => peFile.NgenEEInfoTable);
        }

        #endregion

        [TestMethod]
        public void CorCompileAssemblySignature_Test()
        {
            var str = GenerateTest<CorCompileAssemblySignature>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorCompileCodeManagerEntry_Test()
        {
            var str = GenerateTest<CorCompileCodeManagerEntry>();
            throw new NotImplementedException();
        }

#if FALSE
        [TestMethod]
        public void CorCompileColdMethodEntry_Test()
        {
            var str = GenerateTest<CorCompileColdMethodEntry>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorCompileDebugLabelledEntry_Test()
        {
            var str = GenerateTest<CorCompileDebugLabelledEntry>();
            throw new NotImplementedException();
        }
#endif

        [TestMethod]
        public void CorCompileDepepdency_Test()
        {
            var str = GenerateTest<CorCompileDepepdency>();
            throw new NotImplementedException();
        }

#if FALSE
        [TestMethod]
        public void CorCompileEEInfoTable_Test()
        {
            var str = GenerateTest<CorCompileEEInfoTable>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorCompileExceptionClause_Test()
        {
            var str = GenerateTest<CorCompileExceptionClause>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorCompileExceptionLookupTable_Test()
        {
            var str = GenerateTest<CorCompileExceptionLookupTable>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorCompileExceptionLookupTableEntry_Test()
        {
            var str = GenerateTest<CorCompileExceptionLookupTableEntry>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorCompileExternalMethodThunk_Test()
        {
            var str = GenerateTest<CorCompileExternalMethodThunk>();
            throw new NotImplementedException();
        }
#endif

        [TestMethod]
        public void CorCompileHeader_Test()
        {
            var str = GenerateTest<CorCompileHeader>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorCompileImportTableEntry_Test()
        {
            var str = GenerateTest<CorCompileImportTableEntry>();
            throw new NotImplementedException();
        }

#if FALSE
        [TestMethod]
        public void CorCompileMethodProfileList_Test()
        {
            var str = GenerateTest<CorCompileMethodProfileList>();
            throw new NotImplementedException();
        }
#endif

        [TestMethod]
        public void CorCompileRuntimeDllInfo_Test()
        {
            var str = GenerateTest<CorCompileRuntimeDllInfo>();
            throw new NotImplementedException();
        }

        [TestMethod]
        public void CorCompileVersionInfo_Test()
        {
            var str = GenerateTest<CorCompileVersionInfo>();
            throw new NotImplementedException();
        }

#if FALSE
        [TestMethod]
        public void CorCompileVirtualImportThunk_Test()
        {
            var str = GenerateTest<CorCompileVirtualImportThunk>();
            throw new NotImplementedException();
        }
#endif

        [TestMethod]
        public void CorCompileVirtualSectionInfo_Test()
        {
            var str = GenerateTest<CorCompileVirtualSectionInfo>();
            throw new NotImplementedException();
        }

        #endregion
        #region R2R

        [TestMethod]
        public void ReadyToRunCoreHeader_Test()
        {
            TestStruct<R2R.ReadyToRunCoreHeader>(
                v => v.Flags == (ReadyToRunFlag.READYTORUN_FLAG_SKIP_TYPE_VALIDATION | ReadyToRunFlag.READYTORUN_FLAG_NONSHARED_PINVOKE_STUBS | ReadyToRunFlag.READYTORUN_FLAG_MULTIMODULE_VERSION_BUBBLE),
                v => v.NumberOfSections == 11,
                v => v.Sections == IgnoreValue
            );

            TestView<R2R.ReadyToRunCoreHeader>(
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
            TestStruct<R2R.ReadyToRunHeader>(
                v => v.Signature == 5395538,
                v => v.MajorVersion == 10,
                v => v.MinorVersion == 1
            );

            TestView<R2R.ReadyToRunHeader>(
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
            TestStruct<R2R.ReadyToRunImportSection>(
                v => v.Flags == ReadyToRunImportSectionFlags.PCode,
                v => v.Type == ReadyToRunImportSectionType.StubDispatch,
                v => v.EntrySize == 8,
                v => v.Signatures == 0,
                v => v.AuxiliaryData == 6088
            );

            TestView<R2R.ReadyToRunImportSection>(
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
            TestStruct<R2R.ReadyToRunSection>(
                v => v.Type == ReadyToRunSectionType.CompilerIdentifier,
                v => v.Data.ToString() == "Crossgen2 9.0.425.16305"
            );

            TestView<R2R.ReadyToRunSection>(
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
                    after: 54 //All of the nested files and the ECMA 335 metadata regions are written as "globals" and contribute to this count
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
                v => v.DbiVersion == CorDebugInterfaceVersion.CorDebugVersion_4_0,
                v => v.ContinueStartupEvent == 5376933008
            );

            TestView<ClrEngineMetrics>(
                v => v.VerifyStruct(
                    name: "CLR_ENGINE_METRICS", offset: 8213632, size: 16,
                    c => c.VerifyField(name: "Size", value: 16),
                    c => c.VerifyField(name: "DbiVersion", value: CorDebugInterfaceVersion.CorDebugVersion_4_0),
                    c => c.VerifyField(name: "ContinueStartupEvent", value: (ulong) 5376933008)
                )
            );
        }

        [TestMethod]
        public void DotNetRuntimeDebugHeader_Test()
        {
            TestStruct<NativeAOT.DotNetRuntimeDebugHeader>(
                v => v.Cookie == 0x48444E44,
                v => v.MajorVersion == 4,
                v => v.MinorVersion == 0,
                v => v.Flags == 1,
                v => v.ReservedPadding1 == 0
            );

            TestView<NativeAOT.DotNetRuntimeDebugHeader>(
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
            TestXRefs<NativeAOT.DotNetRuntimeDebugHeader>(
                v => v.Verify(propertyName: "DebugTypeEntries", index: 0, fieldOffset: 16, targetOffset: 1466672),
                v => v.Verify(propertyName: "GlobalValueEntries", index: 185, fieldOffset: 24, targetOffset: 1469072)
            );
        }

        [TestMethod]
        public void DebugTypeEntry_Test()
        {
            TestStruct<NativeAOT.DebugTypeEntry>(
                v => v.TypeName.Value == "GcDacVars",
                v => v.FieldName.Value == "SIZEOF",
                v => v.FieldOffset == 320,
                v => v.ReservedPadding == 0
            );

            TestView<NativeAOT.DebugTypeEntry>(
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

            TestXRefs<NativeAOT.DebugTypeEntry>(
                v => v.Verify(propertyName: "TypeName",  index: 0, fieldOffset: 0, targetOffset: 1305776),
                v => v.Verify(propertyName: "FieldName", index: 1, fieldOffset: 8, targetOffset: 1305788)
            );
        }

        [TestMethod]
        public void GlobalValueEntry_Test()
        {
            TestStruct<NativeAOT.GlobalValueEntry>(
                v => v.Name.Value == "g_CrashInfoBuffer"
            );

            TestView<NativeAOT.GlobalValueEntry>(
                v => v.VerifyValue(0x13F210, "g_CrashInfoBuffer"),
                v => v.VerifyStruct(
                    name: "GlobalValueEntry", offset: 1469072, size: 16,
                    c => c.VerifyFieldIgnoreValue(name: "Name"),
                    c => c.VerifyFieldIgnoreValue(name: "Address")
                )
            );

            TestXRefs<NativeAOT.GlobalValueEntry>(
                v => v.Verify(propertyName: "Name", index: 0, fieldOffset: NativeAOT.GlobalValueEntry.NameOffset, targetOffset: 1307152)
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
        #region RPC

        [TestMethod]
        public void PEFile_RpcInfo_StressTest()
        {
            var exts = new[]
            {
                "*.dll",
                "*.exe"
            };

            foreach (var ext in exts)
            {
                var files = Directory.EnumerateFiles("C:\\Windows\\system32", ext);

                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Debugger.IsAttached ? 1 : -1
                };

                Parallel.ForEach(files, options, fileName =>
                {
                    using var file = Detector.TryOpenFile(fileName);

                    if (file.Kind != FileKind.PE)
                        return;

                    var peFile = (PEFile) file;

                    var rpcInfo = peFile.RpcInfo;

                    if (rpcInfo == null)
                        return;

                    foreach (var iface in rpcInfo.ClientInterfaces)
                    {
                        var midlStublessProxyInfo = iface.InterpreterInfo;

                        if (midlStublessProxyInfo.IsEmpty)
                            continue;

                        if (!midlStublessProxyInfo.IsValid)
                            throw new NotImplementedException();

                        var formatStringOffset = midlStublessProxyInfo.Value.FormatStringOffset;
                        var formatString = midlStublessProxyInfo.Value.ProcFormatString;
                        var syntaxInfos = midlStublessProxyInfo.Value.pSyntaxInfo;

                        TestRpcInterpreterInfo(
                            formatStringOffset,
                            formatString,
                            syntaxInfos
                        );
                    }

                    foreach (var iface in rpcInfo.ServerInterfaces)
                    {
                        var midlServerInfo = iface.InterpreterInfo;

                        if (midlServerInfo.IsEmpty)
                            continue;

                        if (!midlServerInfo.IsValid)
                            throw new NotImplementedException();

                        var dispatchTable = midlServerInfo.Value.DispatchTable;
                        var formatStringOffset = midlServerInfo.Value.FmtStringOffset;
                        var formatString = midlServerInfo.Value.ProcString;

                        var syntaxInfos = midlServerInfo.Value.pSyntaxInfo;

                        TestRpcInterpreterInfo(
                            formatStringOffset,
                            formatString,
                            syntaxInfos
                        );
                    }
                });
            }
        }

        private void TestRpcInterpreterInfo(
            VA<NativeSpan<ushort>> formatStringOffset,
            RpcFormatString formatString,
            VA<MidlSyntaxInfo[]> pSyntaxInfo)
        {
            if (pSyntaxInfo.IsValid)
            {
                TestRpcSyntaxInfo(pSyntaxInfo.Value);
            }
        }

        private void TestRpcSyntaxInfo(MidlSyntaxInfo[] pSyntaxInfo)
        {
            foreach (var syntaxInfo in pSyntaxInfo)
            {
                if (syntaxInfo.TransferSyntax.SyntaxGUID == RpcInfo.NDR64TransferSyntax)
                {
                    var formatStrings = syntaxInfo.FmtStringOffset64;
                }
                else
                {
                    var formatStringOffset = syntaxInfo.FmtStringOffset;
                    var formatString = syntaxInfo.ProcString;
                }

                var dispatchTable = syntaxInfo.DispatchTable;
            }
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
                v => v.Type == IMAGE_SYM_TYPE_NULL,
                v => v.BasicType == IMAGE_SYM_TYPE_NULL,
                v => v.DerivedType == IMAGE_SYM_DTYPE.IMAGE_SYM_DTYPE_NULL,
                v => v.StorageClass == IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC,
                v => v.NumberOfAuxSymbols == 0,
                v => v.AuxSymbols == IgnoreValue
            );

            TestView<ImageSymbol>(
                v => v.VerifyStruct(
                    name: "IMAGE_SYMBOL", offset: 61472, size: 18,
                    c => c.VerifyField(name: "Name", value: "@comp.id"),
                    c => c.VerifyField(name: "Value", value: (uint) 0),
                    c => c.VerifyField(name: "SectionNumber", value: (ushort) 65535),
                    c => c.VerifyField(name: "Type", value: IMAGE_SYM_TYPE_NULL),
                    c => c.VerifyField(name: "StorageClass", value: IMAGE_SYM_CLASS.IMAGE_SYM_CLASS_STATIC),
                    c => c.VerifyField(name: "NumberOfAuxSymbols", value: (byte) 0)
                )
            );
        }

        [TestMethod]
        public void ImageAuxSymbol_Test()
        {
            //We don't currently support writing specific fields
            TestStruct<ImageAuxSymbol>();

            TestView<ImageAuxSymbol>(
                c => c.VerifyStruct(name: "IMAGE_AUX_SYMBOL", offset: 0xf044, size: 18,
                    c1 => c1.VerifyField(name: "Bytes", new byte[]
                    {
                        33, 0, 0, 0, 0, 0, 3, 0, 177, 65, 55, 6, 0, 0, 1, 0, 0, 0
                    })
                )
            );
        }

        #endregion
    }
}
