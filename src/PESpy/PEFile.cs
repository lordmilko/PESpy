using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using ClrDebug;
using PESpy.Native;
using PESpy.View;
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
        public SymStoreKey[] SymStoreKeys => peFile.SymStoreKeys;

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
        public RuntimeFunctionList ExceptionTable => peFile.ExceptionTable;
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
    public class PEFile : IFile, IFileWithCodeViewData, IViewable, IDisposable
    {
        #region Static

        //Note: when it comes to building PE files, we can't have an API for creating an "Empty" PDB, because we don't have a way to denote that stuff like the DOS Header
        //might not exist. Also, I think we do need a PEFileBuilder, because when we add/remove items we may have to shift things around, so we probably need to rewrite the whole PE

        /// <summary>
        /// Reads a <see cref="PEFile"/> from a file on disk.
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A <see cref="PEFile"/> that provides access to the contents of the specified file.</returns>
        public static PEFile FromFile(string path)
        {
            //Opening the file and creating the MMF, without doing anything else, allocates 1.07KB
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new PEFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Reads a <see cref="PEFile"/> from a module contained in a remote process.
        /// </summary>
        /// <param name="hProcess">A handle to the process containing the module that should be read.</param>
        /// <param name="moduleBase">The base address of the module in the remote process that should be read.</param>
        /// <param name="isLoaded">Whether the PE File has been processed by the operating system loader.</param>
        /// <returns>A <see cref="PEFile"/> that provides access to the contents of the specified module.</returns>
        public static unsafe PEFile FromProcess(IntPtr hProcess, IntPtr moduleBase, bool isLoaded = true) =>
            new PEFile(new RemoteMemoryReader(hProcess), (long) (void*) moduleBase, isLoaded);

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
                    mmf.Dispose();

                    throw;
                }
            }

            return new PEFile(new StreamMemoryReader(stream), stream.Position, isLoadedImage);
        }

        #endregion

        /// <summary>
        /// Gets whether the image has been mapped into the address space by the operating system loader, indicating that sections have been laid out according to their RVAs.<para/>
        /// Modules may be memory mapped without having been processed by the loader, in which case they should be processed as if they exist on disk.
        /// </summary>
        public bool IsLoadedImage { get; init; }

        private SymStoreKey[]? symStoreKeys;

        public SymStoreKey[] SymStoreKeys
        {
            get
            {
                if (symStoreKeys == null)
                {
                    using var results = new PooledList<SymStoreKey>();

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
                                        results.Add(SymStoreKey.FromMisc(data.Data.ToString(), FileHeader.TimeDateStamp, OptionalHeader.SizeOfImage));

                                    break;
                                }
                            }
                        }
                    }

                    var runtimeInfo = RuntimeInfo;

                    if (runtimeInfo != null)
                    {
                        var runtimeModuleIndex = runtimeInfo.RuntimeModuleIndex;
                        var dacModuleIndex = runtimeInfo.DacModuleIndex;
                        var dbiModuleIndex = runtimeInfo.DbiModuleIndex;

                        results.Add(SymStoreKey.FromPE("coreclr.dll", runtimeModuleIndex.TimeStamp, runtimeModuleIndex.ImageSize, SymStoreKeyKind.CLR));
                        results.Add(SymStoreKey.FromPE("mscordaccore.dll", dacModuleIndex.TimeStamp, dacModuleIndex.ImageSize, SymStoreKeyKind.DAC));
                        results.Add(SymStoreKey.FromPE("mscordbi.dll", dbiModuleIndex.TimeStamp, dbiModuleIndex.ImageSize, SymStoreKeyKind.DBI));
                    }

                    symStoreKeys = results.ToArray();
                }

                return symStoreKeys;
            }
        }

        public SymStoreKey GetSymStoreKey(SymStoreKeyKind kind)
        {
            if (!TryGetSymStoreKey(kind, out var key))
                throw new InvalidOperationException($"Could not get a SymStoreKey of type '{kind}'");

            return key;
        }

        public bool TryGetSymStoreKey(SymStoreKeyKind kind, out SymStoreKey key)
        {
            key = default;

            switch (kind)
            {
                case SymStoreKeyKind.PE:
                    if (Name == null)
                        return false;

                    key = SymStoreKey.FromPE(Name, FileHeader.TimeDateStamp, OptionalHeader.SizeOfImage);
                    return true;

                case SymStoreKeyKind.PDB:
                    {
                        var debugTable = DebugTable;

                        if (debugTable == null)
                            return false;

                        for (var i = 0; i < debugTable.Length; i++)
                        {
                            ref var debugDir = ref debugTable[i];

                            if (debugDir.Type == ImageDebugType.CodeView)
                            {
                                if (debugDir.Data is ICodeViewPDB p)
                                {
                                    switch (p.Signature)
                                    {
                                        case CodeViewSig.NB10:
                                            key = SymStoreKey.FromNB10((NB10I) p);
                                            return true;

                                        case CodeViewSig.RSDS:
                                            key = SymStoreKey.FromRSDSI((RSDSI) p);
                                            return true;

                                        default:
                                            throw new NotImplementedException($"Don't know how to handle {nameof(CodeViewSig)} '{p.Signature}'");
                                    }
                                }
                            }
                        }

                        return false;
                    }

                case SymStoreKeyKind.DBG:
                    {
                        var debugTable = DebugTable;

                        if (debugTable == null)
                            return false;

                        for (var i = 0; i < debugTable.Length; i++)
                        {
                            ref var debugDir = ref debugTable[i];

                            if (debugDir.Type == ImageDebugType.Misc)
                            {
                                var data = (ImageDebugMisc) debugDir.Data!;
                                key = SymStoreKey.FromMisc(data.Data.ToString(), FileHeader.TimeDateStamp, OptionalHeader.SizeOfImage);
                                return true;
                            }
                        }

                        return false;
                    }

                case SymStoreKeyKind.CLR:
                {
                    var runtimeInfo = RuntimeInfo;

                    if (runtimeInfo == null)
                        return false;

                    var runtimeModuleIndex = runtimeInfo.RuntimeModuleIndex;

                    key = SymStoreKey.FromPE("coreclr.dll", runtimeModuleIndex.TimeStamp, runtimeModuleIndex.ImageSize, SymStoreKeyKind.CLR);
                    return true;
                }

                case SymStoreKeyKind.DAC:
                {
                    var runtimeInfo = RuntimeInfo;

                    if (runtimeInfo == null)
                        return false;

                    var dacModuleIndex = runtimeInfo.DacModuleIndex;

                    key = SymStoreKey.FromPE("mscordaccore.dll", dacModuleIndex.TimeStamp, dacModuleIndex.ImageSize, SymStoreKeyKind.DAC);
                    return true;
                }

                case SymStoreKeyKind.DBI:
                {
                    var runtimeInfo = RuntimeInfo;

                    if (runtimeInfo == null)
                        return false;

                    var dbiModuleIndex = runtimeInfo.DbiModuleIndex;

                    key = SymStoreKey.FromPE("mscordbi.dll", dbiModuleIndex.TimeStamp, dbiModuleIndex.ImageSize, SymStoreKeyKind.DBI);
                    return true;
                }

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(SymStoreKeyKind)} '{kind}'.");
            }
        }

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.PE;

        public int Length => blockProvider is LocalMemoryBlockProvider l ? (int) l.Length : (int) OptionalHeader.SizeOfImage;

        public bool Is32Bit => headerBlock.Is32Bit;

        #region DosHeader

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageDosHeader dosHeader;

        /// <summary>
        /// Gets the MS-DOS 2.0 compatible <see cref="IMAGE_DOS_HEADER"/>.
        /// </summary>
        public ref readonly ImageDosHeader DosHeader => ref dosHeader;

        #endregion
        #region DosStub

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ByteBlob dosStub;

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

        #endregion
        #region RichHeader

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RichHeader? richHeader;
        private bool hasTriedRichHeader;

        /// <summary>
        /// Gets the undocumented Rich Header which describes the build environment that was used to create the file.<para/>
        /// If the file does not have a Rich Header, this property returns <see langword="null"/>.
        /// </summary>
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

        #endregion
        #region NtHeaders

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageNtHeaders ntHeaders;

        public ref readonly ImageNtHeaders NtHeaders => ref ntHeaders;

        /// <summary>
        /// Gets the <see cref="IMAGE_NT_HEADERS.FileHeader"/> field that represents the file header of the image.
        /// </summary>
        public ImageFileHeader FileHeader => ntHeaders.FileHeader;

        /// <summary>
        /// Gets the the <see cref="IMAGE_NT_HEADERS.OptionalHeader"/> field that represents the optional header of the image.
        /// </summary>
        public ImageOptionalHeader OptionalHeader => ntHeaders.OptionalHeader;

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
                if (sectionHeaders == null)
                {
                    var numberOfSections = ntHeaders.FileHeader.NumberOfSections;

                    var list = new ImageSectionHeader[numberOfSections];

                    var offset = dosHeader.FileAddressOfNewExeHeader + NtHeaders.StructSize(headerBlock.Is32Bit);

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

        #endregion
        #region Directories
        #region Export Table (0)

        private ImageExportDirectory? exportTable;

        /// <summary>
        /// Gets the export table pointed to by <see cref="ImageOptionalHeader.ExportTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_EXPORT), containing all exports present in the image.<para/>
        /// If the image does not have an export table, this property returns <see langword="null"/>.
        /// </summary>
        public ImageExportDirectory? ExportTable
        {
            get
            {
                if (exportTable == null)
                {
                    var exportTableDirectory = OptionalHeader.ExportTableDirectory;

                    if (exportTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(exportTableDirectory, out var chunk))
                    {
                        var local = new ImageExportDirectory(chunk);

                        //Since this property returns a reference, we should always return the same object
                        Interlocked.CompareExchange(ref exportTable, local, null);
                    }
                }

                return exportTable;
            }
        }

        #endregion
        #region Import Table (1)

        private ImageImportDescriptor[]? importTable;

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
                        //I don't know if we're guaranteed to fill up the entire ImportTableDirectory with
                        //ImageImportDescriptor objects, or if there's other stuff in there too. I feel like
                        //the latter is the case, as such we can't calculate exactly how many entries we'll have
                        using var results = new PooledList<ImageImportDescriptor>();

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

        #endregion
        #region Resource Directory (2)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageResourceDirectory? resourceDirectory;

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
                        var local = new ImageResourceDirectory(chunk, resourceTableDirectory.VirtualAddress, null);

                        //Since this property returns a reference, we should always return the same object
                        Interlocked.CompareExchange(ref resourceDirectory, local, null);
                    }
                }

                return resourceDirectory;
            }
        }

        #endregion
        #region Exception Table (3)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RuntimeFunction[]? exceptionTable;

