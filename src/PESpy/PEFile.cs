using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using ClrDebug;
using PESpy.Native;
using PESpy.View;
using System.Text;
using Stream = System.IO.Stream;

#if !DEBUG_POSITION
using RVA = System.Int32;
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    //ref readonly properties don't display properly in the debugger. We don't want to hurt application performance, but we also want
    //to ensure that we have a good debugging experience. Using a custom DebuggerTypeProxy allows us to have both
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

        #endregion

        public ImageCorILMethod[]? ILMethods => peFile.ILMethods;

        public ReadyToRunHeader? ReadyToRunHeader => peFile.ReadyToRunHeader;

        public AppHostSignature? AppHostSignature => peFile.AppHostSignature;

        public ClrEngineMetrics? ClrEngineMetrics => peFile.ClrEngineMetrics;

        public RuntimeInfo? RuntimeInfo => peFile.RuntimeInfo;

        public DotNetRuntimeDebugHeader? DotNetRuntimeDebugHeader => peFile.DotNetRuntimeDebugHeader;
    }

    /// <summary>
    /// Represents a Portable Executable (PE) file.
    /// </summary>
    [DebuggerTypeProxy(typeof(PEFileDebugView))]
    public class PEFile : IFile, IViewable, IDisposable
    {
        #region Static

#if PEFAST
        /// <summary>
        /// Reads a <see cref="PEFile"/> from a file on disk.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A <see cref="PEFile"/> that provides access to the contents of the specified file.</returns>
        public static PEFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new PEFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Close();

                throw;
            }
        }

        /// <summary>
        /// Reads a <see cref="PEFile"/> from a module contained in a remote process.
        /// </summary>
        /// <param name="hProcess">A handle to the process containing the module that should be read.</param>
        /// <param name="moduleBase">The base address of the module in the remote process that should be read.</param>
        /// <returns>A <see cref="PEFile"/> that provides access to the contents of the specified module.</returns>
        public static unsafe PEFile FromProcess(IntPtr hProcess, IntPtr moduleBase) =>
            new PEFile(new RemoteMemoryReader(hProcess), (long) (void*) moduleBase);

        public static PEFile FromStream(Stream stream, bool isLoadedImage)
        {
            //If it's a FileStream, implicitly it's not a loaded image
            if (stream is FileStream fs)
            {
                var mmf = new MemoryMappedFileHolder(fs);

                try
                {
                    return new PEFile(fs.Name, mmf);
                }
                catch
                {
                    mmf.Close();

                    throw;
                }
            }

            return new PEFile(new StreamMemoryReader(stream), stream.Position);
        }
#else
        /// <summary>
        /// Reads a <see cref="PEFile"/> from a specified path.<para/>
        /// This method opens the specified file, and does not close it until <see cref="Dispose()"/> is called.
        /// </summary>
        /// <param name="filePath">The path to the file to read.</param>
        /// <returns>A <see cref="PEFile"/> that encapsulates the specified file.</returns>
        public static PEFile FromFile(string filePath) => FromFile(filePath, null);

        public static PEFile FromFile(string filePath, IFileServices? services) =>
            new PEFile(File.OpenRead(filePath), false, services);

        public static PEFile FromProcess(IntPtr hProcess, IntPtr moduleBase) => throw new NotImplementedException();

        public static PEFile FromStream(Stream stream, bool isLoadedImage) => FromStream(stream, isLoadedImage, null);

        public static PEFile FromStream(Stream stream, bool isLoadedImage, IFileServices? services) =>
            new PEFile(stream, isLoadedImage, services);

#if DEBUG
        public static Func<long, string> GetSymbolName { get; set; }
#endif

#endif
        #endregion

        /// <summary>
        /// Gets whether the image exists within the memory a live process, or exists on disk. Offsets are slightly different in some areas when in memory vs on disk.
        /// </summary>
        public bool IsLoadedImage { get; init; }

        private SymStoreKey[]? symStoreKeys;

        public SymStoreKey[] SymStoreKeys
        {
            get
            {
                if (symStoreKeys == null)
                {
                    var results = new List<SymStoreKey>();

                    if (Name != null)
                    {
                        results.Add(SymStoreKey.FromPE(Name, FileHeader.TimeDateStamp, OptionalHeader.SizeOfImage));
                    }

                    var debugTable = DebugTable;

                    if (debugTable != null)
                    {
                        for (var i = 0; i < debugTable.Length; i++)
                        {
                            ref var debugDirectory = ref debugTable[i];

                            switch (debugDirectory.Type)
                            {
                                case ImageDebugType.CodeView:
                                {
                                    var data = (ICodeViewPDB?) debugDirectory.Data;

                                    if (data != null)
                                    {
                                        switch (data.Signature)
                                        {
                                            case CodeViewSig.RSDS:
                                                results.Add(SymStoreKey.FromRSDSI((RSDSI) data));
                                                break;

                                            case CodeViewSig.NB10:
                                                results.Add(SymStoreKey.FromNB10((NB10I) data));
                                                break;
                                        }
                                    }

                                    break;
                                }

                                case ImageDebugType.Misc:
                                {
                                    var data = (ImageDebugMisc?) debugDirectory.Data;

                                    if (data != null)
                                        results.Add(SymStoreKey.FromMisc(data.Data, FileHeader.TimeDateStamp, OptionalHeader.SizeOfImage));

                                    break;
                                }
                            }
                        }
                    }

                    symStoreKeys = results.ToArray();
                }

                return symStoreKeys;
            }
        }

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.PE;

        #region DosHeader

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageDosHeader dosHeader;

        /// <summary>
        /// Gets the MS-DOS 2.0 compatible <see cref="IMAGE_DOS_HEADER"/>.
        /// </summary>
#if PEFAST
        public ref readonly ImageDosHeader DosHeader => ref dosHeader;
#else
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
                        dosHeader = new ImageDosHeader(reader);
                    }

                    SetRegionFlag(PERegionKind.DosHeader);
                }

                return ref dosHeader;
            }
        }
#endif

        #endregion
        #region DosStub

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ByteBlob dosStub;

#if PEFAST
        public ref readonly ByteBlob DosStub
        {
            get
            {
                if (dosStub.Offset == 0)
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

                    var start = ImageDosHeader.StructSize;
                    int end;

                    if (RichHeader != null)
                        end = RichHeader.Offset;
                    else
                    {
                        //We know there isn't a RichHeader. Read up until the start of the new PE Header
                        end = DosHeader.FileAddressOfNewExeHeader;
                    }

                    var length = (int) (end - start);

                    dosStub = new ByteBlob(new MemoryChunk(headerBlock, start), length);
                }

                return ref dosStub;
            }
        }
#else
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

                        var length = (int) (end - start);

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
#endif

        #endregion
        #region RichHeader

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RichHeader? richHeader;
        private bool hasTriedRichHeader;

        /// <summary>
        /// Gets the undocumented Rich Header which describes the build environment that was used to create the file.<para/>
        /// If the file does not have a Rich Header, this property returns <see langword="null"/>.
        /// </summary>
#if PEFAST
        public RichHeader? RichHeader
        {
            get
            {
                if (richHeader == null && !hasTriedRichHeader)
                {
                    //We already know we're a PE file, as the NT Headers have already been loaded

                    richHeader = RichHeader.New(dosHeader.FileAddressOfNewExeHeader, headerBlock);
                    hasTriedRichHeader = true;
                }

                return richHeader;
            }
        }
#else
		public RichHeader? RichHeader
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.RichHeader))
                {
                    lock (readerLock)
                    {
                        //We don't yet know whether we're actually a PE File yet (as opposed to an MS-DOS one). Force load
                        //the NT Headers to verify that e_lfanew points to the PE signature
                        _ = NtHeaders;

                        reader.Seek(ImageDosHeader.StructSize);
                        richHeader = RichHeader.New(DosHeader.FileAddressOfNewExeHeader, reader);
                    }

                    SetRegionFlag(PERegionKind.RichHeader);
                }

                return richHeader;
            }
        }
#endif

        #endregion
        #region NtHeaders

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageNtHeaders ntHeaders;

#if PEFAST
        public ref readonly ImageNtHeaders NtHeaders => ref ntHeaders;
