using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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
#if PEFAST
        public PEMagic Magic => (PEMagic) chunk.PeekUInt16(0);
#else
        public PEMagic Magic { get; init; }
#endif

        /// <summary>
        /// The linker major version number.
        /// </summary>
#if PEFAST
        public byte MajorLinkerVersion => chunk.PeekByte(2);
#else
        public byte MajorLinkerVersion { get; init; }
#endif

        /// <summary>
        /// The linker minor version number.
        /// </summary>
#if PEFAST
        public byte MinorLinkerVersion => chunk.PeekByte(3);
#else
        public byte MinorLinkerVersion { get; init; }
#endif

        /// <summary>
        /// The size of the code (text) section, or the sum of all code sections if there are multiple sections.
        /// </summary>
#if PEFAST
        public int SizeOfCode => chunk.PeekInt32(4);
#else
        public int SizeOfCode { get; init; }
#endif

        /// <summary>
        /// The size of the initialized data section, or the sum of all such sections if there are multiple data sections.
        /// </summary>
#if PEFAST
        public int SizeOfInitializedData => chunk.PeekInt32(8);
#else
        public int SizeOfInitializedData { get; init; }
#endif

        /// <summary>
        /// The size of the uninitialized data section (BSS), or the sum of all such sections if there are multiple BSS sections.
        /// </summary>
#if PEFAST
        public int SizeOfUninitializedData => chunk.PeekInt32(12);
#else
        public int SizeOfUninitializedData { get; init; }
#endif

        /// <summary>
        /// The address of the entry point relative to the image base when the PE file is loaded into memory.
        /// For program images, this is the starting address. For device drivers, this is the address of the initialization function.
        /// An entry point is optional for DLLs. When no entry point is present, this field must be zero.
        /// </summary>
#if PEFAST
        public int AddressOfEntryPoint => chunk.PeekInt32(16);
#else
        public int AddressOfEntryPoint { get; init; }
#endif

        /// <summary>
        /// The address that is relative to the image base of the beginning-of-code section when it is loaded into memory.
        /// </summary>
#if PEFAST
        public int BaseOfCode => chunk.PeekInt32(20);
#else
        public int BaseOfCode { get; init; }
#endif

        /// <summary>
        /// The address that is relative to the image base of the beginning-of-data section when it is loaded into memory.
        /// </summary>
#if PEFAST
        public int BaseOfData => chunk.Is32Bit ? chunk.PeekInt32(24) : 0;
#else
        public int BaseOfData { get; init; }
#endif

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
#if PEFAST
        public long ImageBase => chunk.Is32Bit ? (long) chunk.PeekPointer(28) : (long) chunk.PeekPointer(24);
#else
        public long ImageBase { get; init; }
#endif

        /// <summary>
        /// The alignment (in bytes) of sections when they are loaded into memory. It must be greater than or equal to <see cref="FileAlignment"/>.
        /// The default is the page size for the architecture.
        /// </summary>
#if PEFAST
        public int SectionAlignment => chunk.PeekInt32(24 + chunk.PointerSize);
#else
        public int SectionAlignment { get; init; }
#endif

        /// <summary>
        /// The alignment factor (in bytes) that is used to align the raw data of sections in the image file.
        /// The value should be a power of 2 between 512 and 64K, inclusive. The default is 512.
        /// If the <see cref="SectionAlignment"/> is less than the architecture's page size,
        /// then <see cref="FileAlignment"/> must match <see cref="SectionAlignment"/>.
        /// </summary>
#if PEFAST
        public int FileAlignment => chunk.PeekInt32(28 + chunk.PointerSize);
#else
        public int FileAlignment { get; init; }
#endif

        /// <summary>
        /// The major version number of the required operating system.
        /// </summary>
#if PEFAST
        public ushort MajorOperatingSystemVersion => chunk.PeekUInt16(32 + chunk.PointerSize);
#else
        public ushort MajorOperatingSystemVersion { get; init; }
#endif

        /// <summary>
        /// The minor version number of the required operating system.
        /// </summary>
