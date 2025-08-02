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
        #region Standard Fields

        /// <summary>
        /// Identifies the format of the image file.
        /// </summary>
        public PEMagic Magic => (PEMagic) chunk.PeekUInt16(0);

        /// <summary>
        /// The linker major version number.
        /// </summary>
        public byte MajorLinkerVersion => chunk.PeekByte(2);

        /// <summary>
        /// The linker minor version number.
        /// </summary>
        public byte MinorLinkerVersion => chunk.PeekByte(3);

        /// <summary>
        /// The size of the code (text) section, or the sum of all code sections if there are multiple sections.
        /// </summary>
        public int SizeOfCode => chunk.PeekInt32(4);

        /// <summary>
        /// The size of the initialized data section, or the sum of all such sections if there are multiple data sections.
        /// </summary>
        public int SizeOfInitializedData => chunk.PeekInt32(8);

        /// <summary>
        /// The size of the uninitialized data section (BSS), or the sum of all such sections if there are multiple BSS sections.
        /// </summary>
        public int SizeOfUninitializedData => chunk.PeekInt32(12);

        /// <summary>
        /// The address of the entry point relative to the image base when the PE file is loaded into memory.
        /// For program images, this is the starting address. For device drivers, this is the address of the initialization function.
        /// An entry point is optional for DLLs. When no entry point is present, this field must be zero.
        /// </summary>
        public int AddressOfEntryPoint => chunk.PeekInt32(16);

        /// <summary>
        /// The address that is relative to the image base of the beginning-of-code section when it is loaded into memory.
        /// </summary>
        public int BaseOfCode => chunk.PeekInt32(20);

        /// <summary>
        /// The address that is relative to the image base of the beginning-of-data section when it is loaded into memory.
        /// </summary>
        public int BaseOfData => chunk.Is32Bit ? chunk.PeekInt32(24) : 0;

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
        public int SectionAlignment => chunk.PeekInt32(32);

        /// <summary>
        /// The alignment factor (in bytes) that is used to align the raw data of sections in the image file.
        /// The value should be a power of 2 between 512 and 64K, inclusive. The default is 512.
        /// If the <see cref="SectionAlignment"/> is less than the architecture's page size,
        /// then <see cref="FileAlignment"/> must match <see cref="SectionAlignment"/>.
        /// </summary>
        public int FileAlignment => chunk.PeekInt32(36);

        /// <summary>
        /// The major version number of the required operating system.
        /// </summary>
        public ushort MajorOperatingSystemVersion => chunk.PeekUInt16(40);

        /// <summary>
        /// The minor version number of the required operating system.
        /// </summary>
        public ushort MinorOperatingSystemVersion => chunk.PeekUInt16(42);

        /// <summary>
        /// The major version number of the image.
        /// </summary>
        public ushort MajorImageVersion => chunk.PeekUInt16(44);

        /// <summary>
        /// The minor version number of the image.
        /// </summary>
        public ushort MinorImageVersion => chunk.PeekUInt16(46);

        /// <summary>
        /// The major version number of the subsystem.
        /// </summary>
        public ushort MajorSubsystemVersion => chunk.PeekUInt16(48);

        /// <summary>
        /// The minor version number of the subsystem.
        /// </summary>
        public ushort MinorSubsystemVersion => chunk.PeekUInt16(50);

        /// <summary>
        /// This member is reserved and must be 0.
        /// </summary>
        public int Win32VersionValue => chunk.PeekInt32(52);

        /// <summary>
        /// The size (in bytes) of the image, including all headers, as the image is loaded in memory.
        /// It must be a multiple of <see cref="SectionAlignment"/>. This does not include overlay data, which is not loaded into memory.
        /// </summary>
        public int SizeOfImage => chunk.PeekInt32(56);

        /// <summary>
        /// The combined size of an MS DOS stub, PE header, and section headers rounded up to a multiple of FileAlignment.
        /// </summary>
        public int SizeOfHeaders => chunk.PeekInt32(60);

        /// <summary>
        /// The image file checksum.
        /// </summary>
        public uint CheckSum => chunk.PeekUInt32(64);

        /// <summary>
        /// The subsystem that is required to run this image.
        /// </summary>
        public ImageSubsystem Subsystem => (ImageSubsystem) chunk.PeekUInt16(68);

        /// <summary>
        /// The DLL characteristics of the image.
        /// </summary>
        public ImageDllCharacteristics DllCharacteristics => (ImageDllCharacteristics) chunk.PeekUInt16(70);

        /// <summary>
        /// The size of the stack to reserve. Only <see cref="SizeOfStackCommit"/> is committed;
        /// the rest is made available one page at a time until the reserve size is reached.
        /// </summary>
        public ulong SizeOfStackReserve => chunk.PeekPointer(72);

        /// <summary>
        /// The size of the stack to commit.
        /// </summary>
        public ulong SizeOfStackCommit => chunk.PeekPointer(72 + chunk.PointerSize);

        /// <summary>
        /// The size of the local heap space to reserve. Only <see cref="SizeOfHeapCommit"/> is committed;
        /// the rest is made available one page at a time until the reserve size is reached.
        /// </summary>
        public ulong SizeOfHeapReserve => chunk.PeekPointer(72 + (2 * chunk.PointerSize));

        /// <summary>
        /// The size of the local heap space to commit.
        /// </summary>
        public ulong SizeOfHeapCommit => chunk.PeekPointer(72 + (3 * chunk.PointerSize));

        /// <summary>
        /// This member is obsolete.
        /// </summary>
        public ImageLoaderFlags LoaderFlags => (ImageLoaderFlags) chunk.PeekUInt32(72 + (4 * chunk.PointerSize));

        /// <summary>
        /// The number of data-directory entries in the remainder of the <see cref="ImageOptionalHeader"/>. Each describes a location and size.
        /// </summary>
        public int NumberOfRvaAndSizes => chunk.PeekInt32(76 + (4 * chunk.PointerSize));

        #endregion
        #region Directory Entries

        /// <remarks>
        /// Gets information about the size and location of the export directory (IMAGE_DIRECTORY_ENTRY_EXPORT).
        /// </remarks>
        public ImageDataDirectory ExportTableDirectory => NumberOfRvaAndSizes >= 1 ? new ImageDataDirectory(chunk.Slice(80 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the import directory (IMAGE_DIRECTORY_ENTRY_IMPORT).
        /// </remarks>
        public ImageDataDirectory ImportTableDirectory => NumberOfRvaAndSizes >= 2 ? new ImageDataDirectory(chunk.Slice(88 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the resource directory (IMAGE_DIRECTORY_ENTRY_RESOURCE).
        /// </remarks>
        public ImageDataDirectory ResourceTableDirectory => NumberOfRvaAndSizes >= 3 ? new ImageDataDirectory(chunk.Slice(96 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the exception directory (IMAGE_DIRECTORY_ENTRY_EXCEPTION).
        /// </remarks>
        public ImageDataDirectory ExceptionTableDirectory => NumberOfRvaAndSizes >= 4 ? new ImageDataDirectory(chunk.Slice(104 + (4 * chunk.PointerSize))) : default;

        /// <summary>
        /// Gets information about the size and location of the security (certificate table) directory (IMAGE_DIRECTORY_ENTRY_SECURITY).<para/>
        /// The Certificate Table entry points to a table of attribute certificates.
        /// </summary>
        /// <remarks>
        /// These certificates are not loaded into memory as part of the image.
        /// As such, the first field of this entry, which is normally an RVA, is a file pointer instead.
        /// </remarks>
        public ImageDataDirectory SecurityTableDirectory => NumberOfRvaAndSizes >= 5 ? new ImageDataDirectory(chunk.Slice(112 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the base relocation table (IMAGE_DIRECTORY_ENTRY_BASERELOC).
        /// </remarks>
        public ImageDataDirectory BaseRelocationTableDirectory => NumberOfRvaAndSizes >= 6 ? new ImageDataDirectory(chunk.Slice(120 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the debug directory (IMAGE_DIRECTORY_ENTRY_DEBUG).
        /// </remarks>
        public ImageDataDirectory DebugTableDirectory => NumberOfRvaAndSizes >= 7 ? new ImageDataDirectory(chunk.Slice(128 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the architecture-specific data (IMAGE_DIRECTORY_ENTRY_COPYRIGHT or IMAGE_DIRECTORY_ENTRY_ARCHITECTURE).
        /// </remarks>
        public ImageDataDirectory CopyrightTableDirectory => NumberOfRvaAndSizes >= 8 ? new ImageDataDirectory(chunk.Slice(136 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the the relative virtual address of the global pointer (IMAGE_DIRECTORY_ENTRY_GLOBALPTR).
        /// </remarks>
        public ImageDataDirectory GlobalPointerTableDirectory => NumberOfRvaAndSizes >= 9 ? new ImageDataDirectory(chunk.Slice(144 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the thread local storage directory (IMAGE_DIRECTORY_ENTRY_TLS).
        /// </remarks>
        public ImageDataDirectory ThreadLocalStorageTableDirectory => NumberOfRvaAndSizes >= 10 ? new ImageDataDirectory(chunk.Slice(152 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the load configuration directory (IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG).
        /// </remarks>
        public ImageDataDirectory LoadConfigTableDirectory => NumberOfRvaAndSizes >= 11 ? new ImageDataDirectory(chunk.Slice(160 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the bound import directory (IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT).
        /// </remarks>
        public ImageDataDirectory BoundImportTableDirectory => NumberOfRvaAndSizes >= 12 ? new ImageDataDirectory(chunk.Slice(168 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the import address table (IMAGE_DIRECTORY_ENTRY_IAT).
        /// </remarks>
        public ImageDataDirectory ImportAddressTableDirectory => NumberOfRvaAndSizes >= 13 ? new ImageDataDirectory(chunk.Slice(176 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the delay import table (IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT).
        /// </remarks>
        public ImageDataDirectory DelayImportTableDirectory => NumberOfRvaAndSizes >= 14 ? new ImageDataDirectory(chunk.Slice(184 + (4 * chunk.PointerSize))) : default;

        /// <remarks>
        /// Gets information about the size and location of the COM descriptor table (IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR).
        /// </remarks>
        public ImageDataDirectory CorHeaderTableDirectory => NumberOfRvaAndSizes >= 15 ? new ImageDataDirectory(chunk.Slice(192 + (4 * chunk.PointerSize))) : default;

        public ImageDataDirectory NullDirectory => NumberOfRvaAndSizes >= 16 ? new ImageDataDirectory(chunk.Slice(200 + (4 * chunk.PointerSize))) : default;
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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            #region Standard Fields

            s.WriteField(nameof(Magic), Magic, 2);
            s.WriteField(nameof(MajorLinkerVersion), MajorLinkerVersion);
            s.WriteField(nameof(MinorLinkerVersion), MinorLinkerVersion);
            s.WriteField(nameof(SizeOfCode), SizeOfCode);
            s.WriteField(nameof(SizeOfInitializedData), SizeOfInitializedData);
            s.WriteField(nameof(SizeOfUninitializedData), SizeOfUninitializedData);
            s.WriteField(nameof(AddressOfEntryPoint), AddressOfEntryPoint);
            s.WriteField(nameof(BaseOfCode), BaseOfCode);

            if (chunk.Is32Bit)
                s.WriteField(nameof(BaseOfData), BaseOfData);

            #endregion
            #region Windows Specific Fields

            s.WritePointerField(nameof(ImageBase), ImageBase);
            s.WriteField(nameof(SectionAlignment), SectionAlignment);
            s.WriteField(nameof(FileAlignment), FileAlignment);
            s.WriteField(nameof(MajorOperatingSystemVersion), MajorOperatingSystemVersion);
            s.WriteField(nameof(MinorOperatingSystemVersion), MinorOperatingSystemVersion);
            s.WriteField(nameof(MajorImageVersion), MajorImageVersion);
            s.WriteField(nameof(MinorImageVersion), MinorImageVersion);
            s.WriteField(nameof(MajorSubsystemVersion), MajorSubsystemVersion);
            s.WriteField(nameof(MinorSubsystemVersion), MinorSubsystemVersion);
            s.WriteField(nameof(Win32VersionValue), Win32VersionValue);
            s.WriteField(nameof(SizeOfImage), SizeOfImage);
            s.WriteField(nameof(SizeOfHeaders), SizeOfHeaders);
            s.WriteField(nameof(CheckSum), CheckSum);
            s.WriteField(nameof(Subsystem), Subsystem, sizeof(short));
            s.WriteField(nameof(DllCharacteristics), DllCharacteristics, sizeof(short));
            s.WritePointerField(nameof(SizeOfStackReserve), SizeOfStackReserve);
            s.WritePointerField(nameof(SizeOfStackCommit), SizeOfStackCommit);
            s.WritePointerField(nameof(SizeOfHeapReserve), SizeOfHeapReserve);
            s.WritePointerField(nameof(SizeOfHeapCommit), SizeOfHeapCommit);
            s.WriteField(nameof(LoaderFlags), LoaderFlags, sizeof(int));
            s.WriteField(nameof(NumberOfRvaAndSizes), NumberOfRvaAndSizes);

            #endregion
            #region Directory Entries

            var numberOfRvaAndSizes = NumberOfRvaAndSizes;

            if (numberOfRvaAndSizes >= 1)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT (0)]", ExportTableDirectory);

            if (numberOfRvaAndSizes >= 2)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT (1)]", ImportTableDirectory);

            if (numberOfRvaAndSizes >= 3)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE (2)]", ResourceTableDirectory);

            if (numberOfRvaAndSizes >= 4)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_EXCEPTION (3)]", ExceptionTableDirectory);

            if (numberOfRvaAndSizes >= 5)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY (4)]", SecurityTableDirectory);

            if (numberOfRvaAndSizes >= 6)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC (5)]", BaseRelocationTableDirectory);

            if (numberOfRvaAndSizes >= 7)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG (6)]", DebugTableDirectory);

            if (numberOfRvaAndSizes >= 8)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_COPYRIGHT / IMAGE_DIRECTORY_ENTRY_ARCHITECTURE (7)]", CopyrightTableDirectory);

            if (numberOfRvaAndSizes >= 9)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_GLOBALPTR (8)]", GlobalPointerTableDirectory);

            if (numberOfRvaAndSizes >= 10)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_TLS (9)]", ThreadLocalStorageTableDirectory);

            if (numberOfRvaAndSizes >= 11)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG (10)]", LoadConfigTableDirectory);

            if (numberOfRvaAndSizes >= 12)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT (11)]", BoundImportTableDirectory);

            if (numberOfRvaAndSizes >= 13)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_IAT (12)]", ImportAddressTableDirectory);

            if (numberOfRvaAndSizes >= 14)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT (13)]", DelayImportTableDirectory);

            if (numberOfRvaAndSizes >= 15)
                s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR (14)]", CorHeaderTableDirectory);

            if (numberOfRvaAndSizes >= 16)
                s.WriteStructField(nameof(NullDirectory), NullDirectory);

            #endregion

            return s.ToArray();
        }
    }
}