#else
        public ref readonly ImageNtHeaders NtHeaders
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.NtHeaders))
                {
                    lock (readerLock)
                    {
                        reader.Seek(DosHeader.FileAddressOfNewExeHeader);
                        ntHeaders = new ImageNtHeaders(reader);
                    }

                    SetRegionFlag(PERegionKind.NtHeaders);
                }

                return ref ntHeaders;
            }
        }
#endif

        /// <summary>
        /// Gets the <see cref="IMAGE_NT_HEADERS.FileHeader"/> field that represents the file header of the image.
        /// </summary>
#if PEFAST
        public ImageFileHeader FileHeader => ntHeaders.FileHeader;
#else
        public ImageFileHeader FileHeader => NtHeaders.FileHeader; //Cannot be ref readonly
#endif

        /// <summary>
        /// Gets the the <see cref="IMAGE_NT_HEADERS.OptionalHeader"/> field that represents the optional header of the image.
        /// </summary>
#if PEFAST
        public ImageOptionalHeader OptionalHeader => ntHeaders.OptionalHeader;
#else
        public ImageOptionalHeader OptionalHeader => NtHeaders.OptionalHeader; //Cannot be ref readonly
#endif

        #endregion
        #region SectionHeaders

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageSectionHeader[]? sectionHeaders;

        /// <summary>
        /// Gets the section headers of the image. These values represent the <see cref="IMAGE_SECTION_HEADER"/> values (e.g. .text, .data) that immediately follow the <see cref="OptionalHeader"/>.<para/>
        /// Each section header points to a relative location within the image at which that section actually resides.
        /// </summary>
#if PEFAST
        public ImageSectionHeader[] SectionHeaders
        {
            get
            {
                if (sectionHeaders == null)
                {
                    var numberOfSections = ntHeaders.FileHeader.NumberOfSections;

                    var list = new ImageSectionHeader[numberOfSections];

                    var offset = dosHeader.FileAddressOfNewExeHeader + ImageNtHeaders.StructSize(headerBlock.Is32Bit);

                    for (var i = 0; i < numberOfSections; i++)
                        list[i] = new ImageSectionHeader(new MemoryChunk(headerBlock, offset + (i * ImageSectionHeader.StructSize)));

                    //No need to CompareExchange (which is also marginally slower).
                    //Unlike our blocks, we don't care what gets returned here;
                    //everything is a just pointers into our header block anyway
                    sectionHeaders = list;
                }

                return sectionHeaders;
            }
        }
#else
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
                            list[i] = new ImageSectionHeader(reader);

                        sectionHeaders = list;
                    }

                    SetRegionFlag(PERegionKind.SectionHeaders);
                }

                return sectionHeaders!;
            }
        }
#endif

        #endregion
        #region Directories
        #region Export Table (0)

        private ImageExportDirectory? exportTable;

        /// <summary>
        /// Gets the export table pointed to by <see cref="ImageOptionalHeader.ExportTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_EXPORT), containing all exports present in the image.<para/>
        /// If the image does not have an export table, this property returns <see langword="null"/>.
        /// </summary>
#if PEFAST
        public ImageExportDirectory? ExportTable
        {
            get
            {
                if (exportTable == null)
                {
                    var exportTableDirectory = OptionalHeader.ExportTableDirectory;

                    if (exportTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(exportTableDirectory, out var chunk))
                    {
                        chunk.Demand(exportTableDirectory.VirtualAddress, ImageExportDirectory.StructSize);

                        var local = new ImageExportDirectory(chunk);

                        //Since this property returns a reference, we should always return the same object
                        Interlocked.CompareExchange(ref exportTable, local, null);
                    }
                }

                return exportTable;
            }
        }
#else
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
                            exportTable = new ImageExportDirectory(reader, this);
                        }
                    }

                    SetRegionFlag(PERegionKind.ExportTable);
                }

                return exportTable;
            }
        }
#endif

        #endregion
        #region Import Table (1)

        private ImageImportDescriptor[]? importTable;

#if PEFAST
        /// <summary>
        /// Gets the import table pointed to by <see cref="ImageOptionalHeader.ImportTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_IMPORT), containing all imports present in the image.<para/>
        /// If the image does not have an import table, this property returns <see langword="null"/>.
        /// </summary>
        public ImageImportDescriptor[]? ImportTable
        {
            get
            {
                if (importTable == null)
                {
                    var importTableDirectory = OptionalHeader.ImportTableDirectory;

                    if (importTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(importTableDirectory, out var chunk))
                    {
                        chunk.Demand(importTableDirectory.VirtualAddress, importTableDirectory.Size);

                        //I don't know if we're guaranteed to fill up the entire ImportTableDirectory with
                        //ImageImportDescriptor objects, or if there's other stuff in there too. I feel like
                        //the latter is the case, as such we can't calculate exactly how many entries we'll have
                        var results = new List<ImageImportDescriptor>();

                        var read = 0;

                        while (true)
                        {
                            //If the ImportAddressTable has been loaded, use the same ImageThunkData objects where applicable.
                            //Use the internal field so we don't force load them if they're not already loaded
                            var item = new ImageImportDescriptor(chunk.Slice(read));

                            read += ImageImportDescriptor.StructSize;

                            results.Add(item);

                            //The last item is all 0's. We certainly expect the name should have a value, so we look at that.
                            //We want to ensure that we include the null entry so that we can model the actual structure of the PE File
                            if (item.Name.ListedOffset == 0)
                                break;
                        }

                        //Since this property returns a reference, we should always return the same object
                        Interlocked.CompareExchange(ref importTable, results.ToArray(), null);
                    }
                }

                return importTable;
            }
        }
#else
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
                                var item = new ImageImportDescriptor(reader, this, importAddressTable);

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
#endif

        #endregion
        #region Resource Directory (2)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageResourceDirectory? resourceDirectory;

#if PEFAST
        /// <summary>
        /// Gets the resource directory pointed to by <see cref="ImageOptionalHeader.ResourceTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_RESOURCE), containing all resources present in the image.<para/>
        /// If the image does not have a resource directory, this property returns <see langword="null"/>.
        /// </summary>
        public ImageResourceDirectory? ResourceDirectory
        {
            get
            {
                if (resourceDirectory == null)
                {
                    var resourceTableDirectory = OptionalHeader.ResourceTableDirectory;

                    if (resourceTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(resourceTableDirectory, out var chunk))
                    {
                        chunk.Demand(resourceTableDirectory.VirtualAddress, resourceTableDirectory.Size);

                        var local = new ImageResourceDirectory(chunk, resourceTableDirectory.VirtualAddress, null);

                        //Since this property returns a reference, we should always return the same object
                        Interlocked.CompareExchange(ref resourceDirectory, local, null);
                    }
                }

                return resourceDirectory;
            }
        }
#else
        //Resources can theoretically be 2^31 levels deep. The general convention that Windows uses however
        //is a three level hierarchy: Type/Name/Language
        public ImageResourceDirectory? ResourceDirectory
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

                            resourceDirectory = new ImageResourceDirectory(reader, this, null, offset);
                        }
                    }

                    SetRegionFlag(PERegionKind.ResourceDirectory);
                }

                return resourceDirectory;
            }
        }
#endif

        #endregion
        #region Exception Table (3)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RuntimeFunction[]? exceptionTable;

#if PEFAST
        /// <summary>
        /// Gets the exception table pointed to by <see cref="ImageOptionalHeader.ExceptionTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_EXCEPTION) containing information used to unwind stack frames during exception handling.<para/>
        /// If the image does not have an exception table, this property returns <see langword="null"/>.
        /// </summary>
        public RuntimeFunction[]? ExceptionTable
        {
            get
            {
                if (exceptionTable == null)
                {
                    var exceptionTableDirectory = OptionalHeader.ExceptionTableDirectory;

                    if (exceptionTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(exceptionTableDirectory, out var chunk))
                    {
                        chunk.Demand(exceptionTableDirectory.VirtualAddress, exceptionTableDirectory.Size);

                        var numEntries = OptionalHeader.ExceptionTableDirectory.Size / RuntimeFunction.StructSize;

                        var entries = new RuntimeFunction[numEntries];

                        //Cache resolved imports for faster lookup
                        var context = new ExceptionHandlerContext(this);

                        for (var i = 0; i < entries.Length; i++)
                            entries[i] = new RuntimeFunction(chunk.Slice(i * RuntimeFunction.StructSize));

                        exceptionTable = entries;
                    }
                }

                return exceptionTable;
            }
        }