#if PEFAST
        /// <summary>
        /// Gets the exception table pointed to by <see cref="ImageOptionalHeader.ExceptionTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_EXCEPTION) containing information used to unwind stack frames during exception handling.<para/>
        /// If the image does not have an exception table, this property returns <see langword="null"/>.
        /// </summary>
        public RuntimeFunctionList ExceptionTable
        {
            get
            {
                var exceptionTableDirectory = OptionalHeader.ExceptionTableDirectory;

                if (exceptionTableDirectory.VirtualAddress != 0 && TryGetDirectoryChunk(exceptionTableDirectory, out var chunk))
                {
                    var numEntries = OptionalHeader.ExceptionTableDirectory.Size / RuntimeFunction.StructSize;

                    return new RuntimeFunctionList(numEntries, chunk);
                }

                return default;
            }
        }

        #endregion
        #region Security Table (4)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private WinCertificate[]? securityTable;

        /// <summary>
        /// Gets the certificates contained in the Security Table pointed to by <see cref="ImageOptionalHeader.SecurityTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_SECURITY),
        /// or <see langword="null"/> if the Security Table was not present or did not point to a valid location.<para/>
        /// The Security Table is typically contained in the Overlay (the area beyond the last section) and as such is not present when <see cref="IsLoadedImage"/> is <see langword="true"/>.
        /// </summary>
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

                        var end = securityTableDirectory.Size;

                        using var results = new PooledList<WinCertificate>();

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

        #endregion
        #region Base Relocation Table (5)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageBaseRelocation[]? baseRelocationTable;

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
                    var end = OptionalHeader.BaseRelocationTableDirectory.Size;

                    using var results = new PooledList<ImageBaseRelocation>();

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

        #endregion
        #region Debug Table (6)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageDebugDirectory[]? debugTable;

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

        #endregion
        #region Copyright Table (7)

        #endregion
        #region Global Pointer Table (8)

        #endregion
        #region Thread Local Storage Table (9)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageTlsDirectory? tlsDirectory;

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

        #endregion
        #region Load Config Table (10)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageLoadConfigDirectory? loadConfigTable;

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

        #endregion
        #region Bound Import Table (11)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageBoundImportDescriptor[]? boundImportTable;

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

                        using var results = new PooledList<ImageBoundImportDescriptor>();

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

        #endregion
        #region Import Address Table (12)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageThunkData[]? importAddressTable;

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

        #endregion
        #region Delay Import Table (13)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageDelayLoadDescriptor[]? delayImportTable;

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
                        using var results = new PooledList<ImageDelayLoadDescriptor>();

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

        #endregion
        #region Cor20Header (14)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageCor20Header? cor20Header;

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

        public unsafe bool TryGetRawMetadata(out byte* metadata, out int length)
        {
            var cor20 = Cor20Header;

            if (cor20 != null)
            {
                var table = cor20.Metadata;

                if (table.VirtualAddress != 0 && TryGetDirectoryChunk(table, out var chunk))
                {
                    metadata = chunk.Pointer;
                    length = table.Size;
                    return true;
                }
            }

            metadata = default;
            length = default;
            return false;
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

        private ImageCorILMethod[]? ilMethods;

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

                    using var results = new PooledList<ImageCorILMethod>();

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

        #endregion
        #endregion
        #region NGEN

        public CorCompileHeader? NgenHeader => Cor20ManagedNativeHeader as CorCompileHeader;

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
        #endregion
        #region ReadyToRun

        //Apparently it's possible that the R2R header might also be pointed to by exports. I don't know if it's possible
        //for it to _only_ be pointed to by exports
        public ReadyToRunHeader? ReadyToRunHeader => Cor20ManagedNativeHeader as ReadyToRunHeader;

        #endregion
        #region AppHost

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AppHostSignature? appHostSignature;

        private bool hasTriedAppHostSignature;

        public unsafe AppHostSignature? AppHostSignature
        {
            get
            {
                if (appHostSignature == null && !hasTriedAppHostSignature)
                {
                    //Scanning the entire DLL for the AppHost signature could be slow,
                    //so we don't want the Visual Studio debugger to automatically do this just
                    //because we looked at the properties of the PEFile
                    Debugger.NotifyOfCrossThreadDependency();

                    GetRawPointer(out var pointer, out var length);

                    appHostSignature = AppHostSignature.New(pointer, length, headerBlock);
                    hasTriedAppHostSignature = true;
                }

                return appHostSignature;
            }
        }

        #endregion
        #region ClrEngineMetrics

        private ClrEngineMetrics? clrEngineMetrics;
        private bool hasTriedClrEngineMetrics;

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

        #endregion
        #region Single File

        //If this is a single file .NET application, there should be a "DotNetRuntimeInfo" export

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RuntimeInfo? runtimeInfo;
        private bool hasTriedRuntimeInfo;

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
                        {
                            runtimeInfo = new RuntimeInfo(valueChunk);

                            if (runtimeInfo.Signature != "DotnetRuntimeInfo")
                                runtimeInfo = default;
                        }
                    }

                    hasTriedRuntimeInfo = true;
                }

                return runtimeInfo;
            }
        }

        #endregion
        #region Native AOT

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DotNetRuntimeDebugHeader? dotNetRuntimeDebugHeader;
        private bool hasTriedDotnetRuntimeDebugHeader;

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

        #endregion

        /// <summary>
        /// Gets a <see cref="FileView"/> that allows visualizing the physical structure of the <see cref="PEFile"/>.
        /// </summary>
        /// <param name="mode">Specifies the addressing mode that should be used in the returned view. If this value is <see cref="ViewMode.Default"/>,
        /// <see cref="ViewMode.Virtual"/> or <see cref="ViewMode.Physical"/> will automatically be selected based on the value of <see cref="IsLoadedImage"/>.</param>
        /// <returns>A <see cref="FileView"/> that provides a view over the structure of the PE File.</returns>
        public FileView GetView(ViewMode mode = ViewMode.Default)
        {
            var writer = GetViewWriter(mode, null);
            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        public FileView GetView<T>(ViewDisassembler<T> viewDisassembler, ViewMode mode = ViewMode.Default)
        {
            var writer = GetViewWriter(mode, viewDisassembler);

            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        public unsafe IView GetView(IViewable viewable, ViewMode mode = ViewMode.Default)
        {
            var writer = GetViewWriter(mode, null);
            viewable.WriteStruct(writer);

            if (writer.Current.Count != 1)
                throw new NotImplementedException();

            return writer.Current[0];
        }

        private unsafe PEViewWriter GetViewWriter(ViewMode mode, IViewDisassembler? viewDisassembler)
        {
            GetRawPointer(out var pointer, out var length);

            var writer = new PEViewWriter(this, pointer, length, viewDisassembler, mode);

            return writer;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe void GetRawPointer(out byte* pointer, out int length)
        {
            if (blockProvider is LocalMemoryBlockProvider l)
            {
                pointer = l.Pointer;
                length = (int) l.Length;
            }
            else
            {
                throw new NotImplementedException();
            }
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
            localProvider.is32Bit = OptionalHeader.Magic == PEMagic.PE32;
        }

        //ctor for initializing PEFile from an IMemoryReader that reads remote memory
        private PEFile(IMemoryReader reader, long address, bool isLoadedImage)
        {
            IsLoadedImage = isLoadedImage;

            var remoteProvider = new RemoteMemoryBlockProvider(reader, address, this);
            blockProvider = remoteProvider;

            //Will automatically demand
            headerBlock = new RemoteHeaderMemoryBlock(reader, address, blockProvider);

            InitializeHeaders();

            remoteProvider.is32Bit = OptionalHeader.Magic == PEMagic.PE32;
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
        public bool TryGetDirectoryOffset(in ImageDataDirectory entry, out int offset, bool canCrossSectionBoundary)
        {
            var sectionIndex = GetSectionContainingRVA(entry.VirtualAddress);

            if (sectionIndex < 0)
            {
                offset = -1;
                return false;
            }

            var section = SectionHeaders[sectionIndex];
            var relativeOffset = (int) (entry.VirtualAddress - section.VirtualAddress);

            if (!canCrossSectionBoundary && entry.Size > section.VirtualSize - relativeOffset)
                throw new BadImageFormatException("Section too small.");

            offset = IsLoadedImage
                ? entry.VirtualAddress
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
        public bool TryGetOffset(int rva, out int offset)
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
                offset = -1;
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

        public bool TryGetRVA(int offset, out int rva)
        {
            //Offsets returned from TryGetOffset/TryGetDirectoryOffset are just RVAs
            if (IsLoadedImage)
            {
                rva = offset;
                return true;
            }

            var sectionIndex = GetSectionContainingOffset(offset);

            if (sectionIndex < 0)
            {
                rva = -1;
                return false;
            }

            ref var section = ref SectionHeaders[sectionIndex];

            var relativeRVA = (int) (offset - section.PointerToRawData);
            rva = section.VirtualAddress + relativeRVA;
            return true;
        }

        /// <summary>
        /// Gets the section that contains the specified Relative Virtual Address.
        /// </summary>
        /// <param name="rva">The RVA whose containing section should be found.</param>
        /// <returns>The index of section that contains the RVA, or -1 if none was found.</returns>
        public int GetSectionContainingRVA(int rva)
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

        public int GetSectionContainingOffset(int offset)
        {
            //Offsets returned from TryGetOffset/TryGetDirectoryOffset are just RVAs
            if (IsLoadedImage)
                return GetSectionContainingRVA(offset);

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

        internal bool TryGetDirectoryChunk(in ImageDataDirectory entry, out MemoryChunk chunk)
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

            //IsLoadedImage can be false for in-memory modules
            if (headerBlock is LocalHeaderMemoryBlock)
            {
                //In an unloaded image, our header block technically provides access to the entire module
                chunk = new MemoryChunk(headerBlock, offset);
                return true;
            }
            else
            {
                //The value isn't part of any known section, but if it's part of the header, we can do something with that
                if (offset < OptionalHeader.SizeOfHeaders)
                {
                    chunk = new MemoryChunk(headerBlock, offset);
                    return true;
                }
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

            Debug.Assert(relativeOffset < section.SizeOfRawData);

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

        public unsafe void GetRawSectionDataFromRVA(int rva, out byte* ptr, out int remainingLength)
        {
            if (TryGetSectionBlockFromRVA(rva, out var block, out var relativeOffset))
            {
                ptr = block.LocalPointer + relativeOffset;
                remainingLength = block.Length - relativeOffset;
                return;
            }

            throw new BadImageFormatException();
        }

        public unsafe void GetRawSectionDataFromRVA(int rva, int sectionIndex, out byte* ptr, out int remainingLength)
        {
            ref var section = ref SectionHeaders[sectionIndex];

            var block = GetSectionBlock(sectionIndex, section);

            var relativeOffset = rva - section.VirtualAddress;
            ptr = block.LocalPointer + relativeOffset;
            remainingLength = block.Length - relativeOffset;
        }

        public unsafe void GetRawSectionDataFromOffset(int offset, out byte* ptr, out int remainingLength)
        {
            if (TryGetSectionBlockFromOffset(offset, out var block, out var relativeOffset))
            {
                ptr = block.LocalPointer + relativeOffset;
                remainingLength = block.Length - relativeOffset;
                return;
            }

            throw new BadImageFormatException();
        }

        public unsafe void GetRawSectionDataFromOffset(int offset, int sectionIndex, out byte* ptr, out int remainingLength)
        {
            ref var section = ref SectionHeaders[sectionIndex];

            var block = GetSectionBlock(sectionIndex, section);

            var relativeOffset = offset - section.PointerToRawData;
            ptr = block.LocalPointer + relativeOffset;
            remainingLength = block.Length - relativeOffset;
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

        ICodeView IFileWithCodeViewData.CodeViewData => throw new NotImplementedException(); //todo: try and lookup the relevant codeview debug table

        #endregion

        void IViewable.WriteGlobals(ViewWriter writer)
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

            writer.WriteGlobal(ILMethods);

            writer.WriteGlobal(EcmaMetadata);

            //writer.WriteGlobal(ReadyToRunHeader);

            writer.WriteGlobal(AppHostSignature);
            writer.WriteGlobal(ClrEngineMetrics);
            writer.WriteGlobal(RuntimeInfo);
            writer.WriteGlobal(DotNetRuntimeDebugHeader);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();

        public void Dispose()
        {
            Dispose(true);
        }

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

        public override string ToString()
        {
            if (Name != null)
                return Name.ToString();

            return base.ToString();
        }
    }
}
