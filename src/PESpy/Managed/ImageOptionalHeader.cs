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
        public PEMagic Magic { get; init; }

        /// <summary>
        /// The linker major version number.
        /// </summary>
        public byte MajorLinkerVersion { get; init; }

        /// <summary>
        /// The linker minor version number.
        /// </summary>
        public byte MinorLinkerVersion { get; init; }

        /// <summary>
        /// The size of the code (text) section, or the sum of all code sections if there are multiple sections.
        /// </summary>
        public int SizeOfCode { get; init; }

        /// <summary>
        /// The size of the initialized data section, or the sum of all such sections if there are multiple data sections.
        /// </summary>
        public int SizeOfInitializedData { get; init; }

        /// <summary>
        /// The size of the uninitialized data section (BSS), or the sum of all such sections if there are multiple BSS sections.
        /// </summary>
        public int SizeOfUninitializedData { get; init; }

        /// <summary>
        /// The address of the entry point relative to the image base when the PE file is loaded into memory.
        /// For program images, this is the starting address. For device drivers, this is the address of the initialization function.
        /// An entry point is optional for DLLs. When no entry point is present, this field must be zero.
        /// </summary>
        public int AddressOfEntryPoint { get; init; }

        /// <summary>
        /// The address that is relative to the image base of the beginning-of-code section when it is loaded into memory.
        /// </summary>
        public int BaseOfCode { get; init; }

        /// <summary>
        /// The address that is relative to the image base of the beginning-of-data section when it is loaded into memory.
        /// </summary>
        public int BaseOfData { get; init; }

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
        public long ImageBase { get; init; }

        /// <summary>
        /// The alignment (in bytes) of sections when they are loaded into memory. It must be greater than or equal to <see cref="FileAlignment"/>.
        /// The default is the page size for the architecture.
        /// </summary>
        public int SectionAlignment { get; init; }

        /// <summary>
        /// The alignment factor (in bytes) that is used to align the raw data of sections in the image file.
        /// The value should be a power of 2 between 512 and 64K, inclusive. The default is 512.
        /// If the <see cref="SectionAlignment"/> is less than the architecture's page size,
        /// then <see cref="FileAlignment"/> must match <see cref="SectionAlignment"/>.
        /// </summary>
        public int FileAlignment { get; init; }

        /// <summary>
        /// The major version number of the required operating system.
        /// </summary>
        public ushort MajorOperatingSystemVersion { get; init; }

        /// <summary>
        /// The minor version number of the required operating system.
        /// </summary>
        public ushort MinorOperatingSystemVersion { get; init; }

        /// <summary>
        /// The major version number of the image.
        /// </summary>
        public ushort MajorImageVersion { get; init; }

        /// <summary>
        /// The minor version number of the image.
        /// </summary>
        public ushort MinorImageVersion { get; init; }

        /// <summary>
        /// The major version number of the subsystem.
        /// </summary>
        public ushort MajorSubsystemVersion { get; init; }

        /// <summary>
        /// The minor version number of the subsystem.
        /// </summary>
        public ushort MinorSubsystemVersion { get; init; }

        /// <summary>
        /// This member is reserved and must be 0.
        /// </summary>
        public int Win32VersionValue { get; init; }

        /// <summary>
        /// The size (in bytes) of the image, including all headers, as the image is loaded in memory.
        /// It must be a multiple of <see cref="SectionAlignment"/>. This does not include overlay data, which is not loaded into memory.
        /// </summary>
        public int SizeOfImage { get; init; }

        /// <summary>
        /// The combined size of an MS DOS stub, PE header, and section headers rounded up to a multiple of FileAlignment.
        /// </summary>
        public int SizeOfHeaders { get; init; }

        /// <summary>
        /// The image file checksum.
        /// </summary>
        public uint CheckSum { get; init; }

        /// <summary>
        /// The subsystem that is required to run this image.
        /// </summary>
        public ImageSubsystem Subsystem { get; init; }

        /// <summary>
        /// The DLL characteristics of the image.
        /// </summary>
        public ImageDllCharacteristics DllCharacteristics { get; init; }

        /// <summary>
        /// The size of the stack to reserve. Only <see cref="SizeOfStackCommit"/> is committed;
        /// the rest is made available one page at a time until the reserve size is reached.
        /// </summary>
        public ulong SizeOfStackReserve { get; init; }

        /// <summary>
        /// The size of the stack to commit.
        /// </summary>
        public ulong SizeOfStackCommit { get; init; }

        /// <summary>
        /// The size of the local heap space to reserve. Only <see cref="SizeOfHeapCommit"/> is committed;
        /// the rest is made available one page at a time until the reserve size is reached.
        /// </summary>
        public ulong SizeOfHeapReserve { get; init; }

        /// <summary>
        /// The size of the local heap space to commit.
        /// </summary>
        public ulong SizeOfHeapCommit { get; init; }

        /// <summary>
        /// This member is obsolete.
        /// </summary>
        public ImageLoaderFlags LoaderFlags { get; init; }

        /// <summary>
        /// The number of data-directory entries in the remainder of the <see cref="ImageOptionalHeader"/>. Each describes a location and size.
        /// </summary>
        public int NumberOfRvaAndSizes { get; init; }

        #endregion
        #region Directory Entries

        /// <remarks>
        /// Gets information about the size and location of the export directory (IMAGE_DIRECTORY_ENTRY_EXPORT).
        /// </remarks>
        public ImageDataDirectory ExportTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the import directory (IMAGE_DIRECTORY_ENTRY_IMPORT).
        /// </remarks>
        public ImageDataDirectory ImportTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the resource directory (IMAGE_DIRECTORY_ENTRY_RESOURCE).
        /// </remarks>
        public ImageDataDirectory ResourceTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the exception directory (IMAGE_DIRECTORY_ENTRY_EXCEPTION).
        /// </remarks>
        public ImageDataDirectory ExceptionTableDirectory { get; init; }

        /// <summary>
        /// Gets information about the size and location of the security (certificate table) directory (IMAGE_DIRECTORY_ENTRY_SECURITY).<para/>
        /// The Certificate Table entry points to a table of attribute certificates.
        /// </summary>
        /// <remarks>
        /// These certificates are not loaded into memory as part of the image.
        /// As such, the first field of this entry, which is normally an RVA, is a file pointer instead.
        /// </remarks>
        public ImageDataDirectory SecurityTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the base relocation table (IMAGE_DIRECTORY_ENTRY_BASERELOC).
        /// </remarks>
        public ImageDataDirectory BaseRelocationTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the debug directory (IMAGE_DIRECTORY_ENTRY_DEBUG).
        /// </remarks>
        public ImageDataDirectory DebugTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the architecture-specific data (IMAGE_DIRECTORY_ENTRY_COPYRIGHT or IMAGE_DIRECTORY_ENTRY_ARCHITECTURE).
        /// </remarks>
        public ImageDataDirectory CopyrightTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the the relative virtual address of the global pointer (IMAGE_DIRECTORY_ENTRY_GLOBALPTR).
        /// </remarks>
        public ImageDataDirectory GlobalPointerTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the thread local storage directory (IMAGE_DIRECTORY_ENTRY_TLS).
        /// </remarks>
        public ImageDataDirectory ThreadLocalStorageTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the load configuration directory (IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG).
        /// </remarks>
        public ImageDataDirectory LoadConfigTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the bound import directory (IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT).
        /// </remarks>
        public ImageDataDirectory BoundImportTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the import address table (IMAGE_DIRECTORY_ENTRY_IAT).
        /// </remarks>
        public ImageDataDirectory ImportAddressTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the delay import table (IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT).
        /// </remarks>
        public ImageDataDirectory DelayImportTableDirectory { get; init; }

        /// <remarks>
        /// Gets information about the size and location of the COM descriptor table (IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR).
        /// </remarks>
        public ImageDataDirectory CorHeaderTableDirectory { get; init; }

        public ImageDataDirectory NullDirectory { get; init; } //Not sure what the name is

        #endregion

        public RawOffset Offset { get; }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageOptionalHeader"/>.
        /// </summary>
        public ImageOptionalHeader()
        {
        }

        internal ImageOptionalHeader(ref FileReader reader)
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
            ExportTableDirectory = new ImageDataDirectory(ref reader);
            ImportTableDirectory = new ImageDataDirectory(ref reader);
            ResourceTableDirectory = new ImageDataDirectory(ref reader);
            ExceptionTableDirectory = new ImageDataDirectory(ref reader);
            SecurityTableDirectory = new ImageDataDirectory(ref reader);
            BaseRelocationTableDirectory = new ImageDataDirectory(ref reader);
            DebugTableDirectory = new ImageDataDirectory(ref reader);
            CopyrightTableDirectory = new ImageDataDirectory(ref reader);
            GlobalPointerTableDirectory = new ImageDataDirectory(ref reader);
            ThreadLocalStorageTableDirectory = new ImageDataDirectory(ref reader);
            LoadConfigTableDirectory = new ImageDataDirectory(ref reader);
            BoundImportTableDirectory = new ImageDataDirectory(ref reader);
            ImportAddressTableDirectory = new ImageDataDirectory(ref reader);
            DelayImportTableDirectory = new ImageDataDirectory(ref reader);
            CorHeaderTableDirectory = new ImageDataDirectory(ref reader);

            // ReservedDirectory (should be 0, 0)
            NullDirectory = new ImageDataDirectory(ref reader);
        }

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