#else
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

                                entries[i] = new RuntimeFunction(reader, this, OptionalHeader.ExceptionTableDirectory, context);
                            }

                            exceptionTable = entries;
                        }
                    }

                    SetRegionFlag(PERegionKind.ExceptionTable);
                }

                return exceptionTable;
            }
        }
#endif

        #endregion
        #region Security Table (4)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private WinCertificate[]? securityTable;

        /// <summary>
        /// Gets the certificates contained in the Security Table pointed to by <see cref="ImageOptionalHeader.SecurityTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_SECURITY),
        /// or <see langword="null"/> if the Security Table was not present or did not point to a valid location.<para/>
        /// The Security Table is typically contained in the Overlay (the area beyond the last section) and as such is not present when <see cref="IsLoadedImage"/> is <see langword="true"/>.
        /// </summary>
#if PEFAST
        public WinCertificate[]? SecurityTable
        {
            get
            {
                if (securityTable == null)
                {
                    var securityTableDirectory = OptionalHeader.SecurityTableDirectory;

                    //SecurityTable uses an absolute address, and should be in the overlay. We will make a best effort attempt to resolve the physical
                    //location of this value regardless of whether we are a loaded image or not
                    if (securityTableDirectory.VirtualAddress != 0 && TryGetValueChunkFromPhysicalOffset(securityTableDirectory.VirtualAddress, out var chunk))
                    {
                        //https://blog.trailofbits.com/2020/05/27/verifying-windows-binaries-without-windows/
                        //https://github.com/trailofbits/uthenticode
                        //http://download.microsoft.com/download/9/c/5/9c5b2167-8017-4bae-9fde-d599bac8184a/Authenticode_PE.docx

                        chunk.Demand(securityTableDirectory.VirtualAddress, securityTableDirectory.Size);

                        var end = securityTableDirectory.Size;

                        var results = new List<WinCertificate>();

                        var read = 0;

                        while (read < end)
                        {
                            var item = new WinCertificate(chunk.Slice(read));
                            read += item.Length; //Length includes the fixed members as well

                            results.Add(item);
                        }

                        Debug.Assert(results.Count <= 1, "Do you need to 8-byte align multiple certificates?");

                        //Since this property returns a reference, we should always return the same object
                        Interlocked.CompareExchange(ref securityTable, results.ToArray(), null);
                    }
                }

                return securityTable;
            }
        }
#else
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
                                var item = new WinCertificate(reader);

                                results.Add(item);
                            }

                            Debug.Assert(results.Count <= 1, "Do you need to 8-byte align multiple certificates?");

                            securityTable = results.ToArray();
                        }
                    }

                    SetRegionFlag(PERegionKind.SecurityTable);
                }

                return securityTable;
            }
        }
#endif

        #endregion
        #region Base Relocation Table (5)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageBaseRelocation[]? baseRelocationTable;

#if PEFAST
        /// <summary>
        /// Gets the base relocation table pointed to by <see cref="ImageOptionalHeader.BaseRelocationTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_BASERELOC).<para/>
        /// If the image does not have a base relocation table, this property returns <see langword="null"/>.
        /// </summary>
        public ImageBaseRelocation[]? BaseRelocationTable
        {
            get
            {
                var baseRelocationTableDirectory = OptionalHeader.BaseRelocationTableDirectory;

                if (baseRelocationTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(baseRelocationTableDirectory, out var chunk))
                {
                    chunk.Demand(baseRelocationTableDirectory.VirtualAddress, baseRelocationTableDirectory.Size);

                    var end = OptionalHeader.BaseRelocationTableDirectory.Size;

                    var results = new List<ImageBaseRelocation>();

                    var read = 0;

                    while (read < (int) end)
                    {
                        var item = new ImageBaseRelocation(chunk.Slice(read));

                        read += item.SizeOfBlock;

                        results.Add(item);

                        //Must be 32-bit aligned
                        read = (read + 3) & ~3;
                    }

                    baseRelocationTable = results.ToArray();
                }

                return baseRelocationTable;
            }
        }
#else
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
                                var item = new ImageBaseRelocation(reader);

                                results.Add(item);

                                //Must be 32-bit aligned
                                var alignedPosition = (reader.Position + 3) & ~3;

                                while (reader.Position < alignedPosition)
                                    reader.ReadByte();
                            }

                            baseRelocationTable = results.ToArray();
                        }
                    }

                    SetRegionFlag(PERegionKind.BaseRelocationTable);
                }

                return baseRelocationTable;
            }
        }
#endif

        #endregion
        #region Debug Table (6)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageDebugDirectory[]? debugTable;

#if PEFAST
        /// <summary>
        /// Gets the debug table pointed to by <see cref="ImageOptionalHeader.DebugTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_DEBUG).<para/>
        /// If the image does not have a debug table, this property returns <see langword="null"/>.
        /// </summary>
        public ImageDebugDirectory[]? DebugTable
        {
            get
            {
                if (debugTable == null)
                {
                    var debugTableDirectory = OptionalHeader.DebugTableDirectory;

                    if (debugTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(debugTableDirectory, out var chunk))
                    {
                        chunk.Demand(debugTableDirectory.VirtualAddress, debugTableDirectory.Size);

                        var entryCount = OptionalHeader.DebugTableDirectory.Size / ImageDebugDirectory.StructSize;

                        var entries = new ImageDebugDirectory[entryCount];

                        for (var i = 0; i < entryCount; i++)
                            entries[i] = new ImageDebugDirectory(chunk.Slice(i * ImageDebugDirectory.StructSize));

                        debugTable = entries;
                    }
                }

                return debugTable;
            }
        }
#else
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

                                entries[i] = new ImageDebugDirectory(reader, this);
                            }

                            debugTable = entries;
                        }
                    }

                    SetRegionFlag(PERegionKind.DebugDirectory);
                }

                return debugTable;
            }
        }
#endif

        #endregion
        #region Copyright Table (7)

        #endregion
        #region Global Pointer Table (8)

        #endregion
        #region Thread Local Storage Table (9)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageTlsDirectory? tlsDirectory;

#if PEFAST
        /// <summary>
        /// Gets the thread local storage table pointed to by <see cref="ImageOptionalHeader.ThreadLocalStorageTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_TLS).<para/>
        /// If the image does not have a thread local storage table, this property returns <see langword="null"/>.
        /// </summary>
        public ImageTlsDirectory? TlsDirectory
        {
            get
            {
                if (tlsDirectory == null)
                {
                    var threadLocalStorageTableDirectory = OptionalHeader.ThreadLocalStorageTableDirectory;

                    if (threadLocalStorageTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(threadLocalStorageTableDirectory, out var chunk))
                    {
                        chunk.Demand(threadLocalStorageTableDirectory.VirtualAddress, ImageTlsDirectory.StructSize(chunk.Is32Bit));

                        var local = new ImageTlsDirectory(chunk);

                        //Since this property returns a reference, we should always return the same object
                        Interlocked.CompareExchange(ref tlsDirectory, local, null);
                    }
                }

                return tlsDirectory;
            }
        }
#else
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
                            tlsDirectory = new ImageTlsDirectory(reader, OptionalHeader.Magic == PEMagic.PE32);
                        }
                    }

                    SetRegionFlag(PERegionKind.TlsDirectory);
                }

                return tlsDirectory;
            }
        }
#endif

        #endregion
        #region Load Config Table (10)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageLoadConfigDirectory? loadConfigTable;

#if PEFAST
        /// <summary>
        /// Gets the load config table pointed to by <see cref="ImageOptionalHeader.LoadConfigTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG).<para/>
        /// If the image does not have a load config table, this property returns <see langword="null"/>.
        /// </summary>
        public ImageLoadConfigDirectory? LoadConfigTable
        {
            get
            {
                if (loadConfigTable == null)
                {
                    var loadConfigTableDirectory = OptionalHeader.LoadConfigTableDirectory;

                    if (loadConfigTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(loadConfigTableDirectory, out var chunk))
                    {
                        chunk.Demand(loadConfigTableDirectory.VirtualAddress, loadConfigTableDirectory.Size);

                        var local = new ImageLoadConfigDirectory(chunk);

                        //Since this property returns a reference, we should always return the same object
                        Interlocked.CompareExchange(ref loadConfigTable, local, null);
                    }
                }

                return loadConfigTable;
            }
        }
