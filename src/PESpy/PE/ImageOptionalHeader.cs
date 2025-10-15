using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_OPTIONAL_HEADER32"/> / <see cref="IMAGE_OPTIONAL_HEADER64"/> structure.
    /// </summary>
    public class ImageOptionalHeader : IValue, IViewable //Structs return copies from properties, and ref properties don't display properly in the debugger
    {
        private const int MagicOffset = 0;
        private const int MajorLinkerVersionOffset = 2;
        private const int MinorLinkerVersionOffset = 3;
        private const int SizeOfCodeOffset = 4;
        private const int SizeOfInitializedDataOffset = 8;
        private const int SizeOfUninitializedDataOffset = 12;
        private const int AddressOfEntryPointOffset = 16;
        private const int BaseOfCodeOffset = 20;
        private const int BaseOfDataOffset = 24;
        private int ImageBaseOffset => chunk.Is32Bit ? 28 : 24;
        private const int SectionAlignmentOffset = 32;
        private const int FileAlignmentOffset = 36;
        private const int MajorOperatingSystemVersionOffset = 40;
        private const int MinorOperatingSystemVersionOffset = 42;
        private const int MajorImageVersionOffset = 44;
        private const int MinorImageVersionOffset = 46;
        private const int MajorSubsystemVersionOffset = 48;
        private const int MinorSubsystemVersionOffset = 50;
        private const int Win32VersionValueOffset = 52;
        private const int SizeOfImageOffset = 56;
        private const int SizeOfHeadersOffset = 60;
        private const int CheckSumOffset = 64;
        private const int SubsystemOffset = 68;
        private const int DllCharacteristicsOffset = 70;
        private const int SizeOfStackReserveOffset = 72;
        private int SizeOfStackCommitOffset => 72 + chunk.PointerSize;
        private int SizeOfHeapReserveOffset => 72 + (2 * chunk.PointerSize);
        private int SizeOfHeapCommitOffset => 72 + (3 * chunk.PointerSize);
        private int LoaderFlagsOffset => 72 + (4 * chunk.PointerSize);
        private int NumberOfRvaAndSizesOffset => 76 + (4 * chunk.PointerSize);
        private int ExportTableDirectoryOffset => 80 + (4 * chunk.PointerSize);
        private int ImportTableDirectoryOffset => 88 + (4 * chunk.PointerSize);
        private int ResourceTableDirectoryOffset => 96 + (4 * chunk.PointerSize);
        private int ExceptionTableDirectoryOffset => 104 + (4 * chunk.PointerSize);
        private int SecurityTableDirectoryOffset => 112 + (4 * chunk.PointerSize);
        private int BaseRelocationTableDirectoryOffset => 120 + (4 * chunk.PointerSize);
        private int DebugTableDirectoryOffset => 128 + (4 * chunk.PointerSize);
        private int CopyrightTableDirectoryOffset => 136 + (4 * chunk.PointerSize);
        private int GlobalPointerTableDirectoryOffset => 144 + (4 * chunk.PointerSize);
        private int ThreadLocalStorageTableDirectoryOffset => 152 + (4 * chunk.PointerSize);
        private int LoadConfigTableDirectoryOffset => 160 + (4 * chunk.PointerSize);
        private int BoundImportTableDirectoryOffset => 168 + (4 * chunk.PointerSize);
        private int ImportAddressTableDirectoryOffset => 176 + (4 * chunk.PointerSize);
        private int DelayImportTableDirectoryOffset => 184 + (4 * chunk.PointerSize);
        private int CorHeaderTableDirectoryOffset => 192 + (4 * chunk.PointerSize);
        private int NullDirectoryOffset => 200 + (4 * chunk.PointerSize);

        #region Standard Fields

        /// <summary>
        /// Identifies the format of the image file.
        /// </summary>
        public PEMagic Magic => (PEMagic) chunk.PeekUInt16(MagicOffset);

        /// <summary>
        /// The linker major version number.
        /// </summary>
        public byte MajorLinkerVersion => chunk.PeekByte(MajorLinkerVersionOffset);

        /// <summary>
        /// The linker minor version number.
        /// </summary>
        public byte MinorLinkerVersion => chunk.PeekByte(MinorLinkerVersionOffset);

        /// <summary>
        /// The size of the code (text) section, or the sum of all code sections if there are multiple sections.
        /// </summary>
        public int SizeOfCode => chunk.PeekInt32(SizeOfCodeOffset);

        /// <summary>
        /// The size of the initialized data section, or the sum of all such sections if there are multiple data sections.
        /// </summary>
        public int SizeOfInitializedData => chunk.PeekInt32(SizeOfInitializedDataOffset);

        /// <summary>
        /// The size of the uninitialized data section (BSS), or the sum of all such sections if there are multiple BSS sections.
        /// </summary>
        public int SizeOfUninitializedData => chunk.PeekInt32(SizeOfUninitializedDataOffset);

        /// <summary>
        /// The address of the entry point relative to the image base when the PE file is loaded into memory.
        /// For program images, this is the starting address. For device drivers, this is the address of the initialization function.
        /// An entry point is optional for DLLs. When no entry point is present, this field must be zero.
        /// </summary>
        public int AddressOfEntryPoint => chunk.PeekInt32(AddressOfEntryPointOffset);

        /// <summary>
        /// The address that is relative to the image base of the beginning-of-code section when it is loaded into memory.
        /// </summary>
        public int BaseOfCode => chunk.PeekInt32(BaseOfCodeOffset);

        /// <summary>
        /// The address that is relative to the image base of the beginning-of-data section when it is loaded into memory.
        /// </summary>
        public int BaseOfData => chunk.Is32Bit ? chunk.PeekInt32(BaseOfDataOffset) : 0;

        #endregion
        #region Windows Specific Fields

        /// <summary>
        /// The preferred address of the first byte of image when loaded into memory; must be a multiple of 64K.
        /// </summary>
        /// <remarks>
        /// Debuggers receive LOAD_DLL_DEBUG_EVENT events when kernel32!MapViewOfFile attempts to map a section into memory, which occurs when a process
        /// calls kernel32!LoadLibrary, or or kernel32!CreateFileMapping with SEC_IMAGE. If the image was loaded via kernel32!CreateFileMapping,
        /// this value may not be the address that the image was actually loaded at.
        /// </remarks>
        public long ImageBase => chunk.Is32Bit ? (long) chunk.PeekPointer(28) : (long) chunk.PeekPointer(24);

        /// <summary>
        /// The alignment (in bytes) of sections when they are loaded into memory. It must be greater than or equal to <see cref="FileAlignment"/>.
        /// The default is the page size for the architecture.
        /// </summary>
        public int SectionAlignment => chunk.PeekInt32(SectionAlignmentOffset);

        /// <summary>
        /// The alignment factor (in bytes) that is used to align the raw data of sections in the image file.
        /// The value should be a power of 2 between 512 and 64K, inclusive. The default is 512.
        /// If the <see cref="SectionAlignment"/> is less than the architecture's page size,
        /// then <see cref="FileAlignment"/> must match <see cref="SectionAlignment"/>.
        /// </summary>
        public int FileAlignment => chunk.PeekInt32(FileAlignmentOffset);

        /// <summary>
        /// The major version number of the required operating system.
        /// </summary>
        public ushort MajorOperatingSystemVersion => chunk.PeekUInt16(MajorOperatingSystemVersionOffset);

        /// <summary>
        /// The minor version number of the required operating system.
        /// </summary>
        public ushort MinorOperatingSystemVersion => chunk.PeekUInt16(MinorOperatingSystemVersionOffset);

        /// <summary>
        /// The major version number of the image.
        /// </summary>
        public ushort MajorImageVersion => chunk.PeekUInt16(MajorImageVersionOffset);

        /// <summary>
        /// The minor version number of the image.
        /// </summary>
        public ushort MinorImageVersion => chunk.PeekUInt16(MinorImageVersionOffset);

        /// <summary>
        /// The major version number of the subsystem.
        /// </summary>
        public ushort MajorSubsystemVersion => chunk.PeekUInt16(MajorSubsystemVersionOffset);

        /// <summary>
        /// The minor version number of the subsystem.
        /// </summary>
        public ushort MinorSubsystemVersion => chunk.PeekUInt16(MinorSubsystemVersionOffset);

        /// <summary>
        /// This member is reserved and must be 0.
        /// </summary>
        public int Win32VersionValue => chunk.PeekInt32(Win32VersionValueOffset);

        /// <summary>
        /// The size (in bytes) of the image, including all headers, as the image is loaded in memory.
        /// It must be a multiple of <see cref="SectionAlignment"/>. This does not include overlay data, which is not loaded into memory.
        /// </summary>
        public int SizeOfImage => chunk.PeekInt32(SizeOfImageOffset);

        /// <summary>
        /// The combined size of an MS DOS stub, PE header, and section headers rounded up to a multiple of FileAlignment.
        /// </summary>
        public int SizeOfHeaders => chunk.PeekInt32(SizeOfHeadersOffset);

        /// <summary>
        /// The image file checksum.
        /// </summary>
        public uint CheckSum => chunk.PeekUInt32(CheckSumOffset);

        /// <summary>
        /// The subsystem that is required to run this image.
        /// </summary>
        public ImageSubsystem Subsystem => (ImageSubsystem) chunk.PeekUInt16(SubsystemOffset);

        /// <summary>
        /// The DLL characteristics of the image.
        /// </summary>
        public ImageDllCharacteristics DllCharacteristics => (ImageDllCharacteristics) chunk.PeekUInt16(DllCharacteristicsOffset);

        /// <summary>
        /// The size of the stack to reserve. Only <see cref="SizeOfStackCommit"/> is committed;
        /// the rest is made available one page at a time until the reserve size is reached.
        /// </summary>
        public ulong SizeOfStackReserve => chunk.PeekPointer(SizeOfStackReserveOffset);

        /// <summary>
        /// The size of the stack to commit.
        /// </summary>
        public ulong SizeOfStackCommit => chunk.PeekPointer(SizeOfStackCommitOffset);

        /// <summary>
        /// The size of the local heap space to reserve. Only <see cref="SizeOfHeapCommit"/> is committed;
        /// the rest is made available one page at a time until the reserve size is reached.
        /// </summary>
        public ulong SizeOfHeapReserve => chunk.PeekPointer(SizeOfHeapReserveOffset);

        /// <summary>
        /// The size of the local heap space to commit.
        /// </summary>
        public ulong SizeOfHeapCommit => chunk.PeekPointer(SizeOfHeapCommitOffset);

        /// <summary>
        /// This member is obsolete.
        /// </summary>
        public ImageLoaderFlags LoaderFlags => (ImageLoaderFlags) chunk.PeekUInt32(LoaderFlagsOffset);

        /// <summary>
        /// The number of data-directory entries in the remainder of the <see cref="ImageOptionalHeader"/>. Each describes a location and size.
        /// </summary>
        public int NumberOfRvaAndSizes => chunk.PeekInt32(NumberOfRvaAndSizesOffset);

        #endregion
        #region Directory Entries

        /// <remarks>
        /// Gets information about the size and location of the export directory (IMAGE_DIRECTORY_ENTRY_EXPORT).
        /// </remarks>
        public ImageDataDirectory ExportTableDirectory => NumberOfRvaAndSizes >= 1 ? new ImageDataDirectory(chunk.Slice(ExportTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the import directory (IMAGE_DIRECTORY_ENTRY_IMPORT).
        /// </remarks>
        public ImageDataDirectory ImportTableDirectory => NumberOfRvaAndSizes >= 2 ? new ImageDataDirectory(chunk.Slice(ImportTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the resource directory (IMAGE_DIRECTORY_ENTRY_RESOURCE).
        /// </remarks>
        public ImageDataDirectory ResourceTableDirectory => NumberOfRvaAndSizes >= 3 ? new ImageDataDirectory(chunk.Slice(ResourceTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the exception directory (IMAGE_DIRECTORY_ENTRY_EXCEPTION).
        /// </remarks>
        public ImageDataDirectory ExceptionTableDirectory => NumberOfRvaAndSizes >= 4 ? new ImageDataDirectory(chunk.Slice(ExceptionTableDirectoryOffset)) : default;

        /// <summary>
        /// Gets information about the size and location of the security (certificate table) directory (IMAGE_DIRECTORY_ENTRY_SECURITY).<para/>
        /// The Certificate Table entry points to a table of attribute certificates.
        /// </summary>
        /// <remarks>
        /// These certificates are not loaded into memory as part of the image.
        /// As such, the first field of this entry, which is normally an RVA, is a file pointer instead.
        /// </remarks>
        public ImageDataDirectory SecurityTableDirectory => NumberOfRvaAndSizes >= 5 ? new ImageDataDirectory(chunk.Slice(SecurityTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the base relocation table (IMAGE_DIRECTORY_ENTRY_BASERELOC).
        /// </remarks>
        public ImageDataDirectory BaseRelocationTableDirectory => NumberOfRvaAndSizes >= 6 ? new ImageDataDirectory(chunk.Slice(BaseRelocationTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the debug directory (IMAGE_DIRECTORY_ENTRY_DEBUG).
        /// </remarks>
        public ImageDataDirectory DebugTableDirectory => NumberOfRvaAndSizes >= 7 ? new ImageDataDirectory(chunk.Slice(DebugTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the architecture-specific data (IMAGE_DIRECTORY_ENTRY_COPYRIGHT or IMAGE_DIRECTORY_ENTRY_ARCHITECTURE).
        /// </remarks>
        public ImageDataDirectory CopyrightTableDirectory => NumberOfRvaAndSizes >= 8 ? new ImageDataDirectory(chunk.Slice(CopyrightTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the the relative virtual address of the global pointer (IMAGE_DIRECTORY_ENTRY_GLOBALPTR).
        /// </remarks>
        public ImageDataDirectory GlobalPointerTableDirectory => NumberOfRvaAndSizes >= 9 ? new ImageDataDirectory(chunk.Slice(GlobalPointerTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the thread local storage directory (IMAGE_DIRECTORY_ENTRY_TLS).
        /// </remarks>
        public ImageDataDirectory ThreadLocalStorageTableDirectory => NumberOfRvaAndSizes >= 10 ? new ImageDataDirectory(chunk.Slice(ThreadLocalStorageTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the load configuration directory (IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG).
        /// </remarks>
        public ImageDataDirectory LoadConfigTableDirectory => NumberOfRvaAndSizes >= 11 ? new ImageDataDirectory(chunk.Slice(LoadConfigTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the bound import directory (IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT).
        /// </remarks>
        public ImageDataDirectory BoundImportTableDirectory => NumberOfRvaAndSizes >= 12 ? new ImageDataDirectory(chunk.Slice(BoundImportTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the import address table (IMAGE_DIRECTORY_ENTRY_IAT).
        /// </remarks>
        public ImageDataDirectory ImportAddressTableDirectory => NumberOfRvaAndSizes >= 13 ? new ImageDataDirectory(chunk.Slice(ImportAddressTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the delay import table (IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT).
        /// </remarks>
        public ImageDataDirectory DelayImportTableDirectory => NumberOfRvaAndSizes >= 14 ? new ImageDataDirectory(chunk.Slice(DelayImportTableDirectoryOffset)) : default;

        /// <remarks>
        /// Gets information about the size and location of the COM descriptor table (IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR).
        /// </remarks>
        public ImageDataDirectory CorHeaderTableDirectory => NumberOfRvaAndSizes >= 15 ? new ImageDataDirectory(chunk.Slice(CorHeaderTableDirectoryOffset)) : default;

        public ImageDataDirectory NullDirectory => NumberOfRvaAndSizes >= 16 ? new ImageDataDirectory(chunk.Slice(NullDirectoryOffset)) : default;
        #endregion

        public int Offset => chunk.AbsoluteOffset;

        internal const int OffsetOfChecksum =
            sizeof(short) + // Magic
            sizeof(byte) +  // MajorLinkerVersion
            sizeof(byte) +  // MinorLinkerVersion
            sizeof(int) +   // SizeOfCode
            sizeof(int) +   // SizeOfInitializedData
            sizeof(int) +   // SizeOfUninitializedData
            sizeof(int) +   // AddressOfEntryPoint
            sizeof(int) +   // BaseOfCode
            sizeof(long) +  // PE32:  BaseOfData (int), ImageBase (int)
                            // PE32+: ImageBase (long)
            sizeof(int) +   // SectionAlignment
            sizeof(int) +   // FileAlignment
            sizeof(short) + // MajorOperatingSystemVersion
            sizeof(short) + // MinorOperatingSystemVersion
            sizeof(short) + // MajorImageVersion
            sizeof(short) + // MinorImageVersion
            sizeof(short) + // MajorSubsystemVersion
            sizeof(short) + // MinorSubsystemVersion
            sizeof(int) +   // Win32VersionValue
            sizeof(int) +   // SizeOfImage
            sizeof(int);    // SizeOfHeaders

        internal int StructSize(bool is32Bit) =>
            OffsetOfChecksum +
            sizeof(int) + // Checksum
            sizeof(short) + // Subsystem
            sizeof(short) + // DllCharacteristics
            4 * (is32Bit
                ? sizeof(int)
                : sizeof(long)) + // SizeOfStackReserve, SizeOfStackCommit, SizeOfHeapReserve, SizeOfHeapCommit
            sizeof(int) + // LoaderFlags
            sizeof(int) + // NumberOfRvaAndSizes
            NumberOfRvaAndSizes * sizeof(long); // directory entries

        private readonly MemoryChunk chunk;

        internal ImageOptionalHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        //We don't care about representing that there's a 32 and 64-bit versions of the structure
        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_OPTIONAL_HEADER, this, ViewKind.ImageOptionalHeader, StructSize(((PEViewWriter) writer).Is32Bit));

        int IViewable.NumChildren => 29 + (chunk.Is32Bit ? 1 : 0) + NumberOfRvaAndSizes;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            //We do not include BaseOfData in the switch below; we special case it, and update the index to pretend it's not there
            if (chunk.Is32Bit)
            {
                if (index == 8)
                {
                    //They want BaseOfData
                    structWriter.WriteField(nameof(BaseOfData), BaseOfDataOffset, BaseOfData);
                    return;
                }
                else if (index > 8)
                {
                    //Pretend BaseOfData is not there
                    index--;
                }
            }

            switch (index)
            {
                #region Standard Fields

                case 0:
                    structWriter.WriteField(nameof(Magic), MagicOffset, Magic, 2);
                    break;

                case 1:
                    structWriter.WriteField(nameof(MajorLinkerVersion), MajorLinkerVersionOffset, MajorLinkerVersion);
                    break;

                case 2:
                    structWriter.WriteField(nameof(MinorLinkerVersion), MinorLinkerVersionOffset, MinorLinkerVersion);
                    break;

                case 3:
                    structWriter.WriteField(nameof(SizeOfCode), SizeOfCodeOffset, SizeOfCode);
                    break;

                case 4:
                    structWriter.WriteField(nameof(SizeOfInitializedData), SizeOfInitializedDataOffset, SizeOfInitializedData);
                    break;

                case 5:
                    structWriter.WriteField(nameof(SizeOfUninitializedData), SizeOfUninitializedDataOffset, SizeOfUninitializedData);
                    break;

                case 6:
                    structWriter.WriteField(nameof(AddressOfEntryPoint), AddressOfEntryPointOffset, AddressOfEntryPoint);
                    break;

                case 7:
                    structWriter.WriteField(nameof(BaseOfCode), BaseOfCodeOffset, BaseOfCode);
                    break;

                //BaseOfData is special cased above

                #endregion
                #region Windows Specific Fields

                case 8:
                    structWriter.WritePointerField(nameof(ImageBase), ImageBaseOffset, ImageBase);
                    break;

                case 9:
                    structWriter.WriteField(nameof(SectionAlignment), SectionAlignmentOffset, SectionAlignment);
                    break;

                case 10:
                    structWriter.WriteField(nameof(FileAlignment), FileAlignmentOffset, FileAlignment);
                    break;

                case 11:
                    structWriter.WriteField(nameof(MajorOperatingSystemVersion), MajorOperatingSystemVersionOffset, MajorOperatingSystemVersion);
                    break;

                case 12:
                    structWriter.WriteField(nameof(MinorOperatingSystemVersion), MinorOperatingSystemVersionOffset, MinorOperatingSystemVersion);
                    break;

                case 13:
                    structWriter.WriteField(nameof(MajorImageVersion), MajorImageVersionOffset, MajorImageVersion);
                    break;

                case 14:
                    structWriter.WriteField(nameof(MinorImageVersion), MinorImageVersionOffset, MinorImageVersion);
                    break;

                case 15:
                    structWriter.WriteField(nameof(MajorSubsystemVersion), MajorSubsystemVersionOffset, MajorSubsystemVersion);
                    break;

                case 16:
                    structWriter.WriteField(nameof(MinorSubsystemVersion), MinorSubsystemVersionOffset, MinorSubsystemVersion);
                    break;

                case 17:
                    structWriter.WriteField(nameof(Win32VersionValue), Win32VersionValueOffset, Win32VersionValue);
                    break;

                case 18:
                    structWriter.WriteField(nameof(SizeOfImage), SizeOfImageOffset, SizeOfImage);
                    break;

                case 19:
                    structWriter.WriteField(nameof(SizeOfHeaders), SizeOfHeadersOffset, SizeOfHeaders);
                    break;

                case 20:
                    structWriter.WriteField(nameof(CheckSum), CheckSumOffset, CheckSum);
                    break;

                case 21:
                    structWriter.WriteField(nameof(Subsystem), SubsystemOffset, Subsystem, sizeof(short));
                    break;

                case 22:
                    structWriter.WriteField(nameof(DllCharacteristics), DllCharacteristicsOffset, DllCharacteristics, sizeof(short));
                    break;

                case 23:
                    structWriter.WritePointerField(nameof(SizeOfStackReserve), SizeOfStackReserveOffset, SizeOfStackReserve);
                    break;

                case 24:
                    structWriter.WritePointerField(nameof(SizeOfStackCommit), SizeOfStackCommitOffset, SizeOfStackCommit);
                    break;

                case 25:
                    structWriter.WritePointerField(nameof(SizeOfHeapReserve), SizeOfHeapReserveOffset, SizeOfHeapReserve);
                    break;

                case 26:
                    structWriter.WritePointerField(nameof(SizeOfHeapCommit), SizeOfHeapCommitOffset, SizeOfHeapCommit);
                    break;

                case 27:
                    structWriter.WriteField(nameof(LoaderFlags), LoaderFlagsOffset, LoaderFlags, sizeof(int));
                    break;

                case 28:
                    structWriter.WriteField(nameof(NumberOfRvaAndSizes), NumberOfRvaAndSizesOffset, NumberOfRvaAndSizes);
                    break;

                #endregion
                #region Directory Entries

                case 29:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT (0)]", ExportTableDirectoryOffset, ExportTableDirectory, 0, ref structWriter);
                    break;

                case 30:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT (1)]", ImportTableDirectoryOffset, ImportTableDirectory, 1, ref structWriter);
                    break;

                case 31:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE (2)]", ResourceTableDirectoryOffset, ResourceTableDirectory, 2, ref structWriter);
                    break;

                case 32:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_EXCEPTION (3)]", ExceptionTableDirectoryOffset, ExceptionTableDirectory, 3, ref structWriter);
                    break;

                case 33:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY (4)]", SecurityTableDirectoryOffset, SecurityTableDirectory, 4, ref structWriter);
                    break;

                case 34:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC (5)]", BaseRelocationTableDirectoryOffset, BaseRelocationTableDirectory, 5, ref structWriter);
                    break;

                case 35:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG (6)]", DebugTableDirectoryOffset, DebugTableDirectory, 6, ref structWriter);
                    break;

                case 36:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_COPYRIGHT / IMAGE_DIRECTORY_ENTRY_ARCHITECTURE (7)]", CopyrightTableDirectoryOffset, CopyrightTableDirectory, 7, ref structWriter);
                    break;

                case 37:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_GLOBALPTR (8)]", GlobalPointerTableDirectoryOffset, GlobalPointerTableDirectory, 8, ref structWriter);
                    break;

                case 38:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_TLS (9)]", ThreadLocalStorageTableDirectoryOffset, ThreadLocalStorageTableDirectory, 9, ref structWriter);
                    break;

                case 39:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG (10)]", LoadConfigTableDirectoryOffset, LoadConfigTableDirectory, 10, ref structWriter);
                    break;

                case 40:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT (11)]", BoundImportTableDirectoryOffset, BoundImportTableDirectory, 11, ref structWriter);
                    break;

                case 41:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_IAT (12)]", ImportAddressTableDirectoryOffset, ImportAddressTableDirectory, 12, ref structWriter);
                    break;

                case 42:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT (13)]", DelayImportTableDirectoryOffset, DelayImportTableDirectory, 13, ref structWriter);
                    break;

                case 43:
                    WriteDirectoryOrThrow("DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR (14)]", CorHeaderTableDirectoryOffset, CorHeaderTableDirectory, 14, ref structWriter);
                    break;

                case 44:
                    WriteDirectoryOrThrow(nameof(NullDirectory), NullDirectoryOffset, NullDirectory, 15, ref structWriter);
                    break;

                #endregion

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        private void WriteDirectoryOrThrow(string name, int relativeOffset, in ImageDataDirectory value, int directoryIndex, ref StructWriter structWriter)
        {
            if (directoryIndex < NumberOfRvaAndSizes)
                structWriter.WriteStructField(name, relativeOffset, value);
            else
                throw new IndexOutOfRangeException();
        }
    }
}
