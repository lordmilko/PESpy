using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using ClrDebug;
using PESpy.Native;
using PESpy.View;
using PESpy.VB;
using Stream = System.IO.Stream;
using static ClrDebug.IMAGE_FILE_MACHINE;
using static ClrDebug.COMIMAGE_FLAGS;
using static PESpy.IMAGE_DEBUG_TYPE;
using static PESpy.NativeMethods;

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

        public string? Name => peFile.Name;
        public string? FileName => peFile.FileName;
        public FileKind Kind => peFile.Kind;
        public long Length => peFile.Length;
        public bool Is32Bit => peFile.Is32Bit;

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
        public RawValue<FixedAnsiString>? Copyright => peFile.Copyright;
        public int GlobalPointer => peFile.GlobalPointer;
        public ImageTlsDirectory? TlsDirectory => peFile.TlsDirectory;
        public ImageLoadConfigDirectory? LoadConfigTable => peFile.LoadConfigTable;
        public ImageBoundImportDescriptor[]? BoundImportTable => peFile.BoundImportTable;
        public ImageThunkDataList? ImportAddressTable => peFile.ImportAddressTable;
        public ImageDelayLoadDescriptor[]? DelayImportTable => peFile.DelayImportTable;

        #region Cor20Header

        public ImageCor20Header? Cor20Header => peFile.Cor20Header;

        public EcmaMetadata? EcmaMetadata => peFile.EcmaMetadata;

        public ManifestResource[]? Cor20Resources => peFile.Cor20Resources;

        public object? Cor20StrongNameSignature => peFile.Cor20StrongNameSignature;

        public object? Cor20CodeManagerTable => peFile.Cor20CodeManagerTable;

        public ImageCorVTableFixup[]? Cor20VTableFixups => peFile.Cor20VTableFixups;

        public object? Cor20ExportAddressTableJumps => peFile.Cor20ExportAddressTableJumps;

        public IValue? Cor20ManagedNativeHeader => peFile.Cor20ManagedNativeHeader;

        public ImageCorILMethodList? ILMethods => peFile.ILMethods;

        #endregion
        #endregion
        #region NGEN

        public CorCompileHeader? NgenHeader => peFile.NgenHeader;

        public NgenHelperEntry[]? NgenHelperTable => peFile.NgenHelperTable;

        public CorCompileImportSection[]? NgenImportSections => peFile.NgenImportSections;

        public CorCompileImportTableEntry[]? NgenImportTable => peFile.NgenImportTable;

        public object? NgenStubsData => peFile.NgenStubsData;

        public CorCompileVersionInfo? NgenVersionInfo => peFile.NgenVersionInfo;

        public CorCompileDepepdency[]? NgenDependencies => peFile.NgenDependencies;

        public int[]? NgenDebugMap => peFile.NgenDebugMap;

        public ByteBlob? NgenModuleImage => peFile.NgenModuleImage;

        public CorCompileCodeManagerEntry? NgenCodeManagerTable => peFile.NgenCodeManagerTable;

        public object? NgenProfileDataList => peFile.NgenProfileDataList;

        public EcmaMetadata? NgenManifestMetaData => peFile.NgenManifestMetaData;

        public CorCompileVirtualSectionInfo[]? NgenVirtualSectionsTable => peFile.NgenVirtualSectionsTable;

        public object? NgenEEInfoTable => peFile.NgenEEInfoTable;

        #endregion

        //Don't do "using" for ReadyToRunHeader, as in NativeAOT there's a similar structure with a different layout
        public R2R.ReadyToRunHeader? ReadyToRunHeader => peFile.ReadyToRunHeader;

        public AppHostSignature? AppHostSignature => peFile.AppHostSignature;

        public ClrEngineMetrics? ClrEngineMetrics => peFile.ClrEngineMetrics;

        public RuntimeInfo? DotNetRuntimeInfo => peFile.DotNetRuntimeInfo;

        public NativeAOT.DotNetRuntimeDebugHeader? DotNetRuntimeDebugHeader => peFile.DotNetRuntimeDebugHeader;

        public NativeAOTModulesList? NativeAOTModules => peFile.GetNativeAOTModules(debugger: true);

        public SymbolValueList<RTTICompleteObjectLocator>? RTTICompleteObjectLocators => peFile.GetRTTICompleteObjectLocators(debugger: true);

        public SymbolValueList<VftableInfo>? Vftables => peFile.GetVftables(debugger: true);

        public RpcInfo? RpcInfo => peFile.RpcInfo;

        public ExeProjectInfo? ExeProjectInfo => peFile.ExeProjectInfo;
    }

    /// <summary>
    /// Represents a Portable Executable (PE) file.
    /// </summary>
    [DebuggerTypeProxy(typeof(PEFileDebugView))]
    public class PEFile : IFileInternal, IFileWithCodeViewData, IViewable, IDisposable
    {
        #region Static

        //Note: when it comes to building PE files, we can't have an API for creating an "Empty" PDB, because we don't have a way to denote that stuff like the DOS Header
        //might not exist. Also, I think we do need a PEFileBuilder, because when we add/remove items we may have to shift things around, so we probably need to rewrite the whole PE

        /// <summary>
        /// Reads a <see cref="PEFile"/> from a file on disk.<para/>
        /// If the file is a compressed WINLZ file (e.g. *.dl_ files with magic signature "SZDD"), this method will decompress
        /// the file in memory and then open it
        /// </summary>
        /// <param name="path">The path to the file to read.</param>
        /// <returns>A <see cref="PEFile"/> that provides access to the contents of the specified file.</returns>
        public static unsafe PEFile FromFile(string path)
        {
            //Opening the file and creating the MMF, without doing anything else, allocates 1.07KB
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                if (mmf.Length >= 2 && *(ushort*) mmf.Address != ImageDosHeader.IMAGE_DOS_SIGNATURE)
                {
                    if (Detector.TryExtract(mmf, out var decompressionInfo))
                    {
                        mmf.Dispose(); //Don't need this anymore!

                        //Replace with our own one
                        mmf = new MemoryMappedFileHolder(decompressionInfo.Bytes);

                        Detector.TryGetUncompressedFileName(path, decompressionInfo.ExtensionChar, out var name);

                        return new PEFile(path, mmf, name: name);
                    }
                }

                return new PEFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Locates a file on the symbol server and opens it as a <see cref="PEFile"/>.
        /// </summary>
        /// <param name="symStoreKey">The <see cref="SymStoreKey"/> describing the file that should be located and opened.</param>
        /// <returns>A <see cref="PEFile"/> that provides access to the contents of the specified file.</returns>
        /// <exception cref="ArgumentException">The specified <see cref="SymStoreKey"/> cannot be opened as a <see cref="PEFile"/>.</exception>
        public static PEFile FromKey(SymStoreKey symStoreKey)
        {
            switch (symStoreKey.Kind)
            {
                case SymStoreKeyKind.PE:
                case SymStoreKeyKind.CLR:
                case SymStoreKeyKind.DAC:
                case SymStoreKeyKind.DBI:
                    var path = Locator.Locate(symStoreKey);

                    return FromFile(path);

                default:
                    throw new ArgumentException($"{nameof(SymStoreKey)} '{symStoreKey}' of type '{symStoreKey.Kind}' cannot be opened as a {nameof(PEFile)}");
            }
        }

        /// <summary>
        /// Reads a <see cref="PEFile"/> from a module contained in a remote process.<para/>
        /// This method can only be used on Windows. To read process memory on other operating systems,
        /// use <see cref="PEFile.FromMemory"/>
        /// </summary>
        /// <param name="processId">The ID of the process containing the module.</param>
        /// <param name="moduleName">The name of the module to be read. This may be the module name either with
        /// or without a file extension (e.g. "ntdll", "ntdll.dll"). In the event no file extension is specified,
        /// the first module whose base name matches the specified module name will be used. This method
        /// only supports guessing file extensions that end in ".exe" or ".dll". If a non-standard file
        /// extension is used, the file extension must be explicitly specified.</param>
        /// <param name="isLoaded">Whether the PE File has been processed by the operating system loader.
        /// If the file has been manually mapped via CreateFileMapping(), this value should be false.</param>
        /// <returns></returns>
        public static unsafe PEFile FromProcess(int processId, string moduleName, bool isLoaded = true)
        {
            //We're going to end up duplicating the handle so we're always going to need to free it

            const int PROCESS_VM_OPERATION = 0x8;
            const int PROCESS_VM_READ = 0x10;
            const int PROCESS_VM_WRITE = 0x20;
            const int PROCESS_DUP_HANDLE = 0x40;

            var hProcess = OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_DUP_HANDLE, 0, processId);

            if (hProcess == default)
                throw new DebugException($"Failed to open process '{processId}'", (HRESULT) Marshal.GetHRForLastWin32Error());

            IntPtr pModuleName = default;

            try
            {
                //Convert to ANSI for faster comparison
                pModuleName = Marshal.StringToHGlobalAnsi(moduleName);

                if (!TryGetProcessModule(hProcess, new ReadOnlySpan<byte>((byte*) pModuleName, moduleName.Length), out var hModule, out var fileName))
                    throw new InvalidOperationException($"Failed to find a module named '{moduleName}' in process {processId}");

                if (GetCurrentProcessId() == processId)
                {
                    var moduleInfo = new MODULEINFO();

                    if (GetModuleInformation(hProcess, hModule, &moduleInfo, sizeof(MODULEINFO)) != 0)
                    {
                        //Fast path: just memory map it
                        return new PEFile(fileName, new MemoryMappedFileHolder((byte*) hModule, moduleInfo.SizeOfImage), isLoaded);
                    }
                }

                return new PEFile(new ProcessMemoryAccessor(hProcess), (long) (void*) hModule, isLoaded, fileName);
            }
            finally
            {
                if (pModuleName != default)
                    Marshal.FreeHGlobal(pModuleName);

                CloseHandle(hProcess);
            }
        }

        private unsafe static bool TryGetProcessModule(
            IntPtr hProcess,
            ReadOnlySpan<byte> targetName,
            out IntPtr hModule,
            out string fileName)
        {
            //It's a bit hard to say whether the module name has a file extension or not,
            //because you can have legitimate module names that have dots in them

            var need32BitModulesFor64BitProcess = IntPtr.Size == 8 && IsWow64ProcessOrDefault(hProcess);

            IntPtr[] x86Modules = null;
            HashSet<IntPtr> addedModules = null;

            if (need32BitModulesFor64BitProcess)
            {
                x86Modules = EnumProcessModulesEx(hProcess, LIST_MODULES.LIST_MODULES_32BIT);

                //The application itself has the same module base in both the 32-bit and 64-bit sections of the process, so we need to check
                //to see whether we've added a module before
                addedModules = new HashSet<IntPtr>();
            }

            var buffer = RtlCreateQueryDebugBuffer();

            try
            {
                //MODULES32 implies MODULES, so we need to filter out the retrieved modules for only the ones we're looking for
                RtlQueryProcessDebugInformation((IntPtr) GetProcessId(hProcess), (need32BitModulesFor64BitProcess ? RTL_QUERY_PROCESS.MODULES32 : RTL_QUERY_PROCESS.MODULES) | RTL_QUERY_PROCESS.NONINVASIVE, buffer);

                var pModules = buffer->Modules;

                RTL_PROCESS_MODULE_INFORMATION* pFallbackModule = default;

                for (var i = 0; i < pModules->NumberOfModules; i++)
                {
                    var moduleInfo = ((RTL_PROCESS_MODULE_INFORMATION*) &pModules->Modules) + i;

                    if (!need32BitModulesFor64BitProcess || (!addedModules.Contains(moduleInfo->ImageBase) && Array.IndexOf(x86Modules, moduleInfo->ImageBase) != -1))
                    {
                        if (need32BitModulesFor64BitProcess)
                        {
                            //This is an x86 module. Load it!
                            Debug.Assert(moduleInfo->ImageBase != IntPtr.Zero);
                            addedModules.Add(moduleInfo->ImageBase);
                        }

                        var modulePath = new AnsiString(moduleInfo->FullPathName).AsSpan();

                        var lastSlash = modulePath.LastIndexOf((byte) Path.DirectorySeparatorChar);

                        ReadOnlySpan<byte> currentName = lastSlash == -1 ? modulePath : modulePath.Slice(lastSlash + 1);

                        if (targetName.EqualsOrdinalIgnoreCaseUtf8(currentName))
                        {
                            hModule = pFallbackModule->ImageBase;
                            fileName = new AnsiString(pFallbackModule->FullPathName).ToString();
                            return true;
                        }

                        /* Suppose you're after foo.dll, but you ask for foo, and there's
                         * loaded images foo.dll and foo.bar (without file extension). Stripping
                         * "bar" off foo.bar gives foo, and stripping "dll" of foo.dll also gives
                         * "foo", but "foo.dll" was closer to "foo" than "foo.bar" was. The issue we
                         * have though is we have no way of knowing that "bar" is not a file extension.
                         * As such, we only support guessing file extensions that end in ".exe" or ".dll"
                         */

                        if (pFallbackModule == null && currentName.Length == targetName.Length + 4 &&
                            currentName.StartsWithIgnoreCase(targetName) &&
                            (currentName.EndsWithIgnoreCase(".dll"u8) || currentName.EndsWithIgnoreCase(".exe"u8)))
                        {
                            pFallbackModule = moduleInfo;
                        }
                    }
                }

                //We failed to find a perfect match, so if we got a fallback module, use that
                if (pFallbackModule != default)
                {
                    hModule = pFallbackModule->ImageBase;
                    fileName = new AnsiString(pFallbackModule->FullPathName).ToString();
                    return true;
                }

                hModule = default;
                fileName = default;
                return false;
            }
            finally
            {
                RtlDestroyQueryDebugBuffer(buffer);
            }
        }

        /// <summary>
        /// Reads a <see cref="PEFile"/> from a module contained in a remote process.<para/>
        /// This method can only be used on Windows. To read process memory on other operating systems,
        /// use <see cref="PEFile.FromMemory"/>
        /// </summary>
        /// <param name="hProcess">A handle to the process containing the module that should be read.
        /// This handle will be duplicated by <see cref="PEFile"/>, and can be closed by the caller once they are no
        /// longer using it.</param>
        /// <param name="moduleBase">The base address of the module in the remote process that should be read.</param>
        /// <param name="isLoaded">Whether the PE File has been processed by the operating system loader.
        /// If the file has been manually mapped via CreateFileMapping(), this value should be false.</param>
        /// <returns>A <see cref="PEFile"/> that provides access to the contents of the specified module.</returns>
        public static unsafe PEFile FromProcess(IntPtr hProcess, IntPtr moduleBase, bool isLoaded = true)
        {
            string fileName = null;

#if !NETSTANDARD
            if (!OperatingSystem.IsWindows())
                throw new InvalidOperationException("Reading process memory is only supported on Windows. Consider using PEFile.FromMemory() instead with a custom IMemoryAccessor");
#endif

            fileName = GetModuleFileName(hProcess, moduleBase);

            if (GetProcessId(hProcess) == GetCurrentProcessId())
            {
                var moduleInfo = new MODULEINFO();

                if (GetModuleInformation(hProcess, moduleBase, &moduleInfo, sizeof(MODULEINFO)) != 0)
                {
                    //Fast path: just memory map it
                    return new PEFile(fileName, new MemoryMappedFileHolder((byte*) moduleBase, moduleInfo.SizeOfImage), isLoaded);
                }
            }

            //ProcessMemoryAccessor will clone the process handle
            return new PEFile(new ProcessMemoryAccessor(hProcess), (long) (void*) moduleBase, isLoaded, fileName);
        }

        /// <summary>
        /// Reads a <see cref="PEFile"/> from a custom memory source, such as a dump file.
        /// </summary>
        /// <param name="memoryAccessor">The user defined memory accessor that provides access to the bytes of the <see cref="PEFile"/>.</param>
        /// <param name="isLoaded"></param>
        /// <param name="fileName">The full path to the module that this <see cref="PEFile"/> encapsulates, or the name with
        /// file extension if the full path is not available. This value may be used by <see cref="Locator"/> for the purpose
        /// of locating symbol files in the same directory as this <see cref="PEFile"/>, as well as looking for symbols on
        /// a remote symbol server if local symbols cannot be found. If this value is not specified, this <see cref="PEFile"/>
        /// will be unable to provide access to entities that must be located using symbols.</param>
        /// <param name="baseAddress">The base address of the module within the address space that <paramref name="memoryAccessor"/> provides access to.
        /// This value will be added to each memory address that is passed to <paramref name="memoryAccessor"/> (e.g. to faciliate
        /// reading process memory). If <paramref name="memoryAccessor"/> is implicitly scoped to the bytes that pertain to this
        /// <see cref="PEFile"/>, this value can be 0.</param>
        /// <returns>A <see cref="PEFile"/> that provides access to the contents of the specified module.</returns>
        public static PEFile FromMemory(
            IMemoryAccessor memoryAccessor,
            bool isLoaded = true,
            string? fileName = null,
            long baseAddress = 0)
        {
            if (memoryAccessor == null)
                throw new ArgumentNullException(nameof(memoryAccessor));

            return new PEFile(memoryAccessor, baseAddress, isLoaded, fileName);
        }

        private static unsafe string? GetModuleFileName(IntPtr hProcess, IntPtr hModule)
        {
            var need32BitModulesFor64BitProcess = IntPtr.Size == 8 && IsWow64ProcessOrDefault(hProcess);

            var buffer = RtlCreateQueryDebugBuffer();

            //Note that the x64 and x86 modules have different image bases. Therefore, whichever one the caller is asking for,
            //we'll give them

            try
            {
                //MODULES32 implies MODULES, so we need to filter out the retrieved modules for only the ones we're looking for
                RtlQueryProcessDebugInformation((IntPtr) GetProcessId(hProcess), (need32BitModulesFor64BitProcess ? RTL_QUERY_PROCESS.MODULES32 : RTL_QUERY_PROCESS.MODULES) | RTL_QUERY_PROCESS.NONINVASIVE, buffer);

                var pModules = buffer->Modules;

                for (var i = 0; i < pModules->NumberOfModules; i++)
                {
                    var moduleInfo = ((RTL_PROCESS_MODULE_INFORMATION*) &pModules->Modules)[i];

                    if (moduleInfo.ImageBase == hModule)
                    {
                        var modulePath = new AnsiString(moduleInfo.FullPathName);

                        return modulePath.ToString();
                    }
                }

                return null;
            }
            finally
            {
                RtlDestroyQueryDebugBuffer(buffer);
            }
        }

        //Not not support WINLZ files (*.dl_)
        public static PEFile FromStream(Stream stream, bool isLoadedImage, string? fileName = null)
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

            return new PEFile(new StreamMemoryAccessor(stream), stream.Position, isLoadedImage, fileName);
        }

        #endregion

        /// <summary>
        /// Gets whether the image has been mapped into the address space by the operating system loader, indicating that sections have been laid out according to their RVAs.<para/>
        /// Modules may be memory mapped without having been processed by the loader, in which case they should be processed as if they exist on disk.
        /// </summary>
        public bool IsLoadedImage { get; init; }

        private SymStoreKey[]? symStoreKeys;

        /// <summary>
        /// Gets the keys of all files that this <see cref="PEFile"/> references that can be downloaded from a symbol server
        /// </summary>
        public SymStoreKey[] SymStoreKeys
        {
            get
            {
                if (symStoreKeys == null)
                {
                    using var results = new ValueList<SymStoreKey>();

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
                                case IMAGE_DEBUG_TYPE_CODEVIEW:
                                {
                                    var data = (ICodeViewPDB?) debugDirectory.Data;

                                    if (data != null)
                                    {
                                        switch (data.Signature)
                                        {
                                            case CodeViewSig.RSDS:
                                            {
                                                var rsdsPath = data.Path.ToString();
                                                var rsds = (RSDSI) data;

                                                results.Add(SymStoreKey.FromRSDSI(rsdsPath, rsds.Guid, data.Age));

                                                //If the debug directory indicates that it may correspond to a Portable PDB, there may be a PDB on the server with an age of -1
                                                //(potentially in addition to a PDB with the real age too!)
                                                if (debugDirectory.IsPortablePDB)
                                                    results.Add(SymStoreKey.FromRSDSI(rsdsPath, rsds.Guid, -1, SymStoreKeyKind.PortablePDB));
                                            }
                                            break;

                                            case CodeViewSig.NB10:
                                                results.Add(SymStoreKey.FromNB10((NB10I) data));
                                                break;
                                        }
                                    }

                                    break;
                                }

                                case IMAGE_DEBUG_TYPE_MISC:
                                {
                                    var data = (ImageDebugMisc?) debugDirectory.Data;

                                    if (data != null)
                                        results.Add(SymStoreKey.FromMisc(data.Data.ToString(), FileHeader.TimeDateStamp, OptionalHeader.SizeOfImage));

                                    break;
                                }
                            }
                        }
                    }

                    var runtimeInfo = DotNetRuntimeInfo;

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

        /// <summary>
        /// Gets the first <see cref="SymStoreKey"/> of a specified kind that is referenced
        /// by this <see cref="PEFile"/>.
        /// </summary>
        /// <param name="kind">The kind of <see cref="SymStoreKey"/> to retrieve.</param>
        /// <returns>A <see cref="SymStoreKey"/> of the specified kind.</returns>
        /// <exception cref="InvalidOperationException">A <see cref="SymStoreKey"/> of the specified kind could not be found.</exception>
        public SymStoreKey GetSymStoreKey(SymStoreKeyKind kind)
        {
            if (!TryGetSymStoreKey(kind, out var key))
                throw new InvalidOperationException($"Could not get a SymStoreKey of type '{kind}'");

            return key;
        }

        /// <summary>
        /// Tries to get the first <see cref="SymStoreKey"/> of a specified kind that is referenced by this <see cref="PEFile"/>.
        /// </summary>
        /// <param name="kind">The kind of <see cref="SymStoreKey"/> to retrieve.</param>
        /// <param name="key">The key of the specified kind that was found.</param>
        /// <returns>True if any key could be found of the specified kind. Otherwise, false.</returns>
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
                case SymStoreKeyKind.PortablePDB:
                    {
                        var debugTable = DebugTable;

                        if (debugTable == null)
                            return false;

                        var name = Name;

                        bool isNGENImage = false;

                        if (name != null)
                            isNGENImage = name.EndsWith(".ni.exe") || name.EndsWith(".ni.dll");

                        for (var i = 0; i < debugTable.Length; i++)
                        {
                            ref var debugDir = ref debugTable[i];

                            if (debugDir.Type == IMAGE_DEBUG_TYPE_CODEVIEW)
                            {
                                if (debugDir.Data is ICodeViewPDB p)
                                {
                                    switch (p.Signature)
                                    {
                                        case CodeViewSig.NB10:
                                            if (kind == SymStoreKeyKind.PortablePDB)
                                                continue; //We don't expect that NB10 CodeView records should support Portable PDBs

                                            key = SymStoreKey.FromNB10((NB10I) p);
                                            return true;

                                        case CodeViewSig.RSDS:
                                            if (kind == SymStoreKeyKind.PortablePDB)
                                            {
                                                if (debugDir.IsPortablePDB)
                                                {
                                                    var rsds = (RSDSI) p;
                                                    key = SymStoreKey.FromRSDSI(rsds.Path.ToString(), rsds.Guid, -1, SymStoreKeyKind.PortablePDB);
                                                    return true;
                                                }
                                            }
                                            else
                                            {
                                                var checkForBetterPDBs = false;
                                                var anyBetterPDB = false;

                                                //If we're an NGEN image, prefer the NGEN PDB. Otherwise, prefer the non-NGEN PDB
                                                if (p.Path.EndsWith(".ni.pdb"))
                                                {
                                                    if (!isNGENImage)
                                                        checkForBetterPDBs = true;
                                                }
                                                else
                                                {
                                                    //Not an NGEN PDB
                                                    if (isNGENImage)
                                                        checkForBetterPDBs = true;
                                                }

                                                if (checkForBetterPDBs)
                                                {
                                                    //Are any better PDBs coming up?
                                                    for (var j = i + 1; j < debugTable.Length; j++)
                                                    {
                                                        debugDir = ref debugTable[j];

                                                        if (debugDir.Type == IMAGE_DEBUG_TYPE_CODEVIEW)
                                                        {
                                                            anyBetterPDB = true;
                                                            i = j - 1; //Skip ahead to it (i is about to be incremented to j after this loop ends)
                                                            break;
                                                        }
                                                    }
                                                }

                                                if (!anyBetterPDB)
                                                {
                                                    key = SymStoreKey.FromRSDSI((RSDSI) p);
                                                    return true;
                                                }
                                            }
                                            break;

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

                            if (debugDir.Type == IMAGE_DEBUG_TYPE_MISC)
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
                    var runtimeInfo = DotNetRuntimeInfo;

                    if (runtimeInfo == null)
                        return false;

                    var runtimeModuleIndex = runtimeInfo.RuntimeModuleIndex;

                    key = SymStoreKey.FromPE("coreclr.dll", runtimeModuleIndex.TimeStamp, runtimeModuleIndex.ImageSize, SymStoreKeyKind.CLR);
                    return true;
                }

                case SymStoreKeyKind.DAC:
                {
                    var runtimeInfo = DotNetRuntimeInfo;

                    if (runtimeInfo == null)
                        return false;

                    var dacModuleIndex = runtimeInfo.DacModuleIndex;

                    key = SymStoreKey.FromPE("mscordaccore.dll", dacModuleIndex.TimeStamp, dacModuleIndex.ImageSize, SymStoreKeyKind.DAC);
                    return true;
                }

                case SymStoreKeyKind.DBI:
                {
                    var runtimeInfo = DotNetRuntimeInfo;

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

        //In a single file app, the nested PEFile instances use NestedMemoryBlockProvider which is
        //a type of LocalMemoryBlockProvider
        public long Length => blockProvider is LocalMemoryBlockProvider l ? (int) l.Length : (int) OptionalHeader.SizeOfImage;

        /// <summary>
        /// Gets whether this <see cref="PEFile"/> represents a 32-bit file; that is, whether <see cref="ImageOptionalHeader.Magic"/> is <see cref="PEMagic.IMAGE_NT_OPTIONAL_HDR32_MAGIC"/>.
        /// </summary>
        public bool Is32Bit => headerBlock.Is32Bit;

        public int Offset => blockProvider.StartOffset;

        #region DosHeader

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageDosHeader dosHeader;

        /// <summary>
        /// Gets the MS-DOS 2.0 compatible <see cref="IMAGE_DOS_HEADER"/>.
        /// </summary>
        public ImageDosHeader DosHeader => dosHeader;

        #endregion
        #region DosStub

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ByteBlob dosStub;

        /// <summary>
        /// Gets the bytes of the DOS Stub that represents the program that should be run in the event that the <see cref="PEFile"/>
        /// is executed under MS-DOS.
        /// </summary>
        public ByteBlob DosStub
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
                        end = (int) RichHeader.Offset;
                    else
                    {
                        //We know there isn't a RichHeader. Read up until the start of the new PE Header
                        end = DosHeader.FileAddressOfNewExeHeader;
                    }

                    var length = (int) (end - start);

                    dosStub = new ByteBlob(new MemoryChunk(headerBlock, start), length, ViewKind.DosStub);
                }

                return dosStub;
            }
        }

        #endregion
        #region RichHeader

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RichHeader? richHeader;
        private bool hasTriedRichHeader;

        /// <summary>
        /// Gets the undocumented Rich Header which describes the build environment that was used to create the executable.<para/>
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

        public ImageNtHeaders NtHeaders => ntHeaders;

        /// <summary>
        /// Gets the <see cref="IMAGE_NT_HEADERS.FileHeader"/> field that represents the file header of the image.
        /// </summary>
        public ImageFileHeader FileHeader => ntHeaders.FileHeader;

        /// <summary>
        /// Gets the <see cref="IMAGE_NT_HEADERS.OptionalHeader"/> field that represents the optional header of the image.
        /// </summary>
        public ImageOptionalHeader OptionalHeader => ntHeaders.OptionalHeader;

        internal int GetSizeOfHeaders(ViewMode viewMode)
        {
            /* I've seen a PEFile where the physical offset of the first section
             * was actually before the end of the section headers! As such, we can't
             * trust the section headers, and need to look at the offset of the first
             * section when we're really trying to be physical. If we're virtual,
             * or just pretending to be virtual, the listed size of the headers should
             * be assumed to be virtual */

            var sizeOfHeaders = OptionalHeader.SizeOfHeaders;

            if (viewMode != ViewMode.Virtual && !IsLoadedImage)
            {
                var sectionHeaders = SectionHeaders;

                for (var i = 0; i < sectionHeaders.Length; i++)
                {
                    ref var sectionHeader = ref sectionHeaders[0];

                    //Watch out for empty bss sections at the start
                    if (sectionHeader.SizeOfRawData > 0 && sectionHeader.PointerToRawData < sizeOfHeaders)
                        sizeOfHeaders = sectionHeader.PointerToRawData;
                }
            }

            Debug.Assert(sizeOfHeaders != 0);
            return sizeOfHeaders;
        }

        #endregion
        #region SectionHeaders

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ImageSectionHeader[]? sectionHeaders;

        private SectionRange[] _sectionRanges; //Virtual addresses

        internal SectionRange[] SectionRanges => _sectionRanges;

        /// <summary>
        /// Gets the section headers of the image. These values represent the <see cref="IMAGE_SECTION_HEADER"/> values (e.g. .text, .data) that immediately follow the <see cref="OptionalHeader"/>.<para/>
        /// Each section header points to a relative location within the image at which that section's data actually resides.
        /// </summary>
        public ImageSectionHeader[] SectionHeaders
        {
            get
            {
                if (sectionHeaders == null)
                {
                    var numberOfSections = ntHeaders.FileHeader.NumberOfSections;

                    var list = new ImageSectionHeader[numberOfSections];

                    //FileAddressOfNewExeHeader + sizeof(int) + ImageFileHeader.StructSize + SizeOfOptionalHeader should give this
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

                    if (exportTableDirectory.HasData && TryGetDirectoryChunk(exportTableDirectory, out var chunk))
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

                    if (importTableDirectory.HasData && TryGetDirectoryChunk(importTableDirectory, out var chunk))
                    {
                        //I don't know if we're guaranteed to fill up the entire ImportTableDirectory with
                        //ImageImportDescriptor objects, or if there's other stuff in there too. I feel like
                        //the latter is the case, as such we can't calculate exactly how many entries we'll have
                        using var results = new ValueList<ImageImportDescriptor>();

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

                    if (resourceTableDirectory.HasData && TryGetDirectoryChunk(resourceTableDirectory, out var chunk))
                    {
                        var local = new ImageResourceDirectory(chunk, resourceTableDirectory.VirtualAddress, null);

                        //Since this property returns a reference, we should always return the same object
                        Interlocked.CompareExchange(ref resourceDirectory, local, null);
                    }
                }

                return resourceDirectory;
            }
        }

        public bool TryGetVersionInfo(out VsVersionInfo versionInfo)
        {
            //System.Diagnostics.FileVersionInfo does a whole bunch of crazy stuff trying to guess the locale to use in the event
            //that the PE file does not have the version info stored correctly. I feel like we can just skip all of that and simply
            //return the first version info object that we find

            versionInfo = default;

            var resourceDirectory = ResourceDirectory;

            if (resourceDirectory == null)
                return false;

            for (var i = 0; i < resourceDirectory.Entries.Length; i++)
            {
                ref var entryLevel1 = ref resourceDirectory.Entries[i];

                if (entryLevel1.Type == RT.RT_VERSION && entryLevel1.DataIsDirectory && entryLevel1.OffsetToDirectory.IsValid)
                {
                    var directoryLevel2 = entryLevel1.OffsetToDirectory.Value;

                    //VS_VERSION_INFO is 1

                    for (var j = 0; j < directoryLevel2.Entries.Length; j++)
                    {
                        ref var entryLevel2 = ref directoryLevel2.Entries[j];

                        if (!entryLevel2.NameOrId.NameIsString && entryLevel2.NameOrId.Id == 1)
                        {
                            if (!entryLevel2.DataIsDirectory || !entryLevel2.OffsetToDirectory.IsValid)
                                return false;

                            var directoryLevel3 = entryLevel2.OffsetToDirectory.Value;

                            /* We are now at /RT_VERSION/1/
                             * The current level should be the language, e.g.
                             * /RT_VERSION/1/1033/
                             * 
                             * Rather than do a bunch of shenanigans trying to guess what
                             * language to use, we'll just take the first language that we see
                             */

                            for (var k = 0; k < directoryLevel3.Entries.Length; k++)
                            {
                                ref var entryLevel3 = ref directoryLevel3.Entries[j];

                                if (!entryLevel3.DataIsDirectory && entryLevel3.OffsetToData.IsValid)
                                {
                                    var dataEntry = entryLevel3.OffsetToData.Value;

                                    if (dataEntry.OffsetToData.IsValid)
                                    {
                                        versionInfo = dataEntry.OffsetToData.Value as VsVersionInfo;

                                        if (versionInfo != null)
                                            return true;
                                    }
                                }
                            }

                            //If a match wasn't found inside VS_VERSION_INFO (1), it's over
                            break;
                        }
                    }
                }
            }

            return false;
        }

        #endregion
        #region Exception Table (3)

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RuntimeFunctionList? exceptionTable;

        /// <summary>
        /// Gets the exception table pointed to by <see cref="ImageOptionalHeader.ExceptionTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_EXCEPTION) containing information used to unwind stack frames during exception handling.<para/>
        /// If the image does not have an exception table, this property returns <see langword="null"/>.
        /// </summary>
        public RuntimeFunctionList? ExceptionTable
        {
            get
            {
                if (exceptionTable == null)
                {
                    var exceptionTableDirectory = OptionalHeader.ExceptionTableDirectory;

                    if (exceptionTableDirectory.HasData && TryGetDirectoryChunk(exceptionTableDirectory, out var chunk))
                    {
                        var numEntries = OptionalHeader.ExceptionTableDirectory.Size / RuntimeFunction.StructSize;

                        exceptionTable =  new RuntimeFunctionList(numEntries, chunk);
                    }
                }

                return exceptionTable;
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
                    if (securityTableDirectory.HasData && TryGetValueChunkFromPhysicalOffset(securityTableDirectory.VirtualAddress, out var chunk))
                    {
                        //https://blog.trailofbits.com/2020/05/27/verifying-windows-binaries-without-windows/
                        //https://github.com/trailofbits/uthenticode
                        //http://download.microsoft.com/download/9/c/5/9c5b2167-8017-4bae-9fde-d599bac8184a/Authenticode_PE.docx

                        var end = securityTableDirectory.Size;

                        using var results = new ValueList<WinCertificate>();

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

                if (baseRelocationTableDirectory.HasData && TryGetDirectoryChunk(baseRelocationTableDirectory, out var chunk))
                {
                    var end = OptionalHeader.BaseRelocationTableDirectory.Size;

                    using var results = new ValueList<ImageBaseRelocation>();

                    var read = 0;

                    //https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#the-reloc-section-image-only
                    //says that "each block must start on a 32-bit boundary", however it seems like that's not
                    //the same thing as the ImageBaseRelocation sturct itself starting on a 32-bit boundary
                    while (read < (int) end)
                    {
                        var item = new ImageBaseRelocation(chunk.Slice(read));

                        read += item.SizeOfBlock;

                        results.Add(item);
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

                    if (debugTableDirectory.HasData && TryGetDirectoryChunk(debugTableDirectory, out var chunk))
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

        //NT 4 shows this is a legacy section that used to contain a string and that it represents a physical offset.
        //Unsure whether IMAGE_DIRECTORY_ENTRY_ARCHITECTURE is the same thing. I've seen evidence that IMAGE_DIRECTORY_ENTRY_COPYRIGHT
        //only pertained to x86. I haven't found a sample either way

        internal RawValue<FixedAnsiString>? Copyright //If we expose this externally, need to make sure we write it in WriteGlobals
        {
            get
            {
                var copyrightTable = OptionalHeader.CopyrightTableDirectory;

                if (copyrightTable.HasData && TryGetValueChunkFromPhysicalOffset((int) copyrightTable.Offset, out var chunk) && copyrightTable.Size < chunk.Remaining)
                    return new RawValue<FixedAnsiString>(chunk.AbsoluteOffset, chunk.PeekAnsiFixedLength(0, copyrightTable.Size));

                return default;
            }
        }

        #endregion
        #region Global Pointer Table (8)

        /// <summary>
        /// Gets the RVA of the shared data segment that allows sharing data between all processes that load a given DLL<para/>
        /// The IMAGE_DIRECTORY_ENTRY_GLOBALPTR directory is created in a <see cref="PEFile"/> when an *.obj file is linked
        /// containing an .sdata section that targets <see cref="IMAGE_FILE_MACHINE_R4000"/>, <see cref="IMAGE_FILE_MACHINE_R10000"/> or
        /// <see cref="IMAGE_FILE_MACHINE_ALPHA"/>.<para/>
        /// 
        /// <code>
        /// //Declare globals inside the shared data segment
        /// #pragma data_seg(".sdata")
        /// 
        /// int a = 1;
        /// 
        /// //Restore the original data segment so any other globals
        /// //listed after this are declared in the regular data segment
        /// #pragma data_seg()
        /// </code>
        /// The resulting <see cref="PEFile"/> will not contain an .sdata directory. Curiously, global fields declared inside the .sdata section
        /// may appear before the RVA pointed to by the IMAGE_DIRECTORY_ENTRY_GLOBALPTR directory; coupled with the fact that this directory does not have
        /// a listed size, it's not clear how exactly this works.
        /// </summary>
        public int GlobalPointer => OptionalHeader.GlobalPointerTableDirectory.VirtualAddress; //ImageDataDirectory has logic for handling a null MemoryChunk so we don't need to consider NumberOfRvaAndSizes

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

                    if (threadLocalStorageTableDirectory.HasData && TryGetDirectoryChunk(threadLocalStorageTableDirectory, out var chunk))
                    {
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

                    if (loadConfigTableDirectory.HasData && TryGetDirectoryChunk(loadConfigTableDirectory, out var chunk))
                    {
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

                    if (boundImportTableDirectory.HasData && TryGetValueChunkFromPhysicalOffset(boundImportTableDirectory.VirtualAddress, out var chunk))
                    {
                        var end = boundImportTableDirectory.Size;

                        using var results = new ValueList<ImageBoundImportDescriptor>();

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
        private ImageThunkDataList? importAddressTable;

        /// <summary>
        /// Gets the import address table pointed to by <see cref="ImageOptionalHeader.ImportAddressTableDirectory"/> (IMAGE_DIRECTORY_ENTRY_IAT).<para/>
        /// If the image does not have an import address table, this property returns <see langword="null"/>.
        /// </summary>
        public ImageThunkDataList? ImportAddressTable
        {
            get
            {
                if (importAddressTable == null)
                {
                    var directory = OptionalHeader.ImportAddressTableDirectory;

                    if (directory.HasData && TryGetDirectoryChunk(directory, out var chunk))
                    {
                        //Unlike when parsing thunks for a particular import descriptor, when parsing thunks for the whole IAT,
                        //we don't stop when a null thunk is hit; we stop when we reach the end
                        importAddressTable = new ImageThunkDataList(chunk, directory.Size / chunk.PointerSize, true);
                    }
                }

                return importAddressTable;
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

                    if (delayImportTableDirectory.HasData && TryGetDirectoryChunk(delayImportTableDirectory, out var chunk))
                    {
                        using var results = new ValueList<ImageDelayLoadDescriptor>();

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

                    if (corHeaderTableDirectory.HasData && TryGetDirectoryChunk(corHeaderTableDirectory, out var chunk))
                    {
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

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ecmaMetadata = new EcmaMetadata(chunk);
                        }
                    }
                }

                return ecmaMetadata;
            }
        }

        /// <summary>
        /// Tries to get a pointer to the raw ECMA-335 metadata pointed to by the <see cref="ImageCor20Header.Metadata"/> section,
        /// allowing you to consume metadata using third party metadata readers, such those provided by System.Reflection.Metadata
        /// </summary>
        /// <param name="metadata">A pointer to the start of the ECMA-335 metadata directory</param>
        /// <param name="length">The length of the ECMA-335 metadata directory</param>
        /// <returns>True if ECMA-335 metadata could be found. Otherwise, false.</returns>
        public unsafe bool TryGetRawMetadata(out byte* metadata, out int length)
        {
            var cor20 = Cor20Header;

            if (cor20 != null)
            {
                var table = cor20.Metadata;

                if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
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

        private ManifestResource[]? cor20Resources;

        /// <summary>
        /// Gets the data pointed to by the <see cref="ImageCor20Header.Resources"/> directory.
        /// </summary>
        public ManifestResource[]? Cor20Resources
        {
            get
            {
                if (cor20Resources == null)
                {
                    var cor20 = Cor20Header;

                    if (cor20 != null)
                    {
                        var table = cor20.Resources;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            //The managed resources are described by the manifest resources table

                            var resourceTable = EcmaMetadata?.ModelHeap?.ManifestResourceTable;

                            if (resourceTable != null)
                            {
                                //Only rows with a RID of 0 should be processed (per nidump.cpp)
                                using var results = new ValueList<ManifestResource>();

                                foreach (var row in resourceTable)
                                {
                                    if (row.Implementation.RowId != 0)
                                        continue;

                                    var data = chunk.Slice(row.ResourceOffset);

                                    results.Add(new ManifestResource(row, data));
                                }

                                cor20Resources = results.ToArray();
                            }
                        }
                    }
                }

                return cor20Resources;
            }
        }

        #endregion
        #region Cor20StrongNameSignature

        private ByteBlob? cor20StrongNameSignature;

        /// <summary>
        /// Gets the data pointed to by the <see cref="ImageCor20Header.StrongNameSignature"/> directory.
        /// </summary>
        public ByteBlob? Cor20StrongNameSignature
        {
            get
            {
                if (cor20StrongNameSignature == null)
                {
                    var cor20 = Cor20Header;

                    if (cor20 != null)
                    {
                        var table = cor20.StrongNameSignature;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            cor20StrongNameSignature = new ByteBlob(chunk, table.Size, ViewKind.StrongNameSignature);
                        }
                    }
                }

                return cor20StrongNameSignature;
            }
        }

        #endregion
        #region Cor20CodeManagerTable

        private object? cor20CodeManagerTable;

        /// <summary>
        /// Gets the data pointed to by the <see cref="ImageCor20Header.CodeManagerTable"/> directory.
        /// </summary>
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

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
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

        private ImageCorVTableFixup[]? cor20VTableFixups;

        /// <summary>
        /// Gets the data pointed to by the <see cref="ImageCor20Header.VTableFixups"/> directory.
        /// </summary>
        public ImageCorVTableFixup[]? Cor20VTableFixups
        {
            get
            {
                if (cor20VTableFixups == null)
                {
                    var cor20 = Cor20Header;

                    if (cor20 != null)
                    {
                        var table = cor20.VTableFixups;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            var results = new ImageCorVTableFixup[table.Size / ImageCorVTableFixup.StructSize];

                            for (var i = 0; i < results.Length; i++)
                                results[i] = new ImageCorVTableFixup(chunk.Slice(i * ImageCorVTableFixup.StructSize));

                            cor20VTableFixups = results;
                        }
                    }
                }

                return cor20VTableFixups;
            }
        }

        #endregion
        #region Cor20ExportAddressTableJumps

        private object? cor20ExportAddressTableJumps;

        /// <summary>
        /// Gets the data pointed to by the <see cref="ImageCor20Header.ExportAddressTableJumps"/> directory.
        /// </summary>
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

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
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

        /// <summary>
        /// Gets the data pointed to by the <see cref="ImageCor20Header.ManagedNativeHeader"/> directory.<para/>
        /// If this assembly has been NGEN'd, this will be a <see cref="CorCompileHeader"/>. If this assembly has been R2R'd,
        /// this will be a <see cref="PESpy.R2R.ReadyToRunHeader"/>.
        /// </summary>
        public IValue? Cor20ManagedNativeHeader
        {
            get
            {
                if (cor20ManagedNativeHeader == null)
                {
                    var cor20 = Cor20Header;

                    //Files with a managed header should have COMIMAGE_FLAGS_IL_LIBRARY set. As per pedecoder.cpp,
                    //this name is a misnomer
                    if (cor20 != null && (cor20.Flags & COMIMAGE_FLAGS_IL_LIBRARY) != 0)
                    {
                        var table = cor20.ManagedNativeHeader;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            var sig = chunk.PeekUInt32(0);

                            switch (sig)
                            {
                                case CorCompileHeader.NGESignature:
                                    cor20ManagedNativeHeader = new CorCompileHeader(chunk);
                                    break;

                                case R2R.ReadyToRunHeader.R2RSignature:
                                    cor20ManagedNativeHeader = new R2R.ReadyToRunHeader(chunk);
                                    break;
                            }
                        }
                    }
                }

                return cor20ManagedNativeHeader;
            }
        }

        #endregion

        private ImageCorILMethodList? ilMethods;

        /// <summary>
        /// Gets the IL Methods pointed to by the MethodDef table in ECMA-335 metadata. If this <see cref="PEFile"/> does not contain
        /// any ECMA-335 metadata, or does not have a MethodDef table, this property returns <see langword="null"/>.
        /// </summary>
        public ImageCorILMethodList? ILMethods
        {
            get
            {
                if (ilMethods == null)
                {
                    //For some reason referencing MethodDefTable causes a type load exception to occur in the JIT. I think it's because ClassLoader::LoadTypeHandlerForTypeKey_Body
                    //gets upset that we're doing a recursive type load, because the row and table types reference each other
                    var methodDefs = EcmaMetadata?.ModelHeap?.MethodDefTable;

                    if (methodDefs == null)
                        return null;

                    ilMethods = new ImageCorILMethodList(methodDefs, this);
                }

                return ilMethods;
            }
        }

        /// <summary>
        /// Tries to get the <see cref="ImageCorILMethod"/> that is associated with a given method token.
        /// </summary>
        /// <param name="methodDef">The <see cref="mdMethodDef"/> token of the method whose data should be retrieved.</param>
        /// <param name="ilMethod">The <see cref="ImageCorILMethod"/> that is associated with the specified method token.</param>
        /// <returns><see langword="true"/> if the RVA of the metadata row pointed to by <paramref name="methodDef"/> could be resolved to an <see cref="ImageCorILMethod"/>. Otherwise, <see langword="false"/>.</returns>
        public bool TryGetILMethod(mdMethodDef methodDef, out ImageCorILMethod ilMethod)
        {
            var methodDefs = EcmaMetadata?.ModelHeap?.MethodDefTable;
            ilMethod = default;

            if (methodDefs == null)
                return false;

            if (methodDef.Rid > methodDefs.Count)
                return false;

            var row = methodDefs.FromToken(methodDef);

            if (!TryGetILValueChunk(row, out var valueChunk))
                return false;

            ilMethod = new ImageCorILMethod(valueChunk, out var isValid);

            if (isValid)
                return true;

            //You can have P/Invokes that say they have RVAs but these don't point to valid data
            ilMethod = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetILValueChunk(in Ecma335.MethodDefRow row, out MemoryChunk valueChunk)
        {
            var rva = row.RVA;
            valueChunk = default;

            return rva != 0 && (row.ImplFlags & CorMethodImpl.miNative) == 0 && TryGetValueChunkFromSection(rva, out valueChunk);
        }

        #endregion
        #endregion
        #region NGEN

        /// <summary>
        /// Gets the NGEN header that is pointed to by the <see cref="ImageCor20Header.ManagedNativeHeader"/> directory.
        /// </summary>
        public CorCompileHeader? NgenHeader => Cor20ManagedNativeHeader as CorCompileHeader;

        #region NgenHelperTable

        private NgenHelperEntry[]? ngenHelperTable;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.HelperTable"/> directory.
        /// </summary>
        public NgenHelperEntry[]? NgenHelperTable
        {
            get
            {
                if (ngenHelperTable == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.HelperTable;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            var read = 0;

                            var pointerSize = chunk.PointerSize;

                            using var results = new ValueList<NgenHelperEntry>();

                            while (read < table.Size)
                            {
                                var item = chunk.PeekUnmanaged<NgenHelperEntry>(read);

                                results.Add(item);

                                if (item.IsPointer)
                                    read += pointerSize;
                                else
                                    read += NgenHelperEntry.HELPER_TABLE_ENTRY_LEN;
                            }

                            ngenHelperTable = results.ToArray();
                        }
                    }
                }

                return ngenHelperTable;
            }
        }

        #endregion
        #region NgenImportSections

        private CorCompileImportSection[]? ngenImportSections;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.ImportSections"/> directory.
        /// </summary>
        public CorCompileImportSection[]? NgenImportSections
        {
            get
            {
                if (ngenImportSections == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.ImportSections;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            var results = new CorCompileImportSection[table.Size / CorCompileImportSection.StructSize];

                            for (var i = 0; i < results.Length; i++)
                                results[i] = new CorCompileImportSection(chunk.Slice(i * CorCompileImportSection.StructSize));

                            ngenImportSections = results;
                        }
                    }
                }

                return ngenImportSections;
            }
        }

        #endregion
        #region NgenImportTable

        private CorCompileImportTableEntry[]? ngenImportTable;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.ImportTable"/> directory.
        /// </summary>
        public CorCompileImportTableEntry[]? NgenImportTable
        {
            get
            {
                if (ngenImportTable == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.ImportTable;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            var results = new CorCompileImportTableEntry[table.Size / CorCompileImportTableEntry.StructSize];

                            for (var i = 0; i < results.Length; i++)
                                results[i] = new CorCompileImportTableEntry(chunk.Slice(i * CorCompileImportTableEntry.StructSize));

                            ngenImportTable = results;
                        }
                    }
                }

                return ngenImportTable;
            }
        }

        #endregion
        #region NgenStubsData

        private object? ngenStubsData;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.StubsData"/> directory.
        /// </summary>
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

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
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

        private CorCompileVersionInfo? ngenVersionInfo;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.VersionInfo"/> directory.
        /// </summary>
        public CorCompileVersionInfo? NgenVersionInfo
        {
            get
            {
                if (ngenVersionInfo == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.VersionInfo;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenVersionInfo = new CorCompileVersionInfo(chunk);
                        }
                    }
                }

                return ngenVersionInfo;
            }
        }

        #endregion
        #region NgenDependencies

        private CorCompileDepepdency[]? ngenDependencies;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.Dependencies"/> directory.
        /// </summary>
        public CorCompileDepepdency[]? NgenDependencies
        {
            get
            {
                if (ngenDependencies == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.Dependencies;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            var results = new CorCompileDepepdency[table.Size / CorCompileDepepdency.StructSize];

                            for (var i = 0; i < results.Length; i++)
                                results[i] = new CorCompileDepepdency(chunk.Slice(i * CorCompileDepepdency.StructSize));

                            ngenDependencies = results;
                        }
                    }
                }

                return ngenDependencies;
            }
        }

        #endregion
        #region NgenDebugMap

        private int[]? ngenDebugMap;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.DebugMap"/> directory.
        /// </summary>
        public int[]? NgenDebugMap
        {
            get
            {
                if (ngenDebugMap == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.DebugMap;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            //Seems to relate to the .dbgmap section

                            //The map consists of an array of CORCOMPILE_DEBUG_RID_ENTRY entries
                            //This is a typedef of CORCOMPILE_DEBUG_ENTRY
                            //which is itself a typedef of ULONG

                            ngenDebugMap = chunk.PeekNativeSpan<int>(0, table.Size / sizeof(int)).ToArray();
                        }
                    }
                }

                return ngenDebugMap;
            }
        }

        #endregion
        #region NgenModuleImage

        private ByteBlob? ngenModuleImage;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.ModuleImage"/> directory.
        /// </summary>
        public ByteBlob? NgenModuleImage
        {
            get
            {
                if (ngenModuleImage == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.ModuleImage;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            //The module directory consists of an internal CLR Module data structure persisted to disk.
                            //I don't feel like this would be a backwards compatible data structure; either way, too complex
                            //for now so we'll just return a byte blob

                            ngenModuleImage = new ByteBlob(chunk, table.Size, ViewKind.ModuleImage);
                        }
                    }
                }

                return ngenModuleImage;
            }
        }

        #endregion
        #region NgenCodeManagerTable

        private CorCompileCodeManagerEntry? ngenCodeManagerTable;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.CodeManagerTable"/> directory.
        /// </summary>
        public CorCompileCodeManagerEntry? NgenCodeManagerTable
        {
            get
            {
                if (ngenCodeManagerTable == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.CodeManagerTable;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            ngenCodeManagerTable = new CorCompileCodeManagerEntry(chunk);
                        }
                    }
                }

                return ngenCodeManagerTable;
            }
        }

        #endregion
        #region NgenProfileDataList

        private object? ngenProfileDataList;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.ProfileDataList"/> directory.
        /// </summary>
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

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
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

        private EcmaMetadata? ngenManifestMetaData;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.ManifestMetaData"/> directory.
        /// </summary>
        public EcmaMetadata? NgenManifestMetaData
        {
            get
            {
                if (ngenManifestMetaData == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.ManifestMetaData;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                            ngenManifestMetaData = new EcmaMetadata(chunk);
                    }
                }

                return ngenManifestMetaData;
            }
        }

        #endregion
        #region NgenVirtualSectionsTable

        private CorCompileVirtualSectionInfo[]? ngenVirtualSectionsTable;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.VirtualSectionsTable"/> directory.
        /// </summary>
        public CorCompileVirtualSectionInfo[]? NgenVirtualSectionsTable
        {
            get
            {
                if (ngenVirtualSectionsTable == null)
                {
                    var ngen = NgenHeader;

                    if (ngen != null)
                    {
                        var table = ngen.VirtualSectionsTable;

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            var results = new CorCompileVirtualSectionInfo[table.Size / CorCompileVirtualSectionInfo.StructSize];

                            for (var i = 0; i < results.Length; i++)
                                results[i] = new CorCompileVirtualSectionInfo(chunk.Slice(i * CorCompileVirtualSectionInfo.StructSize));

                            ngenVirtualSectionsTable = results;;
                        }
                    }
                }

                return ngenVirtualSectionsTable;
            }
        }

        #endregion
        #region NgenEEInfoTable

        private object? ngenEEInfoTable;

        /// <summary>
        /// Gets the data pointed to by the NGEN <see cref="CorCompileHeader.EEInfoTable"/> directory.
        /// </summary>
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

                        if (table.HasData && TryGetDirectoryChunk(table, out var chunk))
                        {
                            //I haven't found a file that has this yet; in my NGEN sample, all of the bytes were 0 which makes it hard to verify anything

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

        /// <summary>
        /// Gets the R2R header that is pointed to by the <see cref="ImageCor20Header.ManagedNativeHeader"/> directory.
        /// </summary>
        public R2R.ReadyToRunHeader? ReadyToRunHeader => Cor20ManagedNativeHeader as R2R.ReadyToRunHeader;

        #endregion
        #region AppHost

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AppHostSignature? appHostSignature;

        private bool hasTriedAppHostSignature;

        /// <summary>
        /// Gets the signature that denotes that this <see cref="PEFile"/> was constructed from the .NET Core apphost.exe executable.<para/>
        /// If this <see cref="PEFile"/> represents a Single File App, the AppHost Signature additionally provides access to the bundle manifest
        /// that describes the files embedded within the app.
        /// </summary>
        public unsafe AppHostSignature? AppHostSignature
        {
            get
            {
                /* The way that the bundle marker gets embedded is that it is defined as static data
                 * in a function bundle_marker_t.header_offset(), in exe_main bundle_marker_t::is_bundle() is called,
                 * which calls into header_offset(), which then casts the byte array as a struct in order to get at the first 8 bytes
                 * to see if they're non 0
                 * https://github.com/dotnet/runtime/blob/e572463b5706b0509fe0c524d9d09893e7e252da/src/native/corehost/apphost/bundle_marker.h
                 * https://github.com/dotnet/runtime/blob/e572463b5706b0509fe0c524d9d09893e7e252da/src/native/corehost/apphost/bundle_marker.cpp
                 *
                 * The practical effect of this is that the bundle marker is injected into the .data section. There is no requirement that the .data
                 * section be used. From our perspective, this basically creates a challenge for us because for a remote debug target, we essentially
                 * have to copy the whole thing into our memory just to check whether the signature exists.
                 * 
                 * NativeAOT builds do not appear to use AppHost; the logic that main implements for them is quite different. In the event you've got
                 * A NativeAOT app that includes logic for parsing the AppHost, that code will be located in DehydratedData pseudo-R2R section of
                 * the .rdata section. As such, for now we will limit the scope of our search to the .data section
                 */

                if (appHostSignature == null && !hasTriedAppHostSignature)
                {
                    //To avoid false positives (wherein we're dealing with an application that merely _reads other applications
                    //that contain an apphost signature_ we implement the following heuristics: if it's not an EXE, it can't be
                    //an apphost, and if it contains a managed header it can't be an apphost either

                    if ((FileHeader.Characteristics & IMAGE_FILE.IMAGE_FILE_DLL) != 0 || Cor20Header != null)
                    {
                        //Another possible check could be checking whether we've got a codeview entry that targets apphost.pdb
                        hasTriedAppHostSignature = true;
                        return null;
                    }

                    //We'll block the debugger from hanging in AppHostSignature.FindBundleHeader

                    var sections = SectionHeaders;

                    for (var i = 0; i < sections.Length; i++)
                    {
                        ref var section = ref sections[i];

                        if (section.Name == ".data")
                        {
                            var block = GetSectionBlock(i, section);

                            var index = AppHostSignature.FindBundleHeader(block.LocalPointer, block.Length);

                            if (index != -1)
                            {
                                appHostSignature = new AppHostSignature(block, index);
                                break;
                            }
                        }
                    }

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

                    /* CLR_ENGINE_METRICS are exported by g_CLREngineMetrics, however they're also found at ordinal 2.
                     * You can locate metrics much faster by just jumping straight to the target ordinal rather than trying
                     * to specifically search for g_CLREngineMetrics. However, the issue we have is that we need to be able
                     * to parse any random PE File, so just blindly trusting ordinal 2 isn't going to cut it */

                    var exportTable = ExportTable;

                    const int kOrdinalForMetrics = 2;

                    if (exportTable != null)
                    {
                        var realIndex = kOrdinalForMetrics - exportTable.Base;

                        if (unchecked((uint) realIndex < exportTable.NumberOfFunctions))
                        {
                            var rvaOfRva = exportTable.RawAddressOfFunctions + (realIndex * sizeof(int));

                            if (TryGetValueChunkFromSection(rvaOfRva, out var chunk))
                            {
                                var rva = chunk.PeekInt32(0);

                                if (TryGetValueChunkFromSection(rva, out var valueChunk) && valueChunk.PeekInt32(0) == ClrEngineMetrics.StructSize(Is32Bit))
                                {
                                    //It's looking good that this might be g_CLREngineMetrics, but now let's actually check that
                                    //ordinal 2 actually is g_CLREngineMetrics

                                    //Doesn't seem like the base matters; if the base is 2, g_CLREngineMetrics is still at ordinal 2
                                    //after factoring in ordinal + base (which is what export.Ordinal shows)
                                    if (exportTable.TryGetExport("g_CLREngineMetrics", out export) == true && !export.ForwardOrAddress.IsForward && export.Ordinal == 2)
                                        clrEngineMetrics = new ClrEngineMetrics(valueChunk);
                                }
                            }
                        }
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
        private RuntimeInfo? dotNetRuntimeInfo;
        private bool hasTriedDotNetRuntimeInfo;

        /// <summary>
        /// Gets the structure pointed to by the "DotNetRuntimeInfo" export that describes the CLR, DAC and DBI versions that are associated
        /// with this executable.
        /// </summary>
        public RuntimeInfo? DotNetRuntimeInfo
        {
            get
            {
                if (dotNetRuntimeInfo == null && !hasTriedDotNetRuntimeInfo)
                {
                    ImageExportDirectory.Export export = default;

                    if (ExportTable?.TryGetExport("DotNetRuntimeInfo", out export) == true && !export.ForwardOrAddress.IsForward)
                    {
                        var rva = export.ForwardOrAddress.Address;

                        if (TryGetValueChunkFromSection(rva, out var valueChunk))
                        {
                            dotNetRuntimeInfo = new RuntimeInfo(valueChunk);

                            if (dotNetRuntimeInfo.Signature != "DotNetRuntimeInfo")
                                dotNetRuntimeInfo = default;
                        }
                    }

                    hasTriedDotNetRuntimeInfo = true;
                }

                return dotNetRuntimeInfo;
            }
        }

        #endregion
        #region Native AOT

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private NativeAOT.DotNetRuntimeDebugHeader? dotNetRuntimeDebugHeader;
        private bool hasTriedDotNetRuntimeDebugHeader;

        /// <summary>
        /// Gets the structure pointed to by the "DotNetRuntimeDebugHeader" export that provides debugging information for Native AOT executables.
        /// </summary>
        public NativeAOT.DotNetRuntimeDebugHeader? DotNetRuntimeDebugHeader
        {
            get
            {
                if (dotNetRuntimeDebugHeader == null && !hasTriedDotNetRuntimeDebugHeader)
                {
                    ImageExportDirectory.Export export = default;

                    if (ExportTable?.TryGetExport("DotNetRuntimeDebugHeader", out export) == true && !export.ForwardOrAddress.IsForward)
                    {
                        if (TryGetValueChunkFromSection(export.ForwardOrAddress.Address, out var chunk))
                            dotNetRuntimeDebugHeader = new NativeAOT.DotNetRuntimeDebugHeader(chunk);
                    }

                    hasTriedDotNetRuntimeDebugHeader = true;
                }

                return dotNetRuntimeDebugHeader;
            }
        }

        private NativeAOTModulesList? nativeAOTModules;
        private bool hasTriedNativeAOTModules;

        /// <summary>
        /// Gets the <see cref="NativeAOT.ReadyToRunHeader"/> module headers, if this is a NativeAOT executable.<para/>
        /// This member will attempt to query for symbols, which may cause a delay in accessing this member.<para/>
        /// In the event that symbols are not available, if a NativeAOT <see cref="DotNetRuntimeDebugHeader"/> is present,
        /// this property will attempt to locate the region that points to the <see cref="NativeAOT.ReadyToRunHeader"/> items.<para/>
        /// Due to the way in which the linker injects this information into the executable, this list may contain
        /// "null" entries, which the caller is responsible for skipping over.
        /// </summary>
        public NativeAOTModulesList? NativeAOTModules => GetNativeAOTModules(debugger: false);

        //If the debugger is asking whether we have any modules, disallow performing expensive operations that would upset the debugger
        internal unsafe NativeAOTModulesList? GetNativeAOTModules(
            bool debugger,
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress progress = null)
        {
            if (nativeAOTModules == null && !hasTriedNativeAOTModules)
            {
                if (!debugger && DotNetRuntimeDebugHeader == null)
                {
                    //Don't waste time loading symbols when we know we're not NativeAOT
                    hasTriedNativeAOTModules = true;
                    return null;
                }

                if (TryGetSymbolReader(debugger, httpPolicy, progress, out var symbolReader))
                {
                    nativeAOTModules = symbolReader.NativeAOTModules;
                    hasTriedNativeAOTModules = true;
                }
                else
                {
                    if (debugger)
                    {
                        //We're going to need to do a series of KMP Searches, which we can't be doing under the debugger
                        Debugger.NotifyOfCrossThreadDependency();
                        return null;
                    }
                    else
                    {
                        /* It looks like we're a NativeAOT file, so KMP Search for a NativeAOT ReadyToRunHeader. We're interested in finding
                         * something that has the Signature "R2R", and EntryType 1 (which all headers are currently hardcoded to have) */

                        var sectionHeaders = SectionHeaders;

                        for (var i = 0; i < sectionHeaders.Length; i++)
                        {
                            ref var sectionHeader = ref sectionHeaders[i];

                            if (sectionHeader.Name == ".rdata")
                            {
                                var block = GetSectionBlock(i, sectionHeader);

                                if (TryFindNativeAOTModuleHeader(block, out var offset))
                                {
                                    //If this fails, we tried our best!
                                    TryFindNativeAOTModuleHeaderList(sectionHeader, block, offset, out nativeAOTModules);
                                }

                                break;
                            }
                        }

                        hasTriedNativeAOTModules = true;
                    }
                }
            }

            return nativeAOTModules;
        }

        private unsafe bool TryFindNativeAOTModuleHeader(MemoryBlock block, out int offset)
        {
            var sig = NativeAOT.ReadyToRunHeader.R2RSignature;
            var sigSpan = new Span<byte>((byte*) &sig, sizeof(int));

            var read = 0;
            offset = default;

            while (read < block.Length)
            {
                var start = block.LocalPointer + read;
                var remaining = block.Length - read;
                var index = PESpy.AppHostSignature.KMPSearch(sigSpan, start, remaining);

                if (index == -1)
                    return false;

                //We've got a potential match; check if it fits in the remaining area
                var pReadyToRunHeader = start + index;
                var readyToRunAvailableLength = remaining - index;

                if (readyToRunAvailableLength < NativeAOT.ReadyToRunHeader.FixedStructSize)
                    return false; //Not only will a ReadyToRunHeader not fit, but there isn't going to be enough bytes after us to try another match either

                //As of writing, EntryType is hardcoded to always be 1, which makes for a good sanity check
                var entryType = *(pReadyToRunHeader + NativeAOT.ReadyToRunHeader.EntryTypeOffset);

                if (entryType != 1)
                    continue;

                offset = read + index;
                return true;
            }

            return false;
        }

        private unsafe bool TryFindNativeAOTModuleHeaderList(
            in ImageSectionHeader sectionHeader,
            MemoryBlock block,
            int offset,
            out NativeAOTModulesList list)
        {
            //This is looking like a ReadyToRunHeader. Now, try and find a VA pointing to this entry
            //also in the .rdata section
            Span<byte> vaSpan;
            list = default;

            var imageBase = OptionalHeader.ImageBase;

            if (Is32Bit)
            {
                var va = (uint) imageBase + sectionHeader.VirtualAddress + offset;
                vaSpan = new Span<byte>((byte*) &va, sizeof(int));
            }
            else
            {
                var va = imageBase + sectionHeader.VirtualAddress + offset;
                vaSpan = new Span<byte>((byte*) &va, sizeof(long));
            }

            var read = 0;

            while (read < block.Length)
            {
                var start = block.LocalPointer + read;
                var remaining = block.Length - read;
                var index = PESpy.AppHostSignature.KMPSearch(vaSpan, start, remaining);

                if (index == -1)
                    return false;

                /* We've got a potential candidate. We expect to have a sequence of VA's pointing to
                 * NativeAOT.ReadyToRunHeader instances, separated by 0's. Our first step is to iterate
                 * forwards and backwards to sanity check that all non-0 values that we see are indeed
                 * VA's that point to additional NativeAOT.ReadyToRunHeader instances. Once we've done
                 * that, we can expand out further in both directions trying to find the first and last
                 * position that points to a valid module header */

                var midpointChunk = new MemoryChunk(block, read + index);

                var pointerSize = Is32Bit ? 4 : 8;

                //Skip over the midpoint
                var skipped = pointerSize;

                var lastValueWasZero = false;

                //Find additional items after us
                while (skipped < midpointChunk.Remaining)
                {
                    var value = midpointChunk.PeekPointer(skipped);

                    if (value == 0)
                    {
                        //Two zero's in a row; I don't think that would occur; I'm expecting
                        //when module contributions are aggregated together there'll just be a single 0 between
                        //them (this is just a guess though)
                        if (lastValueWasZero)
                            break;

                        lastValueWasZero = true;
                    }
                    else
                    {
                        if (!IsValidNativeAOTModuleHeader((long) value))
                            break;

                        lastValueWasZero = false;
                    }

                    skipped += pointerSize;
                }

                var numItemsAfter = (skipped / pointerSize) - 1; //Subtract 1 because we had to skip over the midpoint to start with
                skipped = pointerSize;

                lastValueWasZero = false;

                //Find additional items before us
                while (skipped < midpointChunk.RelativeOffset)
                {
                    var value = midpointChunk.PeekPointer(-skipped);

                    if (value == 0)
                    {
                        //Two zero's in a row; I don't think that would occur; I'm expecting
                        //when module contributions are aggregated together there'll just be a single 0 between
                        //them (this is just a guess though)
                        if (lastValueWasZero)
                            break;

                        lastValueWasZero = true;
                    }
                    else
                    {
                        if (!IsValidNativeAOTModuleHeader((long) value))
                            break;

                        lastValueWasZero = false;
                    }

                    skipped += pointerSize;
                }

                var numItemsBefore = (skipped / pointerSize) - 1; //Subtract 1 because we had to skip over the midpoint to start with

                //The first item is __modules_a and the last item is __modules_z which is not included in the count
                //You can't slice backwards, because we cast the offset to uint
                var chunkStart = new MemoryChunk(block, midpointChunk.RelativeOffset - (numItemsBefore * pointerSize));
                list = new NativeAOTModulesList(chunkStart, numItemsBefore + numItemsAfter); //We want to do numitemsBefore + numItemsAfter + 1 - 1 to include the midpoint and exclude __modules_z, which cancels out to just numItemsBefore + numItemsAfter
                return true;
            }

            return false;
        }

        //Check this is a valid VA, and that it points to something with enough bytes remaining to be
        //a ReadyToRunHeader, that it has the R2R signature and has EntryType 1
        private bool IsValidNativeAOTModuleHeader(long va)
        {
            if (TryGetValueChunkFromVA(va, out var memoryChunk) && NativeAOT.ReadyToRunHeader.FixedStructSize < memoryChunk.Remaining)
            {
                var readyToRunHeader = new NativeAOT.ReadyToRunHeader(memoryChunk);

                //EntryType is currently always known to be 1
                if (readyToRunHeader.Signature == NativeAOT.ReadyToRunHeader.R2RSignature && readyToRunHeader.EntryType == 1)
                    return true;
            }

            return false;
        }

        #endregion
        #region RTTICompleteObjectLocators

        public SymbolValueList<RTTICompleteObjectLocator>? RTTICompleteObjectLocators => GetRTTICompleteObjectLocators(debugger: false);

        internal SymbolValueList<RTTICompleteObjectLocator>? GetRTTICompleteObjectLocators(
            bool debugger,
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress progress = null)
        {
            if (TryGetSymbolReader(debugger, httpPolicy, progress, out var symbolReader))
                return symbolReader.RTTICompleteObjectLocators;

            return null;
        }

        #endregion
        #region Vftables

        public SymbolValueList<VftableInfo>? Vftables => GetVftables(debugger: false);

        internal SymbolValueList<VftableInfo>? GetVftables(
            bool debugger,
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress progress = null)
        {
            if (TryGetSymbolReader(debugger, httpPolicy, progress, out var symbolReader))
                return symbolReader.Vftables;

            return null;
        }


        #endregion
        #region RpcInfo

        private RpcInfo? rpcInfo;
        private bool hasTriedRpcInfo;

        public RpcInfo? RpcInfo
        {
            get
            {
                if (rpcInfo == null && !hasTriedRpcInfo)
                {
                    //I think we should only try RPC info if we actually import rpcrt4.dll.
                    //This is also the dll name in Windows 95. The only risk we have with
                    //this approach is if a DLL decides to import RPC functions via an API
                    //set rather than directly

                    var importTable = ImportTable;

                    if (importTable == null)
                    {
                        hasTriedRpcInfo = true;
                        return null;
                    }

                    for (var i = 0; i < importTable.Length; i++)
                    {
                        ref var imageImportDescriptor = ref importTable[i];

                        if (imageImportDescriptor.Name.IsValid && imageImportDescriptor.Name.Value.EqualsIgnoreCase("rpcrt4.dll"))
                        {
                            rpcInfo = RpcInfo.Parse(this);
                            hasTriedRpcInfo = true;
                            break;
                        }
                    }
                }

                return rpcInfo;
            }
        }

        #endregion
        #region VB

        private ExeProjectInfo? exeProjectInfo;
        private bool hasTriedExeProjectInfo;

        /// <summary>
        /// Gets the Visual Basic header that identifies this executable as a VB5/6 application.
        /// </summary>
        public ExeProjectInfo? ExeProjectInfo
        {
            get
            {
                if (exeProjectInfo == null && !hasTriedExeProjectInfo)
                {
                    /* In a Visual Basic 5/6 EXE, the entry point points to a stub that loads
                     * the EXEPROJECTINFO header and then calls msvbvm<version>!ThunRTMain. On this basis,
                     * a VB5/6 EXE can be detected based on the presence of a "push" against a VA, followed
                     * by a "call", where the target of the "push" starts with the VB5 magic bytes. (VB6 uses the same
                     * magic bytes as VB5) */

                    var entryPoint = OptionalHeader.AddressOfEntryPoint;

                    if (entryPoint != 0 && TryGetValueChunkFromSection(entryPoint, out var valueChunk) && valueChunk.Remaining >= 10)
                    {
                        //VB 5/6 were only ever 32-bit
                        const int pushPrefix = 0x68;
                        const int callPrefix = 0xE8;

                        if (valueChunk.PeekByte(0) == pushPrefix && valueChunk.PeekByte(5) == callPrefix)
                        {
                            var va = valueChunk.PeekInt32(1);

                            if (TryGetValueChunkFromVA(va, out valueChunk) && valueChunk.Remaining >= ExeProjectInfo.StructSize && valueChunk.PeekUInt32(0) == ExeProjectInfo.VBMagic)
                            {
                                exeProjectInfo = new ExeProjectInfo(valueChunk);
                            }
                        }
                    }

                    hasTriedExeProjectInfo = true;
                }

                return exeProjectInfo;
            }
        }

        #endregion

        private FileAccessor? _viewAccessorPhysical;
        private FileAccessor? _viewAccessorVirtual;

        public FileView GetView(in FileAnalyzerOptions options = default) =>
            GetView(ViewMode.Default, options);

        public FileView GetView(
            ViewMode viewMode,
            in FileAnalyzerOptions options = default)
        {
            var wantVirtual = viewMode switch
            {
                ViewMode.Default => IsLoadedImage,
                ViewMode.Physical => false,
                ViewMode.Virtual => true
            };

            var viewAccessor = wantVirtual ? _viewAccessorVirtual : _viewAccessorPhysical;

            if (viewAccessor == null)
            {
                viewAccessor = new PEFileAccessor(this, viewMode);
                FileAnalyzer.Analyze(viewAccessor, options);
                ((PEFileAccessor) viewAccessor).OwnsPEFile = false;

                if (wantVirtual)
                    _viewAccessorVirtual = viewAccessor;
                else
                    _viewAccessorPhysical = viewAccessor;
            }

            return viewAccessor.GetFileView(viewMode);
        }

        private ISymbolAccessor? symbolAccessor;

        public ISymbolAccessor GetSymbolAccessor(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (symbolAccessor != null)
                return symbolAccessor;

            if (Locator.TryLocate(this, out var artifacts, out _, httpPolicy: httpPolicy, progress: progress, cancellationToken: cancellationToken))
            {
                switch (artifacts.BestKind)
                {
                    case Locator.ArtifactKind.PDB:
                        if (Detector.TryOpenFile(artifacts.PDBPath, out var pdbFile))
                        {
                            if (pdbFile.Kind == FileKind.PDB)
                                ((PDBFile) pdbFile).SetFallbackSectionHeaders(SectionHeaders);

                            //Even if it's not actually a PDBFile, it's still an IFile so it may have an ISymbolAccessor
                            symbolAccessor = new ExternalFileSymbolAccessor(pdbFile);
                        }
                        else
                            symbolAccessor = NullSymbolAccessor.Instance;

                        return symbolAccessor;

                    case Locator.ArtifactKind.DBG:
                        if (Detector.TryOpenFile(artifacts.DBGPath, out var dbgFile))
                        {
                            //Even if it's not actually a DBGFile, it's still an IFile so it may have an ISymbolAccessor
                            symbolAccessor = new ExternalFileSymbolAccessor(dbgFile);
                        }
                        else
                            symbolAccessor = NullSymbolAccessor.Instance;

                        return symbolAccessor;

                    case Locator.ArtifactKind.EmbeddedPortablePdb:
                        symbolAccessor = new PortablePDBFileSymbolAccessor(PortablePDBFile.FromEmbeddedFile((EmbeddedPortablePdb) DebugTable![artifacts.EmbeddedPortablePdbIndex!.Value].Data!));
                        break;

                    case Locator.ArtifactKind.SYM:
                        if (Detector.TryOpenFile(artifacts.SYMPath, out var symFile))
                        {
                            //Even if it's not actually a PDBFile, it's still an IFIle so it may have an ISymbolAccessor
                            symbolAccessor = new ExternalFileSymbolAccessor(symFile);
                        }
                        else
                            symbolAccessor = NullSymbolAccessor.Instance;

                        return symbolAccessor;

                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                //No external symbols; try internal. Prefer CodeView, fallback to COFF
                if (!ImageDebugDirectory.TryGetSymbolAccessor(this, debugTable, out symbolAccessor))
                {
                    //I don't know if it's guaranteed that if you have COFF symbols that they'll be pointed to by an
                    //IMAGE_DEBUG_TYPE_COFF debug directory entry, so just check the image file header anyway

                    var coff = FileHeader.PointerToSymbolTable.ValueOrDefault;

                    if (coff != null)
                    {
                        symbolAccessor = new CoffSymbolAccessor(coff, SectionHeaders);
                        return symbolAccessor;
                    }
                }
                else
                    return symbolAccessor;
            }

            if (httpPolicy != LocatorHttpPolicy.All)
            {
                //If we didn't try our hardest, don't set the symbol accessor, so we can potentially try again harder next time
                return NullSymbolAccessor.Instance;
            }

            //Fail: use NullSymbolAccessor
            symbolAccessor = NullSymbolAccessor.Instance;
            return symbolAccessor;
        }

        private SymbolReader? _symbolReader;
        private bool _hasTriedSymbolReader;

        /// <summary>
        /// Sets the <see cref="PDBFile"/> that this <see cref="PEFile"/> should
        /// use inside of its <see cref="ISymbolAccessor"/>. This method allows
        /// you to use the <see cref="PDBFile"/> that you've already located, without
        /// forcing the <see cref="PEFile"/> to try and locate the <see cref="PDBFile"/>
        /// again from scratch.<para/>
        /// 
        /// If no <see cref="ISymbolAccessor"/> has been loaded, or the <see cref="PEFile"/>
        /// failed to find any meaningful symbol source, the <see cref="ISymbolAccessor"/>
        /// will be overwritten with a new <see cref="ISymbolAccessor"/> that encapsulates
        /// the specified <see cref="PDBFile"/>. Otherwise, whatever <see cref="ISymbolAccessor"/>
        /// the <see cref="PEFile"/> already has will be left in place.<para/>
        /// 
        /// If this method returns true, this method may also ahve cleared any properties that are best located using symbols that
        /// we may have used fallback locator logic to detect. Ownership of the <see cref="PDBFile"/> transfers to the <see cref="PEFile"/>,
        /// and will be closed when the <see cref="PEFile"/> is disposed.
        /// </summary>
        /// <param name="pdbFile">The <see cref="PDBFile"/> that pertains to this <see cref="PEFile"/>.</param>
        /// <returns>True if the <see cref="ISymbolAccessor"/> was replaced with a new accessor
        /// based on the specified <see cref="PDBFile"/>. Otherwise, false.</returns>
        public bool SetPDBFile(PDBFile pdbFile)
        {
            if (symbolAccessor == null || symbolAccessor is NullSymbolAccessor)
            {
                //If we're NativeAOT, we can now detect the location of NativeAOT modules properly
                if (DotNetRuntimeDebugHeader != null)
                {
                    hasTriedNativeAOTModules = true;
                    nativeAOTModules = null;
                }

                symbolAccessor = new ExternalFileSymbolAccessor(pdbFile);
                return true;
            }

            return false;
        }

        internal bool TryGetSymbolReader(bool debugger, LocatorHttpPolicy httpPolicy, ILocatorProgress? progress, out SymbolReader? symbolReader)
        {
            if (_symbolReader != null)
            {
                symbolReader = _symbolReader;
                return true;
            }

            if (_hasTriedSymbolReader)
            {
                symbolReader = default;
                return false;
            }

            var symbolAccessor = GetSymbolAccessor(debugger ? LocatorHttpPolicy.None : httpPolicy, progress);

            if (symbolAccessor is not NullSymbolAccessor)
            {
                //We successfully got the "real" symbol accessor!

                if (SymbolReader.TryCreate(this, symbolAccessor, out symbolReader))
                {
                    _hasTriedSymbolReader = true;
                    _symbolReader = symbolReader;
                    return true;
                }

                //It's not a SymbolReader kind that we support (e.g. there's nothing for us to do with a Portable PDB)
                _hasTriedSymbolReader = true;
                return false;
            }
            else
            {
                if (debugger)
                {
                    //We're not in a position to get the ISymbolAccessor right now (we might need to
                    //make a HTTP request to get symbols)
                    Debugger.NotifyOfCrossThreadDependency();
                }

                //Don't set _hasTriedSymbolReader
                symbolReader = default;
                return false;
            }
        }

        private unsafe ViewWriter GetViewWriter(ViewMode mode)
        {
            var writer = new ViewWriter(new PEFileViewWriterHelper(this, mode), CreateByteViewProvider(null), mode);

            return writer;
        }

        ByteViewProvider IFileInternal.CreateByteViewProvider(FileAccessor fileAccessor) => CreateByteViewProvider(fileAccessor);

        internal unsafe ByteViewProvider CreateByteViewProvider(FileAccessor fileAccessor)
        {
            if (blockProvider is LocalMemoryBlockProvider l)
                return new LocalByteViewProvider(l.Pointer, (int) l.Length, fileAccessor);

            return new RemoteByteViewProvider(this, fileAccessor);
        }

        //Provides MemoryBlock objects which encompass an area of a PEFile
        internal readonly IMemoryBlockProvider blockProvider;

        //A special MemoryBlock containing the PE File header. We bypass the IMemoryBlockProvider
        //and create this directly since we need a special MemoryBlock implementation with special behaviors
        //just to get things going
        private HeaderMemoryBlock headerBlock;
        private MemoryBlock[]? sectionBlocks;
        private bool disposed;

        [DebuggerDisplay("0x{Start.ToString(\"X\"),nq}-0x{End.ToString(\"X\"),nq}")]
        internal readonly struct SectionRange
        {
            public readonly int Start;
            public readonly int End;

            internal SectionRange(int virtualAddress, int virtualSize)
            {
                Start = virtualAddress;
                End = virtualAddress + virtualSize;
            }
        }

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

        internal PEFile(string fileName, in MemoryMappedFileHolder mmf, bool isLoadedImage = false, string name = null)
        {
            IsLoadedImage = isLoadedImage;

            FileName = fileName;
            Name = name ?? Path.GetFileName(fileName);

            try
            {
                var localProvider = new LocalMemoryBlockProvider(mmf, this);
                blockProvider = localProvider;
                headerBlock = new LocalHeaderMemoryBlock(localProvider);
                InitializeHeaders();
                localProvider.is32Bit = OptionalHeader.Magic != PEMagic.IMAGE_NT_OPTIONAL_HDR64_MAGIC;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal PEFile(string fileName, in MemoryMappedFileHolder mmf, int startOffset)
        {
            IsLoadedImage = false;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            try
            {
                var localProvider = new NestedMemoryBlockProvider(mmf, this, startOffset);
                blockProvider = localProvider;
                headerBlock = new LocalHeaderMemoryBlock(localProvider, startOffset);
                InitializeHeaders();
                localProvider.is32Bit = OptionalHeader.Magic == PEMagic.IMAGE_NT_OPTIONAL_HDR32_MAGIC;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        //ctor for initializing PEFile from an IMemoryReader that reads remote memory
        private PEFile(IMemoryAccessor memoryAccessor, long address, bool isLoadedImage, string? fileName)
        {
            try
            {
                if (fileName != null)
                {
                    FileName = fileName;
                    Name = Path.GetFileName(fileName);
                }

                IsLoadedImage = isLoadedImage;

                var remoteProvider = new RemoteMemoryBlockProvider(memoryAccessor, address, this);
                blockProvider = remoteProvider;

                //Will automatically demand
                headerBlock = new RemoteHeaderMemoryBlock(memoryAccessor, address, blockProvider);

                InitializeHeaders();

                remoteProvider.is32Bit = OptionalHeader.Magic == PEMagic.IMAGE_NT_OPTIONAL_HDR32_MAGIC;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void InitializeHeaders()
        {
            dosHeader = new ImageDosHeader(new MemoryChunk(headerBlock, 0));
            ntHeaders = new ImageNtHeaders(new MemoryChunk(headerBlock, dosHeader.FileAddressOfNewExeHeader));

            ImageSectionHeader[] sectionHeaders;
            SectionRange[] sectionRanges;

            //ImageOptionalHeader cannot be meaningfully read until the pointer size is known
            switch (ntHeaders.OptionalHeader.Magic)
            {
                case PEMagic.IMAGE_NT_OPTIONAL_HDR64_MAGIC:
                    sectionHeaders = SectionHeaders;
                    sectionRanges = new SectionRange[sectionHeaders.Length];

                    for (var i = 0; i < sectionHeaders.Length; i++)
                    {
                        ref var sectionHeader = ref sectionHeaders[i];

                        sectionRanges[i] = new SectionRange(sectionHeader.VirtualAddress, sectionHeader.VirtualSize);
                    }
                    break;

                //I've observed in very old PE Files (e.g. files from Win32s) that the VirtualSize of each IMAGE_SECTION_HEADER
                //may be empty. In this scenario, all attempts to do GetSectionContainingRVA will fail. Since I've also observed
                //that the Magic of these older files is 0, we'll try and use that to determine whether we need to use alternate
                //logic to calculate the bounds of each section
                case 0:
                    headerBlock.Is32Bit = true;
                    sectionHeaders = SectionHeaders;
                    sectionRanges = new SectionRange[sectionHeaders.Length];

                    for (var i = 0; i < sectionHeaders.Length; i++)
                    {
                        ref var sectionHeader = ref sectionHeaders[i];

                        if (i == sectionHeaders.Length - 1)
                            sectionRanges[i] = new SectionRange(sectionHeader.VirtualAddress, ntHeaders.OptionalHeader.SizeOfImage - sectionHeader.VirtualAddress);
                        else
                            sectionRanges[i] = new SectionRange(sectionHeader.VirtualAddress, sectionHeaders[i + 1].VirtualAddress - sectionHeader.VirtualAddress);
                    }
                    break;

                default:
                    headerBlock.Is32Bit = true;
                    goto case PEMagic.IMAGE_NT_OPTIONAL_HDR64_MAGIC;
            }

            _sectionRanges = sectionRanges;

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

            //If it seems we're about to throw, do one last check that we're not dealing with a section whose listed size is 0
            if (!canCrossSectionBoundary && entry.Size > section.VirtualSize - relativeOffset && (entry.VirtualAddress + entry.Size > SectionRanges[sectionIndex].End))
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

                //If VirtualSize is 0, we won't be able to use fast path, and will have to rely on slow path where the proper SectionRange will be used
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

            ref var section = ref SectionHeaders[sectionIndex];
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

        public bool TryGetOffset(int sectionRelativeOffset, int sectionIndex, out int fileOffset)
        {
            var sectionHeaders = SectionHeaders;

            if (sectionIndex < sectionHeaders.Length)
            {
                ref var section = ref SectionHeaders[sectionIndex];

                if (IsLoadedImage)
                {
                    fileOffset = section.VirtualAddress + sectionRelativeOffset;
                    return true;
                }
                else
                {
                    if (sectionRelativeOffset < section.SizeOfRawData)
                    {
                        fileOffset = section.PointerToRawData + sectionRelativeOffset;
                        return true;
                    }
                }
            }

            fileOffset = default;
            return false;
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
            var ranges = _sectionRanges;

            /* Should we be translating RVAs to sections via linear search or binary search?
             *
             * Most modules contain 10 seconds or less, so due to the improved branch prediction you would think that linear search would always win.
             * However, all of our nonsense shuffling ImageSectionHeader records around massively slows us down, so much so that binary searching
             * against section headers actually becomes faster on average (given a random offset anywhere in the file) than performing a linear search.
             *
             * If we rework things so that instead of operating on ImageSectionHeader records we instead have arrays of start and end indices, we end
             * up being 4x faster. Furthermore, it's actually faster having two separate start and end arrays than it is having an array of pairs that
             * hold the start and end addresses.
             *
             * So, should we perhaps start caching the start and end offsets? Further research is required as to whether
             * it's worth the slightly slower startup cost when retrieving the section headers for th first time
             */

            var lo = 0;
            var hi = ranges!.Length - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                ref var sectionRange = ref ranges[mid];

                if (rva < sectionRange.Start)
                    hi = mid - 1;
                else if (rva >= sectionRange.End)
                    lo = mid + 1;
                else
                    return mid;
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

            //See above regarding binary search vs linear
            var lo = 0;
            var hi = headers!.Length - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                ref var sectionHeader = ref headers[mid];

                var start = sectionHeader.PointerToRawData;
                var end = start + sectionHeader.SizeOfRawData;

                if (offset < start)
                    hi = mid - 1;
                else if (offset >= end)
                    lo = mid + 1;
                else
                    return mid;
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
            if (rva < OptionalHeader.SizeOfHeaders)
            {
                chunk = new MemoryChunk(headerBlock, rva);
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

        internal bool TryGetValueChunkFromVA(long va, out MemoryChunk chunk)
        {
            if (va == 0)
            {
                chunk = default;
                return false;
            }

            var rva = (int) (va - OptionalHeader.ImageBase);

            return TryGetValueChunkFromSection(rva, out chunk);
        }

        internal bool TryGetValueChunkFromSection(int relativeOffset, int sectionIndex, out MemoryChunk chunk)
        {
            var sectionHeaders = SectionHeaders;

            if (sectionIndex < sectionHeaders.Length)
            {
                ref var sectionHeader = ref SectionHeaders[sectionIndex];

                if (!IsLoadedImage && relativeOffset >= sectionHeader.SizeOfRawData)
                {
                    chunk = default;
                    return false;
                }

                var block = GetSectionBlock(sectionIndex, sectionHeader);
                chunk = new MemoryChunk(block, relativeOffset);
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

        bool IFileInternal.TryGetValueChunkFromPhysicalOffset(int offset, out MemoryChunk chunk) =>
            TryGetValueChunkFromPhysicalOffset(offset, out chunk);

        //If the caller might pass in 0, it's on them to not do that
        internal bool TryGetValueChunkFromPhysicalOffset(int offset, out MemoryChunk chunk)
        {
            if (offset < OptionalHeader.SizeOfHeaders)
            {
                chunk = new MemoryChunk(headerBlock, offset);
                return true;
            }

            if (TryGetSectionBlockFromOffset(offset, out var block, out var relativeOffset))
            {
                chunk = new MemoryChunk(block!, relativeOffset);
                return true;
            }

            //IsLoadedImage can be false for in-memory modules
            if (headerBlock is LocalHeaderMemoryBlock)
            {
                if (offset > headerBlock.Length)
                {
                    chunk = default;
                    return false;
                }

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

            /* When we have a loaded image, block.Address is the section.VirtualAddress.
             * Otherwise, its the PointerToRawData. But this is an issue, because we need to calculate
             * the reltive offset into the section, which involves looking at the VirtualAddress, so we must
             * do that calculation here above using the section VirtualAddress */
            relativeOffset = rva - section.VirtualAddress;

            //Certain RVAs only exist within the address space of the section as it is when it's laid out in memory.
            //The size of the section on disk may be smaller, in which case it's not going to be possible for us to
            //provide access to the value that this RVA points to
            if (!IsLoadedImage && relativeOffset >= section.SizeOfRawData)
            {
                block = null;
                return false;
            }

            block = GetSectionBlock(sectionIndex, section);
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

        internal MemoryBlock GetSectionBlock(int sectionIndex, in ImageSectionHeader section)
        {
            if (sectionBlocks == null)
                Interlocked.CompareExchange(ref sectionBlocks, new MemoryBlock[ntHeaders.FileHeader.NumberOfSections], null);

            var block = Volatile.Read(ref sectionBlocks[sectionIndex]);

            if (block == null)
            {
                if (IsLoadedImage)
                {
                    //Legacy PE Files with a VirtualSize of 0 are only supported when reading from disk,
                    //so we should never have a scenario where one of these is loaded
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

        public unsafe void GetRawHeaderData(out byte* ptr, out int remainingLength)
        {
            ptr = headerBlock.LocalPointer;
            remainingLength = (int) headerBlock.Length;
        }

        public unsafe bool TryGetRawOverlayData(out byte* ptr, out int remainingLength)
        {
            if (blockProvider is LocalMemoryBlockProvider l)
            {
                var sectionHeaders = SectionHeaders;

                int overlayLength;

                //Get the last section with data
                for (var i = sectionHeaders.Length - 1; i >= 0; i--)
                {
                    ref var lastSection = ref sectionHeaders[i];

                    if (lastSection.PointerToRawData != 0 && lastSection.SizeOfRawData != 0)
                    {
                        var lastSectionEnd = lastSection.PointerToRawData + lastSection.SizeOfRawData;

                        overlayLength = (int) l.Length - lastSectionEnd;

                        if (overlayLength > 0)
                        {
                            ptr = l.Pointer + lastSectionEnd;
                            remainingLength = overlayLength;
                            return true;
                        }

                        ptr = default;
                        remainingLength = default;
                        return false;
                    }
                }

                overlayLength = (int) l.Length - OptionalHeader.SizeOfImage;

                overlayLength = (int) l.Length - OptionalHeader.SizeOfImage;

                if (overlayLength > 0)
                {
                    ptr = l.Pointer + OptionalHeader.SizeOfHeaders;
                    remainingLength = overlayLength;
                    return true;
                }

                ptr = default;
                remainingLength = default;
                return false;
            }
            else
            {
                //It's a RemoteMemoryBlockProvider. If it's backed by a StreamMemoryReader, and we're not loaded, for supported stream types
                //we could potentially have a go at trying to get a length out of it
                ptr = default;
                remainingLength = default;
                return false;
            }
        }

        public unsafe void GetRawSectionDataFromRVA(int rva, out byte* ptr, out int remainingLength)
        {
            if (TryGetSectionBlockFromRVA(rva, out var block, out var relativeOffset))
            {
                ptr = block.LocalPointer + relativeOffset;
                remainingLength = (int) block.Length - relativeOffset;
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
            remainingLength = (int) block.Length - relativeOffset;
        }

        public unsafe void GetRawSectionDataFromOffset(int offset, out byte* ptr, out int remainingLength)
        {
            if (TryGetSectionBlockFromOffset(offset, out var block, out var relativeOffset))
            {
                ptr = block.LocalPointer + relativeOffset;
                remainingLength = (int) block.Length - relativeOffset;
                return;
            }

            throw new BadImageFormatException();
        }

        public unsafe void GetRawSectionDataFromPhysicalOffset(int physicalOffset, int sectionIndex, out byte* ptr, out int remainingLength)
        {
            ref var section = ref SectionHeaders[sectionIndex];

            var relativeOffset = physicalOffset - section.PointerToRawData;

            var block = GetSectionBlock(sectionIndex, section);

            ptr = block.LocalPointer + relativeOffset;
            remainingLength = (int) block.Length - relativeOffset;
        }

        public unsafe void GetRawSectionDataFromRelativeOffset(int relativeOffset, int sectionIndex, out byte* ptr, out int remainingLength)
        {
            ref var section = ref SectionHeaders[sectionIndex];

            Debug.Assert(relativeOffset < section.VirtualSize, "Did you accidentally pass an absolute offset?");

            var block = GetSectionBlock(sectionIndex, section);

            ptr = block.LocalPointer + relativeOffset;
            remainingLength = (int) block.Length - relativeOffset;
        }

        public bool TryGetSectionContainingRVA(int rva, out int index, out ImageSectionHeader header)
        {
            if (rva == 0)
            {
                index = default;
                header = default;
                return false;
            }

            //I don't think we can use lastUsedSection here, as we need to return both the section and the index

            //Store headers locally so that we don't need to keep checking that the headers are loaded each time we touch the headers
            var ranges = SectionRanges;

            for (var i = 0; i < ranges.Length; i++)
            {
                ref var range = ref ranges[i];

                if (range.Start <= rva && rva < range.End)
                {
                    index = i;
                    header = SectionHeaders[i];
                    return true;
                }
            }

            index = default;
            header = default;
            return false;
        }

        public bool TryGetSectionContainingOffset(int offset, out int index, out ImageSectionHeader header)
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

        ICodeViewData IFileWithCodeViewData.CodeViewData => throw new NotImplementedException(); //todo: try and lookup the relevant codeview debug table

        #endregion

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(DosHeader);
            writer.WriteDosStub(DosStub);
            writer.WriteGlobal(RichHeader);
            writer.WriteGlobal(NtHeaders);
            writer.WriteGlobal(SectionHeaders);

            writer.WriteGlobal(ExportTable);

            /* The slowest parts of writing a PEFile are the ExceptionTable and the GuardCFFunctionTable.
             * As such, we do the LoadConfigTable and ExceptionTable last so we can display meaningful progress
             * for all other items in our UI. */

            writer.WriteGlobal(ImportTable);
            writer.WriteUniqueGlobal<ImageThunkDataList, ImageThunkDataList.Enumerator, ImageThunkData>(ImportAddressTable); //The default logic of the merger will be to create an ImportAddressTable region around the ImportAddressTable sub-regions, which will be redundant because all of these will be wrapped in an ImportAddressTableDirectory anyway. As such, we'll block that from happening
            writer.WriteGlobal(ResourceDirectory);
            //ExceptionTable written last because it's slow
            writer.WriteGlobal(SecurityTable);
            writer.WriteGlobal(BaseRelocationTable);
            writer.WriteGlobal(DebugTable);

            var copyright = Copyright;

            if (copyright != null)
            {
                var val = copyright.Value;

                writer.WriteGlobal(val.Offset, copyright.Value, val.Value.Length, ViewKind.Copyright);
            }

            //Global Pointer Table is not a directory and doesn't need writing
            writer.WriteGlobal(TlsDirectory);
            //LoadConfigTable written last because it's slow
            writer.WriteGlobal(BoundImportTable);
            writer.WriteGlobal(DelayImportTable);
            writer.WriteGlobal(Cor20Header);

            writer.WriteGlobal<ImageCorILMethodList, ImageCorILMethodList.Enumerator, ImageCorILMethod>(ILMethods);

            #region Cor20

            writer.WriteGlobal(EcmaMetadata);
            //Cor20Resources
            //Cor20StrongNameSignature
            //Cor20CodeManagerTable
            writer.WriteGlobal(Cor20VTableFixups);
            //Cor20ExportAddressTableJumps

            #endregion
            #region NGEN

            //NgenHeader
            //NgenHelperTable
            //NgenImportSections
            //NgenStubsData
            //NgenVersionInfo
            //NgenDependencies
            //NgenDebugMap
            //NgenModuleImage
            //NgenCodeManagerTable
            //NgenProfileDataList
            //NgenManifestMetaData
            //NgenVirtualSectionsTable
            //NgenEEInfoTable

            #endregion

            writer.WriteGlobal(ExeProjectInfo);
            writer.WriteGlobal(ReadyToRunHeader);

            writer.WriteGlobal(AppHostSignature);
            writer.WriteGlobal(ClrEngineMetrics);
            writer.WriteGlobal(DotNetRuntimeInfo);
            writer.WriteGlobal(DotNetRuntimeDebugHeader);
            writer.WriteGlobal(GetNativeAOTModules(debugger: false, writer._httpPolicy, writer._progress));

            //We write these last because they're the slowest
            writer.WriteGlobal(LoadConfigTable);

            var exceptionTable = ExceptionTable;

            if (exceptionTable != null)
                exceptionTable.WriteFast(writer);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

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
                symbolAccessor?.Dispose();
                _viewAccessorPhysical?.Dispose();
                _viewAccessorVirtual?.Dispose();

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

                _symbolReader?.Dispose();

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