#else
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
                            loadConfigTable = new ImageLoadConfigDirectory(reader, this);
                        }
                    }

                    SetRegionFlag(PERegionKind.LoadConfigTable);
                }

                return loadConfigTable;
            }
        }
#endif

        #endregion
        #region Bound Import Table (11)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageBoundImportDescriptor[]? boundImportTable;

#if PEFAST
        /// <summary>
        /// Gets the bound import table pointed to by <see cref="ImageOptionalHeader.BoundImportTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT).<para/>
        /// If the image does not have a bound import table, this property returns <see langword="null"/>.
        /// </summary>
        public ImageBoundImportDescriptor[]? BoundImportTable
        {
            get
            {
                if (boundImportTable == null)
                {
                    //The bound import directory actually describes an offset in the file, not an RVA
                    //https://stackoverflow.com/questions/55857504/how-field-bound-import-directory-works

                    var boundImportTableDirectory = OptionalHeader.BoundImportTableDirectory;

                    if (boundImportTableDirectory.VirtualAddress != 0 && TryGetValueChunkFromPhysicalOffset(boundImportTableDirectory.VirtualAddress, out var chunk))
                    {
                        var end = boundImportTableDirectory.Size;

                        chunk.Demand(boundImportTableDirectory.VirtualAddress, end);

                        var results = new List<ImageBoundImportDescriptor>();

                        var read = 0;

                        while (read < (int) end)
                        {
                            var item = new ImageBoundImportDescriptor(chunk.Slice(read));

                            if (item.TimeDateStamp == 0 && item.OffsetModuleName == 0 && item.NumberOfModuleForwarderRefs == 0)
                                break;

                            results.Add(item);

                            read += ImageBoundImportDescriptor.FixedStructSize + (item.NumberOfModuleForwarderRefs * ImageBoundForwarderRef.StructSize);
                        }

                        boundImportTable = results.ToArray();
                    }
                }

                return boundImportTable;
            }
        }
#else
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
                                var item = new ImageBoundImportDescriptor(reader, this);

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
#endif

        #endregion
        #region Import Address Table (12)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageThunkData[]? importAddressTable;

#if PEFAST
        /// <summary>
        /// Gets the import address table pointed to by <see cref="ImageOptionalHeader.ImportAddressTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_IAT).<para/>
        /// If the image does not have an import address table, this property returns <see langword="null"/>.
        /// </summary>
        public ImageThunkData[]? ImportAddressTable
        {
            get
            {
                importAddressTable = null;
                Debug.Assert(importAddressTable == null); //Dummy use
                throw new NotImplementedException();
            }
        }
#else
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

                                    thunk = new ImageThunkData(reader, this, is32Bit, true);
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
#endif

        #endregion
        #region Delay Import Table (13)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageDelayLoadDescriptor[]? delayImportTable;

#if PEFAST
        /// <summary>
        /// Gets the delay import table pointed to by <see cref="ImageOptionalHeader.DelayImportTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT).<para/>
        /// If the image does not have a delay import table, this property returns <see langword="null"/>.
        /// </summary>
        public ImageDelayLoadDescriptor[]? DelayImportTable
        {
            get
            {
                if (delayImportTable == null)
                {
                    var delayImportTableDirectory = OptionalHeader.DelayImportTableDirectory;

                    if (delayImportTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(delayImportTableDirectory, out var chunk))
                    {
                        chunk.Demand(delayImportTableDirectory.VirtualAddress, delayImportTableDirectory.Size);

                        var results = new List<ImageDelayLoadDescriptor>();

                        var read = 0;

                        while (true)
                        {
                            var item = new ImageDelayLoadDescriptor(chunk.Slice(read));

                            results.Add(item);

                            if (item.DllNameRVA.ListedOffset == 0)
                                break;

                            read += ImageDelayLoadDescriptor.StructSize;
                        }

                        //Since this property returns a reference, we should always return the same object
                        Interlocked.CompareExchange(ref delayImportTable, results.ToArray(), null);
                    }
                }

                return delayImportTable;
            }
        }
#else
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
                                var item = new ImageDelayLoadDescriptor(reader, this);

                                results.Add(item);

                                if (item.DllNameRVA.ListedOffset == 0)
                                    break;

                                //Each ImageDelayLoadDescriptor resolves RVA references, which will modify the reader position
                                reader.Seek(offset + (results.Count * ImageDelayLoadDescriptor.StructSize));
                            }

                            delayImportTable = results.ToArray();
                        }
                    }

                    SetRegionFlag(PERegionKind.DebugDirectory);
                }

                return delayImportTable;
            }
        }
#endif

        #endregion
        #region Cor20Header (14)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageCor20Header? cor20Header;

#if PEFAST
        /// <summary>
        /// Gets the Cor20 Header pointed to by <see cref="ImageOptionalHeader.CorHeaderTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR).<para/>
        /// If the image does not have a Cor20 Header, this property returns <see langword="null"/>.
        /// </summary>
        public ImageCor20Header? Cor20Header
        {
            get
            {
                if (cor20Header == null)
                {
                    var corHeaderTableDirectory = OptionalHeader.CorHeaderTableDirectory;

                    if (corHeaderTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(corHeaderTableDirectory, out var chunk))
                    {
                        chunk.Demand(corHeaderTableDirectory.VirtualAddress, ImageExportDirectory.StructSize);

                        var local = new ImageCor20Header(chunk);

                        //Since this property returns a reference, we should always return the same object
                        Interlocked.CompareExchange(ref cor20Header, local, null);
                    }
                }

                return cor20Header;
            }
        }
#else
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

                            cor20Header = new ImageCor20Header(reader, this);
                        }
                    }

                    SetRegionFlag(PERegionKind.Cor20Header);
                }

                return cor20Header;
            }
        }