#if PEFAST
        public ushort MinorOperatingSystemVersion => chunk.PeekUInt16(34 + chunk.PointerSize);
#else
        public ushort MinorOperatingSystemVersion { get; init; }
#endif

        /// <summary>
        /// The major version number of the image.
        /// </summary>
#if PEFAST
        public ushort MajorImageVersion => chunk.PeekUInt16(36 + chunk.PointerSize);
#else
        public ushort MajorImageVersion { get; init; }
#endif

        /// <summary>
        /// The minor version number of the image.
        /// </summary>
#if PEFAST
        public ushort MinorImageVersion => chunk.PeekUInt16(38 + chunk.PointerSize);
#else
        public ushort MinorImageVersion { get; init; }
#endif

        /// <summary>
        /// The major version number of the subsystem.
        /// </summary>
#if PEFAST
        public ushort MajorSubsystemVersion => chunk.PeekUInt16(40 + chunk.PointerSize);
#else
        public ushort MajorSubsystemVersion { get; init; }
#endif

        /// <summary>
        /// The minor version number of the subsystem.
        /// </summary>
#if PEFAST
        public ushort MinorSubsystemVersion => chunk.PeekUInt16(42 + chunk.PointerSize);
#else
        public ushort MinorSubsystemVersion { get; init; }
#endif

        /// <summary>
        /// This member is reserved and must be 0.
        /// </summary>
#if PEFAST
        public int Win32VersionValue => chunk.PeekInt32(44 + chunk.PointerSize);
#else
        public int Win32VersionValue { get; init; }
#endif

        /// <summary>
        /// The size (in bytes) of the image, including all headers, as the image is loaded in memory.
        /// It must be a multiple of <see cref="SectionAlignment"/>. This does not include overlay data, which is not loaded into memory.
        /// </summary>
#if PEFAST
        public int SizeOfImage => chunk.PeekInt32(48 + chunk.PointerSize);
#else
        public int SizeOfImage { get; init; }
#endif

        /// <summary>
        /// The combined size of an MS DOS stub, PE header, and section headers rounded up to a multiple of FileAlignment.
        /// </summary>
#if PEFAST
        public int SizeOfHeaders => chunk.PeekInt32(52 + chunk.PointerSize);
#else
        public int SizeOfHeaders { get; init; }
#endif

        /// <summary>
        /// The image file checksum.
        /// </summary>
#if PEFAST
        public uint CheckSum => chunk.PeekUInt32(56 + chunk.PointerSize);
#else
        public uint CheckSum { get; init; }
#endif

        /// <summary>
        /// The subsystem that is required to run this image.
        /// </summary>
#if PEFAST
        public ImageSubsystem Subsystem => (ImageSubsystem) chunk.PeekUInt16(60 + chunk.PointerSize);
#else
        public ImageSubsystem Subsystem { get; init; }
#endif

        /// <summary>
        /// The DLL characteristics of the image.
        /// </summary>
#if PEFAST
        public ImageDllCharacteristics DllCharacteristics => (ImageDllCharacteristics) chunk.PeekUInt16(62 + chunk.PointerSize);
#else
        public ImageDllCharacteristics DllCharacteristics { get; init; }
#endif

        /// <summary>
        /// The size of the stack to reserve. Only <see cref="SizeOfStackCommit"/> is committed;
        /// the rest is made available one page at a time until the reserve size is reached.
        /// </summary>
#if PEFAST
        public ulong SizeOfStackReserve => chunk.PeekPointer(64 + chunk.PointerSize);
#else
        public ulong SizeOfStackReserve { get; init; }
#endif

        /// <summary>
        /// The size of the stack to commit.
        /// </summary>
#if PEFAST
        public ulong SizeOfStackCommit => chunk.PeekPointer(64 + (2 * chunk.PointerSize));
#else
        public ulong SizeOfStackCommit { get; init; }
