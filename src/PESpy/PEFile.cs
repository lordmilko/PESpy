using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using ClrDebug;
using PESpy.Native;
using PESpy.View;
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

        public ReadyToRunHeader ReadyToRunHeader => peFile.ReadyToRunHeader;

        public AppHostSignature? AppHostSignature => peFile.AppHostSignature;

        public ClrEngineMetrics? ClrEngineMetrics => peFile.ClrEngineMetrics;

        public RuntimeInfo? RuntimeInfo => peFile.RuntimeInfo;

        public DotNetRuntimeDebugHeader? DotNetRuntimeDebugHeader => peFile.DotNetRuntimeDebugHeader;
    }

    public interface IMetadataCallback
    {
        void NotifyCompressedModel(CompressedModelHeap data);
        void NotifyStringPool(StringHeap data);
        void NotifyUserStringPool(UserStringHeap data);
        void NotifyBlobPool(BlobHeap data);
        void NotifyGuidPool(GuidHeap data);
        void NotifyPdb(PdbHeap data);
    }

    [DebuggerTypeProxy(typeof(PEFileDebugView))]
    public class PEFile : IViewable, IMetadataCallback, IDisposable
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

            return new PEFile(fs);
        }

        /// <summary>
        /// Reads a <see cref="PEFile"/> from a module contained in a remote process.
        /// </summary>
        /// <param name="hProcess">A handle to the process containing the module that should be read.</param>
        /// <param name="moduleBase">The base address of the module in the remote process that should be read.</param>
        /// <returns>A <see cref="PEFile"/> that provides access to the contents of the specified module.</returns>
        public static PEFile FromProcess(IntPtr hProcess, long moduleBase) =>
            new PEFile(new RemoteMemoryReader(hProcess), moduleBase);

        public static PEFile FromStream(Stream stream, bool isLoadedImage)
        {
            //If it's a FileStream, implicitly it's not a loaded image
            if (stream is FileStream fs)
            {
                return new PEFile(fs);
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
        public ref readonly ByteBlob DosStub => throw new NotImplementedException();
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

        /// <summary>
        /// Gets the undocumented Rich Header which describes the build environment that was used to create the file.<para/>
        /// If the file does not have a Rich Header, this property returns <see langword="null"/>.
        /// </summary>
#if PEFAST
        public RichHeader? RichHeader => throw new NotImplementedException();
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
        public ref readonly ImageFileHeader FileHeader => ref ntHeaders.FileHeader;
#else
        public ImageFileHeader FileHeader => NtHeaders.FileHeader; //Cannot be ref readonly
#endif

        /// <summary>
        /// Gets the the <see cref="IMAGE_NT_HEADERS.OptionalHeader"/> field that represents the optional header of the image.
        /// </summary>
#if PEFAST
        public ref readonly ImageOptionalHeader OptionalHeader => ref ntHeaders.OptionalHeader;
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
        /// Gets the export table, containing all exports present in the image.<para/>
        /// If the image does not have an exports table, this property returns <see langword="null"/>.
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
        public ImageImportDescriptor[]? ImportTable => throw new NotImplementedException();
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
        public ImageResourceDirectory? ResourceDirectory
        {
            get
            {
                //if (resourceDirectory == null)
                //{
                //    var resourceTableDirectory = OptionalHeader.ResourceTableDirectory;

                //    if (resourceTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(resourceTableDirectory, out var chunk))
                //    {
                //        chunk.Demand(resourceTableDirectory.VirtualAddress, ImageResourceDirectory.StructSize);
                //        var local = new ImageResourceDirectory(chunk);

                //        //Since this property returns a reference, we should always return the same object
                //        Interlocked.CompareExchange(ref resourceDirectory, local, null);
                //    }
                //}

                //return resourceDirectory;
                throw new NotImplementedException();
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
        public RuntimeFunction[]? ExceptionTable => throw new NotImplementedException();
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

#if PEFAST
        public WinCertificate[]? SecurityTable => throw new NotImplementedException();
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
        public ImageBaseRelocation[]? BaseRelocationTable => throw new NotImplementedException();
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
        public ImageTlsDirectory? TlsDirectory => throw new NotImplementedException();
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
        public ImageLoadConfigDirectory? LoadConfigTable => throw new NotImplementedException();
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
        public ImageBoundImportDescriptor[]? BoundImportTable => throw new NotImplementedException();
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
        public ImageThunkData[]? ImportAddressTable => throw new NotImplementedException();
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
        public ImageDelayLoadDescriptor[]? DelayImportTable => throw new NotImplementedException();
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
        public ImageCor20Header? Cor20Header => throw new NotImplementedException();
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

        private ImageCorILMethod[]? ilMethods;

#if PEFAST
        public ImageCorILMethod[]? ILMethods => throw new NotImplementedException();
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
        #region ReadyToRun

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ReadyToRunHeader? readyToRunHeader;

#if PEFAST
        public ReadyToRunHeader? ReadyToRunHeader => throw new NotImplementedException();
#else
        public ReadyToRunHeader? ReadyToRunHeader
        {
        #region AppHost

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AppHostSignature? appHostSignature;

#if PEFAST
        public AppHostSignature? AppHostSignature => throw new NotImplementedException();
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

#if PEFAST
        public ClrEngineMetrics? ClrEngineMetrics => throw new NotImplementedException();
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

#if PEFAST
        public RuntimeInfo? RuntimeInfo => throw new NotImplementedException();
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

#if PEFAST
        public DotNetRuntimeDebugHeader? DotNetRuntimeDebugHeader => throw new NotImplementedException();
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

                                dotNetRuntimeDebugHeader = new DotNetRuntimeDebugHeader(reader, OptionalHeader.Magic == PEMagic.PE32);
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
                var writer = new PEViewWriter(this, reader, mode);
                ((IViewable) this).WriteView(writer);

                return (PEFileView) writer.Finalize();
            }
        }

        public IView GetView(IViewable viewable, ViewMode mode = ViewMode.Default)
        {
            lock (readerLock)
            {
                var writer = new PEViewWriter(this, reader, mode);
                viewable.WriteView(writer);

                if (writer.Current.Count != 1)
                    throw new NotImplementedException();

                return writer.Current[0];
            }
        }
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

            if (stream is FileStream fs && false)
                reader = new MemoryMappedFileReader(fs, readerLock); //todo: do we need to set isloaded to true if we do this? i think no, cos nobody specifies sec_image, not even c#'s memorymappedfile
            else
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
        private ImageSectionHeader lastUsedSection;

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
            if (lastUsedSection.VirtualAddress != 0)
            {
                var local = lastUsedSection;

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

            for (var i = 0; i < headers.Length; i++)
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
#if PEFAST
            throw new NotImplementedException();
#else
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

#if PEFAST
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                (blockProvider as IDisposable)?.Dispose();

                //This is a bit of a gotcha! If you declare a finalizer, it won't be GC'd until the finalizer thread processes it.
                //Which means if you're generating a lot of objects, the finalizer thread might not be able to keep up
                GC.SuppressFinalize(this);
            }
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
    }
}
