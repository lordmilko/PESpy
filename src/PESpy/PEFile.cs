using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ClrDebug;
using PESpy.Native;
using PESpy.View;
using PESpy.View.Builder;
#if !DEBUG_POSITION
using RVA = System.Int32;
using RawOffset = System.Int32;
#endif

    class PEFileDebugView
    {
        private PEFile peFile;

        public PEFileDebugView(PEFile peFile)
        {
            this.peFile = peFile;
        }

        public bool IsLoadedImage => peFile.IsLoadedImage;
        public ImageDosHeader DosHeader => peFile.DosHeader;
        public ByteBlob DosStub => peFile.DosStub;
        public RichHeader? RichHeader => peFile.RichHeader;
        public ImageNtHeaders NtHeaders => peFile.NtHeaders;
        public ImageFileHeader FileHeader => peFile.FileHeader;
        public ImageOptionalHeader OptionalHeader => peFile.OptionalHeader;
        public ImageSectionHeader[]? SectionHeaders => peFile.SectionHeaders;

        #region Directories

        public ImageExportDirectory? ExportTable => peFile.ExportTable;
        public ImageImportDescriptor[]? ImportTable => peFile.ImportTable;
        public ImageResourceDirectory? ResourceDirectory => peFile.ResourceDirectory;
        public RuntimeFunction[]? ExceptionTable => peFile.ExceptionTable;
        public WinCertificate[]? SecurityTable => peFile.SecurityTable;
        public ImageBaseRelocation[]? BaseRelocationTable => peFile.BaseRelocationTable;
        public ImageDebugDirectory[]? DebugTable => peFile.DebugTable;
        //Copyright Table
        //Global Pointer Table
        public ImageTlsDirectory? TlsDirectory => peFile.TlsDirectory;
        public ImageLoadConfigDirectory? LoadConfigTable => peFile.LoadConfigTable;
        public ImageBoundImportDescriptor[]? BoundImportTable => peFile.BoundImportTable;
        public ImageThunkData[]? ImportAddressTable => peFile.ImportAddressTable;
        public ImageDelayLoadDescriptor[]? DelayImportTable => peFile.DelayImportTable;
        public ImageCor20Header? Cor20Header => peFile.Cor20Header;

        /// <summary>
        /// Gets the MS-DOS 2.0 compatible <see cref="IMAGE_DOS_HEADER"/>.
        /// </summary>
        public ref readonly ImageDosHeader DosHeader
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.DosHeader))
                {
                    //We only lock using the reader; if another thread tries to read as well, it's not that big a deal
                    lock (readerLock)
                    {
                        reader.Seek(0);
                        dosHeader = new ImageDosHeader(ref reader);
                    }

                    SetRegionFlag(PERegionKind.DosHeader);
                }

                return ref dosHeader;
            }
        }

        #endregion
        #region DosStub

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ByteBlob dosStub;

        public ref readonly ByteBlob DosStub
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.DosStub))
                {
                    lock (readerLock)
                    {
                        /* Ostensibly, the DOS Stub should live between bytes 0x40 and 0x7F (inclusive).
                         * The RichHeader (if present) begins immediately after the DOS Stub. Technically
                         * speaking its possible to have a custom DOS Stub, so we can't just assume that the stub
                         * will live exactly where we think it will be. In addition, we want to draw attention
                         * to the case when the stub is non-standard. As such, we use the following logic:
                         * - If the RichHeader was present, we know that the stub lives between the end of
                         *   the DOS Header and the start of the RichHeader
                         * - If the RichHeader was not present, we read all bytes between the end of the DOS Header
                         *   and the start of the PE Header.
                         *
                         * The DOS stub is not guaranteed to always be the same. Certain compilers can cause minor differences in the order of the DOS instructions that are executed, as well as
                         * include padding between the stub and the error message, which can also vary ("This program cannot be run in DOS mode", "This program must be run under Win32" */

                        var start = (RawOffset) ImageDosHeader.StructSize;
                        RawOffset end;

                        if (RichHeader != null)
                            end = RichHeader.Offset;
                        else
                        {
                            //We know there isn't a RichHeader. Read up until the start of the new PE Header
                            end = DosHeader.FileAddressOfNewExeHeader;
                        }

                        var length = (int)(end - start);

                        reader.Seek(start);

                        reader.FillBuffer(length);
                        var bytes = reader.ReadBytes(length);

                        dosStub = new ByteBlob(start, bytes);
                    }

                    SetRegionFlag(PERegionKind.DosStub);
                }

                return ref dosStub;
            }
        }

        #endregion
        #region RichHeader

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RichHeader? richHeader;

        /// <summary>
        /// Gets the undocumented Rich Header which describes the build environment that was used to create the file.<para/>
        /// If the file does not have a Rich Header, this property returns <see langword="null"/>.
        /// </summary>
        public RichHeader? RichHeader
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.RichHeader))
                {
                    lock (readerLock)
                    {
                        reader.Seek(ImageDosHeader.StructSize);
                        richHeader = RichHeader.New(DosHeader.FileAddressOfNewExeHeader, ref reader);
                    }

                    SetRegionFlag(PERegionKind.RichHeader);
                }

                return richHeader;
            }
        }

        #endregion
        #region NtHeaders

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageNtHeaders ntHeaders;

        public ref readonly ImageNtHeaders NtHeaders
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.NtHeaders))
                {
                    lock (readerLock)
                    {
                        reader.Seek(DosHeader.FileAddressOfNewExeHeader);
                        ntHeaders = new ImageNtHeaders(ref reader);
                    }

                    SetRegionFlag(PERegionKind.NtHeaders);
                }

                return ref ntHeaders;
            }
        }

        /// <summary>
        /// Gets the <see cref="IMAGE_NT_HEADERS.FileHeader"/> field that represents the file header of the image.
        /// </summary>
        public ImageFileHeader FileHeader => NtHeaders.FileHeader; //Cannot be ref readonly

        /// <summary>
        /// Gets the the <see cref="IMAGE_NT_HEADERS.OptionalHeader"/> field that represents the optional header of the image.
        /// </summary>
        public ImageOptionalHeader OptionalHeader => NtHeaders.OptionalHeader; //Cannot be ref readonly

        #endregion
        #region SectionHeaders

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageSectionHeader[]? sectionHeaders;

        /// <summary>
        /// Gets the section headers of the image. These values represent the <see cref="IMAGE_SECTION_HEADER"/> values (e.g. .text, .data) that immediately follow the <see cref="OptionalHeader"/>.<para/>
        /// Each section header points to a relative location within the image at which that section actually resides.
        /// </summary>
        public ImageSectionHeader[] SectionHeaders
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.SectionHeaders))
                {
                    lock (readerLock)
                    {
                        if (FileHeader.NumberOfSections < 0)
                            throw new BadImageFormatException("Invalid number of sections declared in PE header.");

                        var ntHeadersSize = ImageNtHeaders.StructSize(NtHeaders.OptionalHeader.Magic == PEMagic.PE32);
                        reader.Seek(DosHeader.FileAddressOfNewExeHeader + ntHeadersSize);

                        var list = new ImageSectionHeader[FileHeader.NumberOfSections];

                        for (var i = 0; i < FileHeader.NumberOfSections; i++)
                            list[i] = new ImageSectionHeader(ref reader);

                        sectionHeaders = list;
                    }

                    SetRegionFlag(PERegionKind.SectionHeaders);
                }

                return sectionHeaders!;
            }
        }

        #endregion
        #region Directories
        #region Export Table (0)

        private ImageExportDirectory? exportTable;

        public ImageExportDirectory? ExportTable
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.ExportTable))
                {
                    RawOffset offset;

                    if (TryGetDirectoryOffset(OptionalHeader.ExportTableDirectory, out offset, true))
                    {
                        lock (readerLock)
                        {
                            reader.Seek(offset);
                            exportTable = new ImageExportDirectory(ref reader, this);
                        }
                    }

                    SetRegionFlag(PERegionKind.ExportTable);
                }

                return exportTable;
            }
        }

        #endregion
        #region Import Table (1)

        private ImageImportDescriptor[]? importTable;

        public ImageImportDescriptor[]? ImportTable
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.ImportTable))
                {
                    if (TryGetDirectoryOffset(OptionalHeader.ImportTableDirectory, out var offset, true))
                    {
                        lock (readerLock)
                        {
                            reader.Seek(offset);

                            var results = new List<ImageImportDescriptor>();

                            while (true)
                            {
                                //If the ImportAddressTable has been loaded, use the same ImageThunkData objects where applicable.
                                //Use the internal field so we don't force load them if they're not already loaded
                                var item = new ImageImportDescriptor(ref reader, this, importAddressTable);

                                results.Add(item);

                                //The last item is all 0's. We certainly expect the name should have a value, so we look at that.
                                //We want to ensure that we include the null entry so that we can model the actual structure of the PE File
                                if (item.Name.ListedOffset == 0)
                                    break;

                                //Each ImageImportDescriptor resolves RVA references, which will modify the reader position
                                reader.Seek(offset + (results.Count * ImageImportDescriptor.StructSize));
                            }

                            importTable = results.ToArray();
                        }
                    }

                    SetRegionFlag(PERegionKind.ImportTable);
                }

                return importTable;
            }
        }

        #endregion
        #region Resource Directory (2)

        private ImageResourceDirectory? resourceDirectory;

        //Resources can theoretically be 2^31 levels deep. The general convention that Windows uses however
        //is a three level hierarchy: Type/Name/Language
        public ImageResourceDirectory? ResourceDirectory //todo: make class i think, cant ref readonly a nullable
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.ResourceDirectory))
                {
                    if (TryGetDirectoryOffset(OptionalHeader.ResourceTableDirectory, out var offset, true))
                    {
                        lock (readerLock)
                        {
                            reader.Seek(offset);

                            resourceDirectory = new ImageResourceDirectory(ref reader, this, null, offset);
                        }
                    }
                }

                return resourceDirectory;
            }
        }

        #endregion
        #region Exception Table (3)

        private RuntimeFunction[]? exceptionTable;

        public RuntimeFunction[]? ExceptionTable
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.ExceptionTable))
                {
                    if (TryGetDirectoryOffset(OptionalHeader.ExceptionTableDirectory, out var offset, true))
                    {
                        lock (readerLock)
                        {
                            reader.Seek(offset);

                            var numEntries = OptionalHeader.ExceptionTableDirectory.Size / RuntimeFunction.StructSize;

                            var entries = new RuntimeFunction[numEntries];

                            //Cache resolved imports for faster lookup
                            var context = new ExceptionHandlerContext(this);

                            for (var i = 0; i < entries.Length; i++)
                            {
                                if (i > 0)
                                    reader.Seek(offset + i * RuntimeFunction.StructSize);

                                entries[i] = new RuntimeFunction(ref reader, this, OptionalHeader.ExceptionTableDirectory, context);
                            }

                            exceptionTable = entries;
                        }
                    }

                    SetRegionFlag(PERegionKind.ExceptionTable);
                }

                return exceptionTable;
            }
        }

        #endregion
        #region Security Table (4)

        private WinCertificate[]? securityTable;

        public WinCertificate[]? SecurityTable
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.SecurityTable))
                {
                    var offset = (int) OptionalHeader.SecurityTableDirectory.VirtualAddress;

                    //https://blog.trailofbits.com/2020/05/27/verifying-windows-binaries-without-windows/
                    //https://github.com/trailofbits/uthenticode
                    //http://download.microsoft.com/download/9/c/5/9c5b2167-8017-4bae-9fde-d599bac8184a/Authenticode_PE.docx

                    if (offset != 0)
                    {
                        lock (readerLock)
                        {
                            //SecurityTable uses an absolute address
                            reader.Seek(offset);

                            var end = offset + OptionalHeader.SecurityTableDirectory.Size;

                            var results = new List<WinCertificate>();

                            while (reader.Position < end)
                            {
                                var item = new WinCertificate(ref reader);

                                results.Add(item);
                            }

                            Debug.Assert(results.Count <= 1, "Do you need to 8-byte align multiple certificates?");

                            securityTable = results.ToArray();
                        }
                    }
                }

                return securityTable;
            }
        }

        #endregion
        #region Base Relocation Table (5)

        private ImageBaseRelocation[]? baseRelocationTable;

        public ImageBaseRelocation[]? BaseRelocationTable
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.BaseRelocationTable))
                {
                    if (TryGetDirectoryOffset(OptionalHeader.BaseRelocationTableDirectory, out var offset, true))
                    {
                        lock (readerLock)
                        {
                            var end = offset + OptionalHeader.BaseRelocationTableDirectory.Size;

                            var results = new List<ImageBaseRelocation>();

                            reader.Seek(offset);

                            reader.FillBuffer(OptionalHeader.BaseRelocationTableDirectory.Size);

                            while (reader.Position < (int) end)
                            {
                                var item = new ImageBaseRelocation(ref reader);

                                results.Add(item);

                                //Must be 32-bit aligned
                                var alignedPosition = (reader.Position + 3) & ~3;

                                while (reader.Position < alignedPosition)
                                    reader.ReadByte();
                            }

                            baseRelocationTable = results.ToArray();
                        }
                    }
                }

                return baseRelocationTable;
            }
        }

        #endregion
        #region Debug Table (6)

        private ImageDebugDirectory[]? debugTable;

        public ImageDebugDirectory[]? DebugTable
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.DebugDirectory))
                {
                    if (TryGetDirectoryOffset(OptionalHeader.DebugTableDirectory, out var offset, true))
                    {
                        lock (readerLock)
                        {
                            var entryCount = OptionalHeader.DebugTableDirectory.Size / ImageDebugDirectory.StructSize;

                            var entries = new ImageDebugDirectory[entryCount];

                            for (var i = 0; i < entryCount; i++)
                            {
                                reader.Seek(offset + (i * ImageDebugDirectory.StructSize));

                                entries[i] = new ImageDebugDirectory(ref reader, this);
                            }

                            debugTable = entries;
                        }
                    }
                }

                return debugTable;
            }
        }

        #endregion
        #region Copyright Table (7)

        #endregion
        #region Global Pointer Table (8)

        #endregion
        #region Thread Local Storage Table (9)

        private ImageTlsDirectory? tlsDirectory;

        public ImageTlsDirectory? TlsDirectory
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.TlsDirectory))
                {
                    if (TryGetDirectoryOffset(OptionalHeader.ThreadLocalStorageTableDirectory, out var offset, true))
                    {
                        lock (readerLock)
                        {
                            reader.Seek(offset);
                            tlsDirectory = new ImageTlsDirectory(ref reader, OptionalHeader.Magic == PEMagic.PE32);
                        }
                    }

                    SetRegionFlag(PERegionKind.TlsDirectory);
                }

                return tlsDirectory;
            }
        }

        #endregion
        #region Load Config Table (10)

        private ImageLoadConfigDirectory? loadConfigTable;

        public ImageLoadConfigDirectory? LoadConfigTable
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.LoadConfigTable))
                {
                    if (TryGetDirectoryOffset(OptionalHeader.LoadConfigTableDirectory, out var offset, true))
                    {
                        lock (readerLock)
                        {
                            reader.Seek(offset);
                            loadConfigTable = new ImageLoadConfigDirectory(ref reader, this);
                        }
                    }

                    SetRegionFlag(PERegionKind.LoadConfigTable);
                }

                return loadConfigTable;
            }
        }

        #endregion
        #region Bound Import Table (11)

        private ImageBoundImportDescriptor[]? boundImportTable;

        public ImageBoundImportDescriptor[]? BoundImportTable
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.BoundImportTable))
                {
                    var offset = (int) OptionalHeader.BoundImportTableDirectory.VirtualAddress;

                    if (offset != 0)
                    {
                        //The bound import directory actually describes an offset in the file, not an RVA
                        //https://stackoverflow.com/questions/55857504/how-field-bound-import-directory-works

                        lock (readerLock)
                        {
                            var end = offset + OptionalHeader.BoundImportTableDirectory.Size;

                            var results = new List<ImageBoundImportDescriptor>();

                            reader.Seek(offset);

                            while (reader.Position < (int) end)
                            {
                                var item = new ImageBoundImportDescriptor(ref reader, this);

                                if (item.TimeDateStamp == 0 && item.OffsetModuleName == 0 && item.NumberOfModuleForwarderRefs == 0)
                                    break;

                                results.Add(item);

                                //Each descriptor will also read its forward refs, which will move the stream out of position
                                offset += ImageBoundImportDescriptor.FixedStructSize + (item.NumberOfModuleForwarderRefs * ImageBoundForwarderRef.StructSize);

                                reader.Seek(offset);
                            }

                            boundImportTable = results.ToArray();
                        }
                    }

                    SetRegionFlag(PERegionKind.BoundImportTable);
                }

                return boundImportTable;
            }
        }

        #endregion
        #region Import Address Table (12)

        private ImageThunkData[]? importAddressTable;

        public ImageThunkData[]? ImportAddressTable
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.ImportAddressTable))
                {
                    if (TryGetDirectoryOffset(OptionalHeader.ImportAddressTableDirectory, out var offset, true))
                    {
                        lock (readerLock)
                        {
                            reader.Seek(offset);

                            var is32Bit = OptionalHeader.Magic == PEMagic.PE32;
                            var thunkDataSize = is32Bit ? 4 : 8;

                            var results = new List<ImageThunkData>();

                            var read = 0;

                            Dictionary<RawOffset, ImageThunkData> iatCache = null;

                            if (importTable != null)
                            {
                                //If the import table has been loaded, use the same ImageThunkData objects where applicable.
                                //Use the internal field so we don't force load them if they're not already loaded
                                iatCache = new Dictionary<RawOffset, ImageThunkData>();

                                for (var i = 0; i < importTable.Length; i++)
                                {
                                    var descriptor = importTable[i];

                                    if (descriptor.ImportAddressTable.IsValid)
                                    {
                                        var iat = descriptor.ImportAddressTable.Value;

                                        if (iat == null) //The last entry is null
                                            continue;

                                        for (var j = 0; j < iat.Length; j++)
                                        {
                                            var item = iat[j];

                                            iatCache.Add(item.Offset, item);
                                        }
                                    }
                                }
                            }

                            while (read < OptionalHeader.ImportAddressTableDirectory.Size)
                            {
                                var itemOffset = offset + read;

                                if (iatCache == null || !iatCache.TryGetValue(itemOffset, out var thunk))
                                {
                                    reader.Seek(itemOffset);

                                    thunk = new ImageThunkData(ref reader, this, is32Bit, true);
                                }

                                results.Add(thunk);

                                read += thunkDataSize;
                            }

                            importAddressTable = results.ToArray();
                        }
                    }

                    SetRegionFlag(PERegionKind.ImportAddressTable);
                }

                return importAddressTable;
            }
        }

        #endregion
        #region Delay Import Table (13)

        private ImageDelayLoadDescriptor[]? delayImportTable;

        public ImageDelayLoadDescriptor[]? DelayImportTable
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.DelayImportTable))
                {
                    if (TryGetDirectoryOffset(OptionalHeader.DelayImportTableDirectory, out var offset, true))
                    {
                        lock (readerLock)
                        {
                            reader.Seek(offset);

                            var results = new List<ImageDelayLoadDescriptor>();

                            while (true)
                            {
                                var item = new ImageDelayLoadDescriptor(ref reader, this);

                                results.Add(item);

                                if (item.DllNameRVA.ListedOffset == 0)
                                    break;

                                //Each ImageDelayLoadDescriptor resolves RVA references, which will modify the reader position
                                reader.Seek(offset + (results.Count * ImageDelayLoadDescriptor.StructSize));
                            }

                            delayImportTable = results.ToArray();
                        }
                    }
                }

                return delayImportTable;
            }
        }

        #endregion
        #region Cor20Header (14)

        private ImageCor20Header? cor20Header;

        public ImageCor20Header? Cor20Header
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.Cor20Header))
                {
                    if (TryGetDirectoryOffset(OptionalHeader.CorHeaderTableDirectory, out var offset, true))
                    {
                        lock (readerLock)
                        {
                            reader.Seek(offset);

                            cor20Header = new ImageCor20Header(ref reader, this);
                        }
                    }

                    SetRegionFlag(PERegionKind.Cor20Header);
                }

                return cor20Header;
            }
        }

        private ImageCorILMethod[]? ilMethods;

        public ImageCorILMethod[]? ILMethods
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.ILMethods))
                {
                    var methods = GetCLRMetadata()?.MethodDefTable;

                    if (methods != null)
                    {
                        var ilMethods = new List<ImageCorILMethod>();

                        lock (readerLock)
                        {
                            for (var i = 0; i < methods.Count; i++)
                            {
                                var method = methods[i];

                                //Some methods (like interfaces) have an RVA of 0

                                if (!TryGetOffset(method.RVA, out var offset))
                                    continue;

                                reader.Seek(offset);

                                ilMethods.Add(new ImageCorILMethod(ref reader));
                            }

                            this.ilMethods = ilMethods.ToArray();
                        }
                    }
                    
                    SetRegionFlag(PERegionKind.ILMethods);
                }

                return ilMethods;
            }
        }
        }

        #endregion
        #endregion
        }
        }

        #endregion

        /// <summary>
        /// Gets a <see cref="PEFileView"/> that allows visualizing the physical structure of the <see cref="PEFile"/>.
        /// </summary>
        /// <param name="mode">Specifies the addressing mode that should be used in the returned view. If this value is <see cref="ViewMode.Default"/>,
        /// <see cref="ViewMode.Virtual"/> or <see cref="ViewMode.Physical"/> will automatically be selected based on the value of <see cref="IsLoadedImage"/>.</param>
        /// <returns>A <see cref="PEFileView"/> that provides a view over the structure of the PE File.</returns>
        public PEFileView GetView(ViewMode mode = ViewMode.Default)
        {
            //View may use Stream to read bytes
            lock (readerLock)
            {
                var writer = new PEViewWriter(this, ref reader, mode);
                ((IViewable) this).WriteView(writer);

                return (PEFileView) writer.Finalize();
            }
        }

        private FileReader reader;
        private object readerLock = new object();
        private volatile int flags;
        private bool disposed;

        internal IFileServices? Services { get; }

        private PEFile(Stream stream, bool isLoadedImage, IFileServices? services)
        {
            reader = new FileReader(stream, readerLock);
            IsLoadedImage = isLoadedImage;
            Services = services;

#if STRESS_TEST
            _ = DosHeader;
            _ = DosStub;
            _ = RichHeader;
            _ = NtHeaders;
            _ = SectionHeaders;

            _ = ExportTable;
            _ = ImportTable;
            _ = ResourceDirectory;
            _ = ExceptionTable;
            _ = SecurityTable;
            _ = BaseRelocationTable;
            _ = DebugTable;
            //_ = CopyrightTable;
            //_ = GlobalPointerTable;
            _ = TlsDirectory;
            _ = LoadConfigTable;
            _ = BoundImportTable;
            _ = ImportAddressTable;
            _ = DelayImportTable;
            _ = Cor20Header;
            _ = ILMethods;

            _ = ReadyToRunHeader;
            _ = AppHostSignature;
#endif
        }

        ~PEFile()
        {
            Dispose(false);
        }

        #region Offset

        /// <summary>
        /// Tries to get the physical offset within the image of the full directory pointed to by an <see cref="ImageDataDirectory"/>.
        /// </summary>
        /// <param name="entry">The <see cref="ImageDataDirectory"/> that may point to a full directory within the image.</param>
        /// <param name="offset">Offset from the start of the image to the given directory data.</param>
        /// <param name="canCrossSectionBoundary">Whether size of the entry is allowed to cross over the end of the section boundary.</param>
        /// <returns>True if the <see cref="ImageDataDirectory"/> points to a valid section, otherwise false.</returns>
        public bool TryGetDirectoryOffset(ImageDataDirectory entry, out RawOffset offset, bool canCrossSectionBoundary)
        {
            var sectionIndex = GetSectionContainingRVA(entry.VirtualAddress);

            if (sectionIndex < 0)
            {
                offset = (RawOffset) (-1);
                return false;
            }

            var section = SectionHeaders[sectionIndex];
            var relativeOffset = (int) (entry.VirtualAddress - section.VirtualAddress);

            if (!canCrossSectionBoundary && entry.Size > section.VirtualSize - relativeOffset)
                throw new BadImageFormatException("Section too small.");

            offset = IsLoadedImage
                ? (RawOffset) entry.VirtualAddress
                : section.PointerToRawData + relativeOffset;

            return true;
        }

        /// <summary>
        /// Tries to get the physical offset within the image of a specified relative virtual address.
        /// </summary>
        /// <param name="rva">The relative virtual address within the image to translate.</param>
        /// <param name="offset">The translated physical address.</param>
        /// <returns>True if the RVA was translated to a physical offset, otherwise false.</returns>
        public bool TryGetOffset(RVA rva, out RawOffset offset)
        {
            var sectionIndex = GetSectionContainingRVA(rva);

            if (sectionIndex < 0)
            {
                offset = (RawOffset) (-1);
                return false;
            }

            var section = SectionHeaders[sectionIndex];
            var relativeOffset = (int) (rva - section.VirtualAddress);

            offset = IsLoadedImage
                ? (RawOffset) (int) rva
                : section.PointerToRawData + relativeOffset;

            return true;
        }

        public bool TryGetRVA(RawOffset offset, out RVA rva)
        {
            //Offsets returned from TryGetOffset/TryGetDirectoryOffset are just RVAs
            if (IsLoadedImage)
            {
                rva = (RVA) offset;
                return true;
            }

            var sectionIndex = GetSectionContainingOffset(offset);

            if (sectionIndex < 0)
            {
                rva = (RVA) (-1);
                return false;
            }

            var section = SectionHeaders[sectionIndex];

            var relativeRVA = (int) (offset - section.PointerToRawData);
            rva = section.VirtualAddress + relativeRVA;
            return true;
        }

        /// <summary>
        /// Gets the section that contains the specified Relative Virtual Address.
        /// </summary>
        /// <param name="rva">The RVA whose containing section should be found.</param>
        /// <returns>The index of section that contains the RVA, or -1 if none was found.</returns>
        public int GetSectionContainingRVA(RVA rva)
        {
            if (rva == 0)
                return -1;

            Debug.Assert(SectionHeaders != null);

            for (var i = 0; i < SectionHeaders.Length; i++)
            {
                var start = SectionHeaders[i].VirtualAddress;
                var end = SectionHeaders[i].VirtualAddress + SectionHeaders[i].VirtualSize;

                if (start <= rva && rva < end)
                    return i;
            }

            return -1;
        }

        public int GetSectionContainingOffset(RawOffset offset)
        {
            //Offsets returned from TryGetOffset/TryGetDirectoryOffset are just RVAs
            if (IsLoadedImage)
                return GetSectionContainingRVA((RVA) offset);

            for (var i = 0; i < SectionHeaders.Length; i++)
            {
                var start = SectionHeaders[i].PointerToRawData;
                var end = SectionHeaders[i].PointerToRawData + SectionHeaders[i].VirtualSize;

                if (start <= offset && offset < end)
                    return i;
            }

            return -1;
        }

        #endregion

        /// <summary>
        /// Invalidates all regions that have been read from the Portable Executable file,
        /// requiring that they be re-read when they are next accessed.
        /// </summary>
        public void Invalidate() => SetRegionFlag(0);

        internal delegate T WithReaderCallback<T>(ref FileReader reader, PEFile peFile);

        internal T WithReader<T>(RawOffset offset, WithReaderCallback<T> callback)
        {
            lock (readerLock)
            {
                reader.Seek(offset);

                return callback(ref reader, this);
            }
        }

        /// <summary>
        /// Invalidates one or more regions that have been read from the Portable Executable file,
        /// requiring that they be re-read when they are next accessed.
        /// </summary>
        /// <param name="regions">The regions to invalidate.</param>
        public void Invalidate(PERegionKind regions) =>
            SetRegionFlag(~regions);

        internal bool HasRegionFlag(PERegionKind flag) =>
            (this.flags & (int) flag) != 0;

        internal void SetRegionFlag(PERegionKind newFlag)
        {
            int original;
            int newValue;

            do
            {
                original = this.flags;
                newValue = newFlag == 0 ? 0 : original | (int) newFlag;
            } while (Interlocked.CompareExchange(ref this.flags, newValue, original) != original);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            writer.WriteGlobal(DosHeader);
            writer.WriteDosStub(DosStub);
            writer.WriteGlobal(RichHeader);
            writer.WriteGlobal(NtHeaders);
            writer.WriteGlobal(SectionHeaders);

            writer.WriteGlobal(ExportTable);
            writer.WriteUniqueGlobal(ImportAddressTable); //Write this before the Import Table as we want IAT entries to be in a data directory, not a logical region
            writer.WriteGlobal(ImportTable);
            writer.WriteGlobal(ResourceDirectory);
            writer.WriteGlobal(ExceptionTable);
            writer.WriteGlobal(SecurityTable);
            writer.WriteGlobal(BaseRelocationTable);
            writer.WriteGlobal(DebugTable);
            //Copyright Table
            //Global Pointer Table
            writer.WriteGlobal(TlsDirectory);
            writer.WriteGlobal(LoadConfigTable);
            writer.WriteGlobal(BoundImportTable);
            writer.WriteGlobal(DelayImportTable);
            writer.WriteGlobal(Cor20Header);
            writer.WriteGlobal(AppHostSignature);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                reader.Dispose();

                //This is a bit of a gotcha! If you declare a finalizer, it won't be GC'd until the finalizer thread processes it.
                //Which means if you're generating a lot of objects, the finalizer thread might not be able to keep up
                GC.SuppressFinalize(this);
            }

            disposed = true;
        }
    }
}