#endif
#if PEFAST
        #region EcmaMetadata

        private EcmaMetadata? ecmaMetadata;

        /// <summary>
        /// Gets the metadata table pointed to by <see cref="ImageCor20Header.Metadata"/>.<para/>
        /// If the image does not have a Cor20 Header, or does not contain CLR Metadata, this property returns <see langword="null"/>.
        /// </summary>
        public EcmaMetadata? EcmaMetadata
        {
            get
            {
                if (ecmaMetadata == null)
                {
                    var cor20 = Cor20Header;

                    if (cor20 != null)
                    {
                        var table = cor20.Metadata;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ecmaMetadata = new EcmaMetadata(chunk);
                        }
                    }
                }

                return ecmaMetadata;
            }
        }

        #endregion
        #region Cor20Resources

        private object? cor20Resources;

        public object? Cor20Resources
        {
            get
            {
                if (cor20Resources == null)
                {
                    var cor20 = Cor20Header;

                    if (cor20 != null)
                    {
                        var table = cor20.Resources;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            cor20Resources = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return cor20Resources;
            }
        }

        #endregion
        #region Cor20StrongNameSignature

        private object? cor20StrongNameSignature;

        public object? Cor20StrongNameSignature
        {
            get
            {
                if (cor20StrongNameSignature == null)
                {
                    var cor20 = Cor20Header;

                    if (cor20 != null)
                    {
                        var table = cor20.StrongNameSignature;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            cor20StrongNameSignature = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return cor20StrongNameSignature;
            }
        }

        #endregion
        #region Cor20CodeManagerTable

        private object? cor20CodeManagerTable;

        public object? Cor20CodeManagerTable
        {
            get
            {
                if (cor20CodeManagerTable == null)
                {
                    var cor20 = Cor20Header;

                    if (cor20 != null)
                    {
                        var table = cor20.CodeManagerTable;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            cor20CodeManagerTable = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return cor20CodeManagerTable;
            }
        }

        #endregion
        #region Cor20VTableFixups

        private object? cor20VTableFixups;

        public object? Cor20VTableFixups
        {
            get
            {
                if (cor20VTableFixups == null)
                {
                    var cor20 = Cor20Header;

                    if (cor20 != null)
                    {
                        var table = cor20.VTableFixups;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            cor20VTableFixups = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return cor20VTableFixups;
            }
        }

        #endregion
        #region Cor20ExportAddressTableJumps

        private object? cor20ExportAddressTableJumps;

        public object? Cor20ExportAddressTableJumps
        {
            get
            {
                if (cor20ExportAddressTableJumps == null)
                {
                    var cor20 = Cor20Header;

                    if (cor20 != null)
                    {
                        var table = cor20.ExportAddressTableJumps;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            cor20ExportAddressTableJumps = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return cor20ExportAddressTableJumps;
            }
        }

        #endregion

        //Managed Native Header is either an NGEN or ReadyToRun header, which are
        //handled separately below

        #region Cor20ManagedNativeHeader

        private IValue? cor20ManagedNativeHeader;

        public IValue? Cor20ManagedNativeHeader
        {
            get
            {
                if (cor20ManagedNativeHeader == null)
                {
                    var cor20 = Cor20Header;

                    //Files with a managed header should have COMIMAGE_FLAGS_IL_LIBRARY set. As per pedecoder.cpp,
                    //this name is a misnomer
                    if (cor20 != null && (cor20.Flags & COMIMAGE_FLAGS.IL_LIBRARY) != 0)
                    {
                        var table = cor20.ManagedNativeHeader;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            var sig = chunk.PeekUInt32(0);

                            switch (sig)
                            {
                                case CorCompileHeader.NGESignature:
                                    cor20ManagedNativeHeader = new CorCompileHeader(chunk);
                                    break;

                                case ReadyToRunHeader.R2RSignature:
                                    cor20ManagedNativeHeader = new ReadyToRunHeader(chunk);
                                    break;
                            }
                        }
                    }
                }

                return cor20ManagedNativeHeader;
            }
        }

        #endregion
#endif

        private ImageCorILMethod[]? ilMethods;

#if PEFAST
        public ImageCorILMethod[]? ILMethods
        {
            get
            {
                if (ilMethods == null)
                {
                    //For some reason referencing MethodDefTable causes a type load exception to occur in the JIT. I think it's because ClassLoader::LoadTypeHandlerForTypeKey_Body
                    //gets upset that we're doing a recursive type load, because the row and table types reference each other
                    var methodDefs = EcmaMetadata?.CompressedModelHeap?.MethodDefTable;

                    if (methodDefs == null)
                        return null;

                    var results = new List<ImageCorILMethod>();

                    foreach (var methodDef in methodDefs)
                    {
                        //Certain methods (such as interface methods) have an RVA of 0, and so do not
                        //have an IL method

                        var rva = methodDef.RVA;

                        if (rva == 0 || !TryGetValueChunkFromSection(rva, out var valueChunk))
                            continue;

                        var ilMethod = new ImageCorILMethod(valueChunk, out var isValid);

                        //You can have P/Invokes that say they have RVAs but these don't point to valid data
                        if (isValid)
                            results.Add(ilMethod);
                    }

                    ilMethods = results.ToArray();
                }

                return ilMethods;
            }
        }

        /// <summary>
        /// Gets the <see cref="ImageCorILMethod"/> that is associated with a given method token.
        /// </summary>
        /// <param name="methodDef">The <see cref="mdMethodDef"/> token of the method whose data should be retrieved.</param>
        /// <param name="ilMethod">The <see cref="ImageCorILMethod"/> that is associated with the specified method token.</param>
        /// <returns><see langword="true"/> if the RVA of the metadata row pointed to by <paramref name="methodDef"/> could be resolved to an <see cref="ImageCorILMethod"/>. Otherwise, <see langword="false"/>.</returns>
        public bool TryGetILMethod(mdMethodDef methodDef, out ImageCorILMethod ilMethod)
        {
            var methodDefs = EcmaMetadata?.CompressedModelHeap?.MethodDefTable;
            ilMethod = default;

            if (methodDefs == null)
                return false;

            if (methodDef.Rid > methodDefs.Count)
                return false;

            var row = methodDefs[methodDef.Rid];

            var rva = row.RVA;

            if (rva == 0 || !TryGetValueChunkFromSection(rva, out var valueChunk))
                return false;

            ilMethod = new ImageCorILMethod(valueChunk, out var isValid);

            if (isValid)
                return true;

            //You can have P/Invokes that say they have RVAs but these don't point to valid data
            ilMethod = default;
            return false;
        }
#else
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
                            //References are 1 based
                            for (var i = 1; i <= methods.Count; i++)
                            {
                                var method = methods[i];

                                //Some methods (like interfaces) have an RVA of 0

                                if (!TryGetOffset(method.RVA, out var offset))
                                    continue;

                                reader.Seek(offset);

                                var item = new ImageCorILMethod(reader, out var isValid);

                                if (isValid)
                                    ilMethods.Add(item);
                            }

                            this.ilMethods = ilMethods.ToArray();
                        }
                    }

                    SetRegionFlag(PERegionKind.ILMethods);
                }

                return ilMethods;
            }
        }

        private CompressedModelHeap? clrMetadata;

        public CompressedModelHeap? GetCLRMetadata()
        {
            if (clrMetadata == null)
            {
                _ = Cor20Header?.Metadata.Data;
            }

            return clrMetadata;
        }
#endif

        #endregion
        #endregion
        #region NGEN
#if PEFAST

        private CorCompileHeader? NgenHeader => Cor20ManagedNativeHeader as CorCompileHeader;

        #region NgenHelperTable

        private object? ngenHelperTable;

        public object? NgenHelperTable
        {
            get
            {
                if (ngenHelperTable == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.HelperTable;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenHelperTable = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return ngenHelperTable;
            }
        }

        #endregion
        #region NgenImportSections

        private object? ngenImportSections;

        public object? NgenImportSections
        {
            get
            {
                if (ngenImportSections == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.ImportSections;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenImportSections = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return ngenImportSections;
            }
        }

        #endregion
        #region NgenStubsData

        private object? ngenStubsData;

        public object? NgenStubsData
        {
            get
            {
                if (ngenStubsData == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.StubsData;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenStubsData = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return ngenStubsData;
            }
        }

        #endregion
        #region NgenVersionInfo

        private object? ngenVersionInfo;

        public object? NgenVersionInfo
        {
            get
            {
                if (ngenVersionInfo == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.VersionInfo;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenVersionInfo = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return ngenVersionInfo;
            }
        }

        #endregion
        #region NgenDependencies

        private object? ngenDependencies;

        public object? NgenDependencies
        {
            get
            {
                if (ngenDependencies == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.Dependencies;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenDependencies = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return ngenDependencies;
            }
        }

        #endregion
        #region NgenDebugMap

        private object? ngenDebugMap;

        public object? NgenDebugMap
        {
            get
            {
                if (ngenDebugMap == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.DebugMap;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenDebugMap = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return ngenDebugMap;
            }
        }

        #endregion
        #region NgenModuleImage

        private object? ngenModuleImage;

        public object? NgenModuleImage
        {
            get
            {
                if (ngenModuleImage == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.ModuleImage;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenModuleImage = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return ngenModuleImage;
            }
        }

        #endregion
        #region NgenCodeManagerTable

        private object? ngenCodeManagerTable;

        public object? NgenCodeManagerTable
        {
            get
            {
                if (ngenCodeManagerTable == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.CodeManagerTable;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenCodeManagerTable = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return ngenCodeManagerTable;
            }
        }

        #endregion
        #region NgenProfileDataList

        private object? ngenProfileDataList;

        public object? NgenProfileDataList
        {
            get
            {
                if (ngenProfileDataList == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.ProfileDataList;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenProfileDataList = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return ngenProfileDataList;
            }
        }

        #endregion
        #region NgenManifestMetaData

        private object? ngenManifestMetaData;

        public object? NgenManifestMetaData
        {
            get
            {
                if (ngenManifestMetaData == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.ManifestMetaData;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenManifestMetaData = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return ngenManifestMetaData;
            }
        }

        #endregion
        #region NgenVirtualSectionsTable

        private object? ngenVirtualSectionsTable;

        public object? NgenVirtualSectionsTable
        {
            get
            {
                if (ngenVirtualSectionsTable == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.VirtualSectionsTable;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenVirtualSectionsTable = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return ngenVirtualSectionsTable;
            }
        }

        #endregion
        #region NgenEEInfoTable

        private object? ngenEEInfoTable;

        public object? NgenEEInfoTable
        {
            get
            {
                if (ngenEEInfoTable == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.EEInfoTable;

                        if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenEEInfoTable = null;
                            throw new NotImplementedException();
                        }
                    }
                }

                return ngenEEInfoTable;
            }
        }

        #endregion
#endif
        #endregion
        #region ReadyToRun

#if PEFAST
        //Apparently it's possible that the R2R header might also be pointed to by exports. I don't know if it's possible
        //for it to _only_ be pointed to by exports
        public ReadyToRunHeader? ReadyToRunHeader => Cor20ManagedNativeHeader as ReadyToRunHeader;
#else
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ReadyToRunHeader? readyToRunHeader;

        public ReadyToRunHeader? ReadyToRunHeader
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.ReadyToRunHeader))
                {
                    var cor20Header = Cor20Header;

                    if (cor20Header != null && cor20Header.Flags.HasFlag(COMIMAGE_FLAGS.IL_LIBRARY))
                    {
                        if (TryGetOffset(cor20Header.ManagedNativeHeader.VirtualAddress, out var offset))
                        {
                            lock (readerLock)
                            {
                                reader.Seek(offset);

                                var signature = reader.ReadInt32();

                                if (signature == ReadyToRunHeader.R2RSignature)
                                    readyToRunHeader = new ReadyToRunHeader(reader, this, signature);
                            }
                        }
                    }

                    SetRegionFlag(PERegionKind.ReadyToRunHeader);
                }

                return readyToRunHeader;
            }
        }
#endif

        #endregion
        #region AppHost

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AppHostSignature? appHostSignature;

#if PEFAST
        public AppHostSignature? AppHostSignature
        {
            get
            {
                appHostSignature = null;
                Debug.Assert(appHostSignature == null); //Dummy use
                throw new NotImplementedException();
            }
        }
#else
        public AppHostSignature? AppHostSignature
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.AppHostSignature))
                {
                    //Scanning the entire DLL for the AppHost signature could be slow,
                    //so we don't want the Visual Studio debugger to automatically do this just
                    //because we looked at the properties of the PEFile
                    Debugger.NotifyOfCrossThreadDependency();

                    lock (readerLock)
                        appHostSignature = AppHostSignature.New(reader);

                    SetRegionFlag(PERegionKind.AppHostSignature);
                }

                return appHostSignature;
            }
        }
#endif

        #endregion
        #region ClrEngineMetrics

        private ClrEngineMetrics? clrEngineMetrics;
        private bool hasTriedClrEngineMetrics;

#if PEFAST
        public ClrEngineMetrics? ClrEngineMetrics
        {
            get
            {
                if (clrEngineMetrics == null && !hasTriedClrEngineMetrics)
                {
                    ImageExportDirectory.Export export = default;

                    //Doesn't seem like the base matters; if the base is 2, g_CLREngineMetrics is still at ordinal 2
                    //after factoring in ordinal + base (which is what export.Ordinal shows)
                    if (ExportTable?.TryGetExport("g_CLREngineMetrics", out export) == true && !export.ForwardOrAddress.IsForward && export.Ordinal == 2)
                    {
                        var rva = export.ForwardOrAddress.Address;

                        if (TryGetValueChunkFromSection(rva, out var valueChunk))
                            clrEngineMetrics = new ClrEngineMetrics(valueChunk);
                    }

                    hasTriedClrEngineMetrics = true;
                }

                return clrEngineMetrics;
            }
        }
#else
        public ClrEngineMetrics? ClrEngineMetrics
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.ClrEngineMetrics))
                {
                    ImageExportDirectory.Export export = default;

                    //Doesn't seem like the base matters; if the base is 2, g_CLREngineMetrics is still at ordinal 2
                    //after factoring in ordinal + base (which is what export.Ordinal shows)
                    if (ExportTable?.TryGetExport("g_CLREngineMetrics", out export) == true && !export.ForwardOrAddress.IsForward && export.Ordinal == 2)
                    {
                        var rva = export.ForwardOrAddress.Address;

                        if (TryGetOffset(rva, out var offset))
                        {
                            lock (readerLock)
                            {
                                reader.Seek(offset);

                                clrEngineMetrics = new ClrEngineMetrics(reader, this);
                            }
                        }
                    }

                    SetRegionFlag(PERegionKind.ClrEngineMetrics);
                }

                return clrEngineMetrics;
            }
        }
#endif

        #endregion
        #region Single File

        //If this is a single file .NET application, there should be a "DotNetRuntimeInfo" export

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RuntimeInfo? runtimeInfo;
        private bool hasTriedRuntimeInfo;

#if PEFAST
        public RuntimeInfo? RuntimeInfo
        {
            get
            {
                if (runtimeInfo == null && !hasTriedRuntimeInfo)
                {
                    ImageExportDirectory.Export export = default;

                    if (ExportTable?.TryGetExport("DotNetRuntimeInfo", out export) == true && !export.ForwardOrAddress.IsForward)
                    {
                        var rva = export.ForwardOrAddress.Address;

                        if (TryGetValueChunkFromSection(rva, out var valueChunk))
                            runtimeInfo = new RuntimeInfo(valueChunk);
                    }

                    hasTriedRuntimeInfo = true;
                }

                return runtimeInfo;
            }
        }
#else
        public RuntimeInfo? RuntimeInfo
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.RuntimeInfo))
                {
                    ImageExportDirectory.Export export = default;

                    if (ExportTable?.TryGetExport("DotNetRuntimeInfo", out export) == true && !export.ForwardOrAddress.IsForward)
                    {
                        if (TryGetOffset(export.ForwardOrAddress.Address, out var offset))
                        {
                            lock (readerLock)
                            {
                                reader.Seek(offset);

                                runtimeInfo = new RuntimeInfo(reader);
                            }
                        }
                    }

                    SetRegionFlag(PERegionKind.RuntimeInfo);
                }

                return runtimeInfo;
            }
        }