#endif

        /// <summary>
        /// The size of the local heap space to reserve. Only <see cref="SizeOfHeapCommit"/> is committed;
        /// the rest is made available one page at a time until the reserve size is reached.
        /// </summary>
#if PEFAST
        public ulong SizeOfHeapReserve => chunk.PeekPointer(64 + (3 * chunk.PointerSize));
#else
        public ulong SizeOfHeapReserve { get; init; }
#endif

        /// <summary>
        /// The size of the local heap space to commit.
        /// </summary>
#if PEFAST
        public ulong SizeOfHeapCommit => chunk.PeekPointer(64 + (4 * chunk.PointerSize));
#else
        public ulong SizeOfHeapCommit { get; init; }
#endif

        /// <summary>
        /// This member is obsolete.
        /// </summary>
#if PEFAST
        public ImageLoaderFlags LoaderFlags => (ImageLoaderFlags) chunk.PeekUInt32(64 + (5 * chunk.PointerSize));
#else
        public ImageLoaderFlags LoaderFlags { get; init; }
#endif

        /// <summary>
        /// The number of data-directory entries in the remainder of the <see cref="ImageOptionalHeader"/>. Each describes a location and size.
        /// </summary>
#if PEFAST
        public int NumberOfRvaAndSizes => chunk.PeekInt32(68 + (5 * chunk.PointerSize));
#else
        public int NumberOfRvaAndSizes { get; init; }
#endif

        #endregion
        #region Directory Entries

        /// <remarks>
        /// Gets information about the size and location of the export directory (IMAGE_DIRECTORY_ENTRY_EXPORT).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory ExportTableDirectory => new ImageDataDirectory(chunk.Slice(72 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory ExportTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the import directory (IMAGE_DIRECTORY_ENTRY_IMPORT).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory ImportTableDirectory => new ImageDataDirectory(chunk.Slice(80 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory ImportTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the resource directory (IMAGE_DIRECTORY_ENTRY_RESOURCE).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory ResourceTableDirectory => new ImageDataDirectory(chunk.Slice(88 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory ResourceTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the exception directory (IMAGE_DIRECTORY_ENTRY_EXCEPTION).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory ExceptionTableDirectory => new ImageDataDirectory(chunk.Slice(96 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory ExceptionTableDirectory { get; init; }
#endif

        /// <summary>
        /// Gets information about the size and location of the security (certificate table) directory (IMAGE_DIRECTORY_ENTRY_SECURITY).<para/>
        /// The Certificate Table entry points to a table of attribute certificates.
        /// </summary>
        /// <remarks>
        /// These certificates are not loaded into memory as part of the image.
        /// As such, the first field of this entry, which is normally an RVA, is a file pointer instead.
        /// </remarks>