#endif

        #endregion
        #region Native AOT

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DotNetRuntimeDebugHeader? dotNetRuntimeDebugHeader;
        private bool hasTriedDotnetRuntimeDebugHeader;

#if PEFAST
        public DotNetRuntimeDebugHeader? DotNetRuntimeDebugHeader
        {
            get
            {
                if (dotNetRuntimeDebugHeader == null && !hasTriedDotnetRuntimeDebugHeader)
                {
                    ImageExportDirectory.Export export = default;

                    if (ExportTable?.TryGetExport("DotNetRuntimeDebugHeader", out export) == true && !export.ForwardOrAddress.IsForward)
                    {
                        if (TryGetValueChunkFromSection(export.ForwardOrAddress.Address, out var chunk))
                            dotNetRuntimeDebugHeader = new DotNetRuntimeDebugHeader(chunk);
                    }

                    hasTriedDotnetRuntimeDebugHeader = true;
                }

                return dotNetRuntimeDebugHeader;
            }
        }
#else
        public DotNetRuntimeDebugHeader? DotNetRuntimeDebugHeader
        {
            get
            {
                if (!HasRegionFlag(PERegionKind.DotNetRuntimeDebugHeader))
                {
                    ImageExportDirectory.Export export = default;

                    if (ExportTable?.TryGetExport("DotNetRuntimeDebugHeader", out export) == true && !export.ForwardOrAddress.IsForward)
                    {
                        if (TryGetOffset(export.ForwardOrAddress.Address, out var offset))
                        {
                            lock (readerLock)
                            {
                                reader.Seek(offset);

                                dotNetRuntimeDebugHeader = new DotNetRuntimeDebugHeader(reader, this);
                            }
                        }
                    }

                    SetRegionFlag(PERegionKind.DotNetRuntimeDebugHeader);
                }

                return dotNetRuntimeDebugHeader;
            }
        }
#endif

        #endregion

        /// <summary>
        /// Gets a <see cref="FileView"/> that allows visualizing the physical structure of the <see cref="PEFile"/>.
        /// </summary>
        /// <param name="mode">Specifies the addressing mode that should be used in the returned view. If this value is <see cref="ViewMode.Default"/>,
        /// <see cref="ViewMode.Virtual"/> or <see cref="ViewMode.Physical"/> will automatically be selected based on the value of <see cref="IsLoadedImage"/>.</param>
        /// <returns>A <see cref="FileView"/> that provides a view over the structure of the PE File.</returns>
        public FileView GetView(ViewMode mode = ViewMode.Default)
        {
#if PEFAST
            var writer = GetViewWriter(mode, null);
            ((IViewable) this).WriteView(writer);

            return (FileView) writer.Finalize();
#else
            //View may use Stream to read bytes
            lock (readerLock)
            {
                var writer = new PEViewWriter(this, reader, null, mode);
                ((IViewable) this).WriteView(writer);

                return (FileView) writer.Finalize();
            }
#endif
        }

        public FileView GetView<T>(ViewDisassembler<T> viewDisassembler, ViewMode mode = ViewMode.Default)
        {
#if PEFAST
            var writer = GetViewWriter(mode, viewDisassembler);

            ((IViewable) this).WriteView(writer);

            return (FileView) writer.Finalize();
#else
            //View may use Stream to read bytes
            lock (readerLock)
            {
                var writer = new PEViewWriter(this, reader, viewDisassembler, mode);
                ((IViewable) this).WriteView(writer);

                return (FileView) writer.Finalize();
            }
#endif
        }

        public unsafe IView GetView(IViewable viewable, ViewMode mode = ViewMode.Default)
        {
#if PEFAST
            var writer = GetViewWriter(mode, null);
            viewable.WriteView(writer);

            if (writer.Current.Count != 1)
                throw new NotImplementedException();

            return writer.Current[0];
#else
            lock (readerLock)
            {
                var writer = new PEViewWriter(this, reader, null, mode);
                viewable.WriteView(writer);

                if (writer.Current.Count != 1)
                    throw new NotImplementedException();

                return writer.Current[0];
            }
#endif
        }

#if PEFAST
        private unsafe PEViewWriter GetViewWriter(ViewMode mode, IViewDisassembler? viewDisassembler)
        {
            byte* pointer;
            int length;

            if (blockProvider is LocalMemoryBlockProvider l)
            {
                pointer = l.Pointer;
                length = (int) l.Length;
            }
            else
            {
                throw new NotImplementedException();
            }

            var writer = new PEViewWriter(this, pointer, length, viewDisassembler, mode);

            return writer;
        }

        //Provides MemoryBlock objects which encompass an area of a PEFile
        internal readonly IMemoryBlockProvider blockProvider;

        //A special MemoryBlock containing the PE File header. We bypass the IMemoryBlockProvider
        //and create this directly since we need a special MemoryBlock implementation with special behaviors
        //just to get things going
        private HeaderMemoryBlock headerBlock;
        private MemoryBlock[]? sectionBlocks;
        private bool disposed;

        private ExceptionHandlerContext? exceptionHandlerContext;

        internal ExceptionHandlerContext ExceptionHandlerContext
        {
            get
            {
                if (exceptionHandlerContext == null)
                    exceptionHandlerContext = new ExceptionHandlerContext(this);

                return exceptionHandlerContext;
            }
        }

        //PERF: benchmarks consistently show that when accessing a single member a single time,
        //it's faster to do lazy initialization of our core header types. However, once you start
        //accessing members multiple times, it quickly becomes faster to preload everything

        internal PEFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            IsLoadedImage = false;

            FileName = fileName;
            Name = Path.GetFileName(fileName);
            var localProvider = new LocalMemoryBlockProvider(mmf, this);
            blockProvider = localProvider;

            headerBlock = new LocalHeaderMemoryBlock(localProvider);

            InitializeHeaders();
        }

        //ctor for initializing PEFile from an IMemoryReader that reads remote memory
        private PEFile(IMemoryReader reader, long address)
        {
            IsLoadedImage = true;

            blockProvider = new RemoteMemoryBlockProvider(reader, address, this);

            //Will automatically demand
            headerBlock = new RemoteHeaderMemoryBlock(reader, address, blockProvider);

            InitializeHeaders();
        }

        private void InitializeHeaders()
        {
            dosHeader = new ImageDosHeader(new MemoryChunk(headerBlock, 0));
            ntHeaders = new ImageNtHeaders(new MemoryChunk(headerBlock, dosHeader.FileAddressOfNewExeHeader));

            //ImageOptionalHeader cannot be meaningfully read until the pointer size is known
            headerBlock.Is32Bit = ntHeaders.OptionalHeader.Magic == PEMagic.PE32;

            //If the header block was not big enough to store the size of the image, resize it before anyone has started using the PEFile.
            //If the PEFile is a memory mapped file, this is a no-op
            headerBlock.Resize(ntHeaders.OptionalHeader.SizeOfHeaders);
        }