#if PEFAST
        public ImageDataDirectory SecurityTableDirectory => new ImageDataDirectory(chunk.Slice(104 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory SecurityTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the base relocation table (IMAGE_DIRECTORY_ENTRY_BASERELOC).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory BaseRelocationTableDirectory => new ImageDataDirectory(chunk.Slice(112 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory BaseRelocationTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the debug directory (IMAGE_DIRECTORY_ENTRY_DEBUG).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory DebugTableDirectory => new ImageDataDirectory(chunk.Slice(120 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory DebugTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the architecture-specific data (IMAGE_DIRECTORY_ENTRY_COPYRIGHT or IMAGE_DIRECTORY_ENTRY_ARCHITECTURE).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory CopyrightTableDirectory => new ImageDataDirectory(chunk.Slice(128 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory CopyrightTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the the relative virtual address of the global pointer (IMAGE_DIRECTORY_ENTRY_GLOBALPTR).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory GlobalPointerTableDirectory => new ImageDataDirectory(chunk.Slice(136 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory GlobalPointerTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the thread local storage directory (IMAGE_DIRECTORY_ENTRY_TLS).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory ThreadLocalStorageTableDirectory => new ImageDataDirectory(chunk.Slice(144 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory ThreadLocalStorageTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the load configuration directory (IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory LoadConfigTableDirectory => new ImageDataDirectory(chunk.Slice(152 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory LoadConfigTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the bound import directory (IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory BoundImportTableDirectory => new ImageDataDirectory(chunk.Slice(160 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory BoundImportTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the import address table (IMAGE_DIRECTORY_ENTRY_IAT).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory ImportAddressTableDirectory => new ImageDataDirectory(chunk.Slice(168 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory ImportAddressTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the delay import table (IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory DelayImportTableDirectory => new ImageDataDirectory(chunk.Slice(176 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory DelayImportTableDirectory { get; init; }
#endif

        /// <remarks>
        /// Gets information about the size and location of the COM descriptor table (IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR).
        /// </remarks>
#if PEFAST
        public ImageDataDirectory CorHeaderTableDirectory => new ImageDataDirectory(chunk.Slice(184 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory CorHeaderTableDirectory { get; init; }
#endif

#if PEFAST
        public ImageDataDirectory NullDirectory => new ImageDataDirectory(chunk.Slice(192 + (5 * chunk.PointerSize)));
#else
        public ImageDataDirectory NullDirectory { get; init; } //Not sure what the name is
#endif
        #endregion

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

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

        internal static int StructSize(bool is32Bit) =>
            OffsetOfChecksum +
            sizeof(int) + // Checksum
            sizeof(short) + // Subsystem
            sizeof(short) + // DllCharacteristics
            4 * (is32Bit
                ? sizeof(int)
                : sizeof(long)) + // SizeOfStackReserve, SizeOfStackCommit, SizeOfHeapReserve, SizeOfHeapCommit
            sizeof(int) + // LoaderFlags
            sizeof(int) + // NumberOfRvaAndSizes
            16 * sizeof(long); // directory entries

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageOptionalHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ImageOptionalHeader(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            PEMagic magic = (PEMagic) reader.ReadUInt16();

            if (magic == PEMagic.ROM)
                throw new NotImplementedException($"Handling {nameof(PEMagic)}.{magic} is not implemented");

            if (magic != PEMagic.PE32 && magic != PEMagic.PE32Plus)
                throw new BadImageFormatException("Unknown PE Magic value.");

            //We already have the magic, so we can exclude them from the amount we read in
            reader.FillBuffer(StructSize(magic == PEMagic.PE32) - sizeof(short));

            Magic = magic;
            MajorLinkerVersion = reader.ReadByte();
            MinorLinkerVersion = reader.ReadByte();
            SizeOfCode = reader.ReadInt32();
            SizeOfInitializedData = reader.ReadInt32();
            SizeOfUninitializedData = reader.ReadInt32();
            AddressOfEntryPoint = reader.ReadInt32();
            BaseOfCode = reader.ReadInt32();

            if (magic == PEMagic.PE32Plus)
            {
                BaseOfData = 0; // not present
            }
            else
            {
                Debug.Assert(magic == PEMagic.PE32);
                BaseOfData = reader.ReadInt32();
            }

#pragma warning disable RS0030
            ImageBase = magic == PEMagic.PE32Plus
#pragma warning restore RS0030
                ? reader.ReadInt64()
                : reader.ReadInt32();

            // NT additional fields:
            SectionAlignment = reader.ReadInt32();
            FileAlignment = reader.ReadInt32();
            MajorOperatingSystemVersion = reader.ReadUInt16();
            MinorOperatingSystemVersion = reader.ReadUInt16();
            MajorImageVersion = reader.ReadUInt16();
            MinorImageVersion = reader.ReadUInt16();
            MajorSubsystemVersion = reader.ReadUInt16();
            MinorSubsystemVersion = reader.ReadUInt16();

            // Win32VersionValue (reserved, should be 0)
            Win32VersionValue = reader.ReadInt32();

            SizeOfImage = reader.ReadInt32();
            SizeOfHeaders = reader.ReadInt32();
            CheckSum = reader.ReadUInt32();
            Subsystem = (ImageSubsystem) reader.ReadUInt16();
            DllCharacteristics = (ImageDllCharacteristics) reader.ReadUInt16();

            if (magic == PEMagic.PE32Plus)
            {
                SizeOfStackReserve = reader.ReadUInt64();
                SizeOfStackCommit = reader.ReadUInt64();
                SizeOfHeapReserve = reader.ReadUInt64();
                SizeOfHeapCommit = reader.ReadUInt64();
            }
            else
            {
                SizeOfStackReserve = reader.ReadUInt32();
                SizeOfStackCommit = reader.ReadUInt32();
                SizeOfHeapReserve = reader.ReadUInt32();
                SizeOfHeapCommit = reader.ReadUInt32();
            }

            LoaderFlags = (ImageLoaderFlags) reader.ReadUInt32();

            NumberOfRvaAndSizes = reader.ReadInt32();

            Debug.Assert(NumberOfRvaAndSizes == 16);

            //Directory entries
            ExportTableDirectory = new ImageDataDirectory(reader);
            ImportTableDirectory = new ImageDataDirectory(reader);
            ResourceTableDirectory = new ImageDataDirectory(reader);
            ExceptionTableDirectory = new ImageDataDirectory(reader);
            SecurityTableDirectory = new ImageDataDirectory(reader);
            BaseRelocationTableDirectory = new ImageDataDirectory(reader);
            DebugTableDirectory = new ImageDataDirectory(reader);
            CopyrightTableDirectory = new ImageDataDirectory(reader);
            GlobalPointerTableDirectory = new ImageDataDirectory(reader);
            ThreadLocalStorageTableDirectory = new ImageDataDirectory(reader);
            LoadConfigTableDirectory = new ImageDataDirectory(reader);
            BoundImportTableDirectory = new ImageDataDirectory(reader);
            ImportAddressTableDirectory = new ImageDataDirectory(reader);
            DelayImportTableDirectory = new ImageDataDirectory(reader);
            CorHeaderTableDirectory = new ImageDataDirectory(reader);

            // ReservedDirectory (should be 0, 0)
            NullDirectory = new ImageDataDirectory(reader);
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            //We don't care about representing that there's a 32 and 64-bit versions of the structure
            using var s = writer.CreateStruct("IMAGE_OPTIONAL_HEADER", this, ViewKind.ImageOptionalHeader);

            #region Standard Fields

            s.WriteField(nameof(Magic), Magic, 2);
            s.WriteField(nameof(MajorLinkerVersion), MajorLinkerVersion);
            s.WriteField(nameof(MinorLinkerVersion), MinorLinkerVersion);
            s.WriteField(nameof(SizeOfCode), SizeOfCode);
            s.WriteField(nameof(SizeOfInitializedData), SizeOfInitializedData);
            s.WriteField(nameof(SizeOfUninitializedData), SizeOfUninitializedData);
            s.WriteField(nameof(AddressOfEntryPoint), AddressOfEntryPoint);
            s.WriteField(nameof(BaseOfCode), BaseOfCode);

            if (((PEViewWriter) writer).Is32Bit)
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

            Debug.Assert(NumberOfRvaAndSizes == 16);

            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT (0)]", ExportTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT (1)]", ImportTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE (2)]", ResourceTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_EXCEPTION (3)]", ExceptionTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_SECURITY (4)]", SecurityTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC (5)]", BaseRelocationTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG (6)]", DebugTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_COPYRIGHT / IMAGE_DIRECTORY_ENTRY_ARCHITECTURE (7)]", CopyrightTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_GLOBALPTR (8)]", GlobalPointerTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_TLS (9)]", ThreadLocalStorageTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG (10)]", LoadConfigTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT (11)]", BoundImportTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_IAT (12)]", ImportAddressTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT (13)]", DelayImportTableDirectory);
            s.WriteStructField("DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR (14)]", CorHeaderTableDirectory);
            s.WriteStructField(nameof(NullDirectory), NullDirectory);

            #endregion
        }
    }
}