#else
        private IFileReader reader;
        private object readerLock = new object();
        private volatile int flags;
        private bool disposed;

        internal IFileServices? Services { get; }

        internal PEFile(IFileReader reader)
        {
            IsLoadedImage = false;
            (this.reader, this.readerLock) = ((StreamFileReader) reader).CreateSubReader();
        }

        private PEFile(Stream stream, bool isLoadedImage, IFileServices? services)
        {
            //Reading strings from a FileStream is very slow, so use an MMF reader instead
 
            reader = new StreamFileReader(stream, readerLock);

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
#endif

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
        public bool TryGetDirectoryOffset(in ImageDataDirectory entry, out RawOffset offset, bool canCrossSectionBoundary)
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

        //We do not lock on this field; if we match, we match
        private ImageSectionHeader? lastUsedSection;

        /// <summary>
        /// Tries to get the physical offset within the image of a specified relative virtual address.
        /// </summary>
        /// <param name="rva">The relative virtual address within the image to translate.</param>
        /// <param name="offset">The translated physical address.</param>
        /// <returns>True if the RVA was translated to a physical offset, otherwise false.</returns>
        public bool TryGetOffset(RVA rva, out RawOffset offset)
        {
            //When we're reading data, we'll typically be seeking between data contained within the same section. As such, as an optimization
            //we can cache the last section we seeked to, and check whether that section contains our RVA
            if (lastUsedSection != null && lastUsedSection.Value.VirtualAddress != 0)
            {
                var local = lastUsedSection.Value;

                var start = local.VirtualAddress;
                var end = local.VirtualAddress + local.VirtualSize;

                if (start <= rva && rva < end)
                {
                    if (IsLoadedImage)
                    {
                        offset = rva;
                    }
                    else
                    {
                        var diff = rva - local.VirtualAddress;

                        //The location this value points to does not exist in the unloaded image; the directory that the address
                        //points to will be expanded when loaded into memory
                        if (diff > local.SizeOfRawData)
                        {
                            offset = default;
                            return false;
                        }

                        offset = local.PointerToRawData + diff;
                    }

                    return true;
                }
            }

            var sectionIndex = GetSectionContainingRVA(rva);

            if (sectionIndex < 0)
            {
                offset = (RawOffset) (-1);
                return false;
            }

            var section = SectionHeaders[sectionIndex];
            lastUsedSection = section;

            if (IsLoadedImage)
            {
                offset = rva;
            }
            else
            {
                var diff = rva - section.VirtualAddress;

                //The location this value points to does not exist in the unloaded image; the directory that the address
                //points to will be expanded when loaded into memory
                if (diff > section.SizeOfRawData)
                {
                    offset = default;
                    return false;
                }

                offset = section.PointerToRawData + diff;
            }

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

            //Store headers locally so that we don't need to keep checking that the headers are loaded each time we touch the headers
            var headers = SectionHeaders;

            Debug.Assert(headers != null);

            for (var i = 0; i < headers!.Length; i++)
            {
                var start = headers[i].VirtualAddress;
                var end = headers[i].VirtualAddress + headers[i].VirtualSize;

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

            //Store headers locally so that we don't need to keep checking that the headers are loaded each time we touch the headers
            var headers = SectionHeaders;

            for (var i = 0; i < headers.Length; i++)
            {
                var start = headers[i].PointerToRawData;
                var end = headers[i].PointerToRawData + headers[i].VirtualSize;

                if (start <= offset && offset < end)
                    return i;
            }

            return -1;
        }

#if PEFAST
        private bool TryGetDirectoryChunk(in ImageDataDirectory entry, out MemoryChunk chunk)
        {
            if (TryGetSectionBlockFromRVA(entry.VirtualAddress, out var block, out var relativeOffset))
            {
                chunk = new MemoryChunk(block!, relativeOffset);
                return true;
            }

            chunk = default;
            return false;
        }

        internal bool TryGetValueChunkFromSectionOrHeader(int rva, out MemoryChunk chunk)
        {
            if (rva == 0)
            {
                chunk = default;
                return false;
            }

            if (rva < OptionalHeader.SizeOfHeaders)
            {
                chunk = new MemoryChunk(headerBlock, 0);
                return true;
            }

            return TryGetValueChunkFromSection(rva, out chunk);
        }

        internal bool TryGetValueChunkFromSection(int rva, out MemoryChunk chunk)
        {
            if (TryGetSectionBlockFromRVA(rva, out var block, out var relativeOffset))
            {
                //We don't know what memory they want, so I guess we need to demand all of it?
                block!.Demand();

                chunk = new MemoryChunk(block!, relativeOffset);
                return true;
            }

            chunk = default;
            return false;
        }

        internal bool TryGetRVARelativeValueChunk(int rva, int offset, out MemoryChunk chunk)
        {
            if (TryGetValueChunkFromSection(rva, out chunk))
            {
                chunk = chunk.Slice(offset);
                return true;
            }

            return false;
        }

        internal bool TryGetValueChunkFromPhysicalOffset(int offset, out MemoryChunk chunk)
        {
            if (TryGetSectionBlockFromOffset(offset, out var block, out var relativeOffset))
            {
                chunk = new MemoryChunk(block!, relativeOffset);
                return true;
            }

            if (IsLoadedImage)
            {
                //The value isn't part of any known section, but if it's part of the header, we can do something with that
                if (offset < OptionalHeader.SizeOfHeaders)
                {
                    chunk = new MemoryChunk(headerBlock, offset);
                    return true;
                }
            }
            else
            {
                //In an unloaded image, our header block technically provides access to the entire module
                chunk = new MemoryChunk(headerBlock, offset);
                return true;
            }

            chunk = default;
            return false;
        }

        internal bool TryGetSectionBlockFromRVA(int rva, out MemoryBlock? block, out int relativeOffset)
        {
            //Will check for 0
            if (!TryGetSectionContainingRVA(rva, out var sectionIndex, out var section))
            {
                block = null;
                relativeOffset = default;
                return false;
            }

            block = GetSectionBlock(sectionIndex, section);

            //When it's a loaded image, block.Address is the section.VirtualAddress.
            //Otherwise, its the PointerToRawData. But this is an issue, because we need to calculate
            //the reltive offset into the section, which involves looking at the VirtualAddress, so we must
            //do that calculation here
            relativeOffset = rva - section.VirtualAddress;

            return true;
        }

        internal bool TryGetSectionBlockFromOffset(int offset, out MemoryBlock? block, out int relativeOffset)
        {
            if (!TryGetSectionContainingOffset(offset, out var sectionIndex, out var section))
            {
                //The offset does not belong to any known section, thereby making it part of the header or overlay.
                //In loaded modules, the overlay is not loaded into memory. If the caller wants to use the header block
                //in order to access the overlay in an unloaded module, they need to decide to do that; it's not our responsibility

                block = null;
                relativeOffset = default;
                return false;
            }

            block = GetSectionBlock(sectionIndex, section);

            relativeOffset = offset - section.PointerToRawData;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MemoryBlock GetSectionBlock(int sectionIndex, in ImageSectionHeader section)
        {
            if (sectionBlocks == null)
                Interlocked.CompareExchange(ref sectionBlocks, new MemoryBlock[ntHeaders.FileHeader.NumberOfSections], null);

            var block = Volatile.Read(ref sectionBlocks[sectionIndex]);

            if (block == null)
            {
                if (IsLoadedImage)
                {
                    block = blockProvider.CreateBlock(section.VirtualAddress, section.VirtualSize);
                }
                else
                {
                    //The file is on disk. In order to align sections,
                    //the size of raw data might be padded out. That's OK;
                    //we want to reflect reality

                    block = blockProvider.CreateBlock(section.PointerToRawData, section.SizeOfRawData);
                }

                if (Interlocked.CompareExchange(ref sectionBlocks[sectionIndex], block, null!) != null)
                {
                    //Another thread tried to access the section at the same time, and wrote their value into our block list first.
                    //Dispose the buffer we created
                    block.Dispose();

                    //The existing block won
                    block = sectionBlocks[sectionIndex];
                }
            }

            return block;
        }

        private bool TryGetSectionContainingRVA(int rva, out int index, out ImageSectionHeader header)
        {
            if (rva == 0)
            {
                index = default;
                header = default;
                return false;
            }

            //I don't think we can use lastUsedSection here, as we need to return both the section and the index

            //Store headers locally so that we don't need to keep checking that the headers are loaded each time we touch the headers
            var headers = SectionHeaders;

            Debug.Assert(headers != null);

            for (var i = 0; i < headers!.Length; i++)
            {
                header = headers[i];

                var start = header.VirtualAddress;
                var end = start + header.VirtualSize;

                if (start <= rva && rva < end)
                {
                    index = i;
                    return true;
                }
            }

            index = default;
            header = default;
            return false;
        }

        private bool TryGetSectionContainingOffset(int offset, out int index, out ImageSectionHeader header)
        {
            if (offset == 0)
            {
                index = default;
                header = default;
                return false;
            }

            //I don't think we can use lastUsedSection here, as we need to return both the section and the index

            //Store headers locally so that we don't need to keep checking that the headers are loaded each time we touch the headers
            var headers = SectionHeaders;

            Debug.Assert(headers != null);

            for (var i = 0; i < headers!.Length; i++)
            {
                header = headers[i];

                var start = header.PointerToRawData;
                var end = start + header.SizeOfRawData;

                if (start <= offset && offset < end)
                {
                    index = i;
                    return true;
                }
            }

            index = default;
            header = default;
            return false;
        }
#endif

        #endregion

#if !PEFAST
        /// <summary>
        /// Invalidates all regions that have been read from the Portable Executable file,
        /// requiring that they be re-read when they are next accessed.
        /// </summary>
        public void Invalidate() => SetRegionFlag(0);

        internal delegate T WithReaderCallback<T>(IFileReader reader, PEFile peFile);

        internal T WithReader<T>(RawOffset offset, WithReaderCallback<T> callback)
        {
            lock (readerLock)
            {
                reader.Seek(offset);

                return callback(reader, this);
            }
        }

        /// <summary>
        /// Invalidates one or more regions that have been read from the Portable Executable file,
        /// requiring that they be re-read when they are next accessed.
        /// </summary>
        /// <param name="regions">The regions to invalidate.</param>
        public void Invalidate(PERegionKind regions) =>
            SetRegionFlag(~regions);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            writer.WriteGlobal(DosHeader);
            writer.WriteDosStub(DosStub);
            writer.WriteGlobal(RichHeader);
            writer.WriteGlobal(NtHeaders);
            writer.WriteGlobal(SectionHeaders);

            writer.WriteGlobal(ExportTable);

#if !PEFAST
            writer.WriteUniqueGlobal(ImportAddressTable); //Write this before the Import Table as we want IAT entries to be in a data directory, not a logical region
#endif
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

            writer.WriteGlobal(ILMethods);

            //writer.WriteGlobal(ReadyToRunHeader);

#if !PEFAST
            writer.WriteGlobal(AppHostSignature);
#endif
            writer.WriteGlobal(ClrEngineMetrics);
            writer.WriteGlobal(RuntimeInfo);
            writer.WriteGlobal(DotNetRuntimeDebugHeader);
        }

        public void Dispose()
        {
            Dispose(true);
        }

#if PEFAST
        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                headerBlock.Dispose();

                if (sectionBlocks != null)
                {
                    foreach (var block in sectionBlocks)
                    {
                        if (block != null)
                            block.Dispose();
                    }

                    sectionBlocks = null;
                }

                (blockProvider as IDisposable)?.Dispose();

                //This is a bit of a gotcha! If you declare a finalizer, it won't be GC'd until the finalizer thread processes it.
                //Which means if you're generating a lot of objects, the finalizer thread might not be able to keep up
                GC.SuppressFinalize(this);
            }

            disposed = true;
        }
#else
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                (reader as IDisposable)?.Dispose();

                //This is a bit of a gotcha! If you declare a finalizer, it won't be GC'd until the finalizer thread processes it.
                //Which means if you're generating a lot of objects, the finalizer thread might not be able to keep up
                GC.SuppressFinalize(this);
            }

            disposed = true;
        }
#endif

        public override string ToString()
        {
            if (Name != null)
                return Name.ToString();

            return base.ToString();
        }
    }
}
