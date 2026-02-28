using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug;
using PESpy.Ecma335;
using static PESpy.IMAGE_DEBUG_TYPE;
using static System.Diagnostics.DebuggableAttribute;

namespace PESpy
{
    public class PEFileOverview
    {
        #region Properties

        public FileKind FileKind => FileKind.PE;

        /// <summary>
        /// Gets whether this file is 64-bit.<para/>
        /// <see langword="true"/> if <see cref="ImageOptionalHeader.Magic"/> is <see cref="PEMagic.IMAGE_NT_OPTIONAL_HDR64_MAGIC"/>.
        /// </summary>
        public bool Is64Bit { get; set; }

        /// <summary>
        /// Gets the machine type listed in <see cref="ImageFileHeader.Machine"/>.
        /// </summary>
        public IMAGE_FILE_MACHINE Machine { get; set; }

        public IMAGE_SUBSYSTEM Subsystem { get; set; }

        /// <summary>
        /// Gets the EXE or DLL entry point listed in <see cref="ImageOptionalHeader.AddressOfEntryPoint"/>, or
        /// <see langword="null"/> if the listed entry point is 0.
        /// </summary>
        public FileOverview.NativeSymbol? EntryPoint { get; set; }

        /// <summary>
        /// Gets the entry point listed in <see cref="ImageCor20Header.EntryPointTokenOrRVA"/>
        /// when the entry point is managed (<see cref="COMIMAGE_FLAGS.NATIVE_ENTRYPOINT"/> is not listed in <see cref="ImageCor20Header.Flags"/>).
        /// </summary>
        public FileOverview.ManagedSymbol? Cor20ManagedEntryPoint { get; set; }

        /// <summary>
        /// Gets the entry point listed in <see cref="ImageCor20Header.EntryPointTokenOrRVA"/>
        /// when the entry point is native (<see cref="COMIMAGE_FLAGS.NATIVE_ENTRYPOINT"/> is listed in <see cref="ImageCor20Header.Flags"/>).
        /// </summary>
        public FileOverview.NativeSymbol? Cor20NativeEntryPoint { get; set; }

        /// <summary>
        /// Gets the platform type that this managed file was built for.<para/>
        /// If this file is not a managed file, this value is <see langword="null"/>.
        /// </summary>
        public FileOverview.CorPlatform? CorPlatform { get; set; }

        /// <summary>
        /// Gets the image version listed in <see cref="ImageOptionalHeader.MajorImageVersion"/> and <see cref="ImageOptionalHeader.MinorImageVersion"/>.
        /// </summary>
        public Version? ImageVersion { get; set; }

        /// <summary>
        /// Gets the image version listed in <see cref="ImageOptionalHeader.MajorLinkerVersion"/> and <see cref="ImageOptionalHeader.MinorLinkerVersion"/>.
        /// </summary>
        public LinkerVersion LinkerVersion { get; set; }

        //I don't think you could have more than 1 of these (after all, you only get linked once?)

        /// <summary>
        /// Gets the linker version that is listed in the Rich Header.<para/>
        /// If no Rich Header is present, or the Rich Header does not specify the linker, this value is <see langword="null"/>.
        /// </summary>
        public string? RichLinkerVersion { get; set; }

        public PRODID RichLinkerProdID { get; set; }

        /// <summary>
        /// Gets the subsystem version listed in <see cref="ImageOptionalHeader.MajorSubsystemVersion"/> and <see cref="ImageOptionalHeader.MinorSubsystemVersion"/>.
        /// </summary>
        public OSVersion SubsystemVersion { get; set; }

        /// <summary>
        /// Gets the operating system version listed in <see cref="ImageOptionalHeader.MajorOperatingSystemVersion"/> and <see cref="ImageOptionalHeader.MinorOperatingSystemVersion"/>.
        /// </summary>
        public OSVersion OSVersion { get; set; }

        /// <summary>
        /// Gets all compiler backend versions that were listed in the Rich Header.
        /// </summary>
        public string[]? RichCompilerBackend { get; set; }

        /// <summary>
        /// Gets all languages that were listed in the Rich Header.
        /// </summary>
        public string[]? RichLanguage { get; set; }

        /// <summary>
        /// Gets all toolset (e.g. Visual Studio) versions that were listed in the Rich Header.
        /// </summary>
        public string[] RichToolsetVersion { get; set; }

        #region .NET

        /// <summary>
        /// Gets the .NET version listed in the System.Runtime.Versioning.TargetFrameworkAttribute.
        /// </summary>
        public string? TargetFrameworkAttribute { get; set; }

        /// <summary>
        /// Gets the .NET debug information listed in the System.Diagnostics.DebuggableAttribute.
        /// </summary>
        public FileOverview.DebuggableAttributeInfo? DebuggableAttribute { get; set; }

        public bool IsNativeAOT { get; set; }
        public Version? NativeAOTHeaderVersion { get; set; }

        /// <summary>
        /// Gets whether an <see cref="AppHostSignature"/> is present.
        /// </summary>
        public bool IsAppHost { get; set; }

        /// <summary>
        /// Gets whether a <see cref="Bundle.Manifest"/> is present in an <see cref="AppHostSignature"/>, indicating that
        /// this is a .NET Single File App that has bundled its dependencies inside of it.
        /// </summary>
        public bool IsSingleFileApp { get; set; }

        /// <summary>
        /// Gets whether the <see cref="ImageCor20Header.ManagedNativeHeader"/> points to a <see cref="CorCompileHeader"/>.
        /// </summary>
        public bool IsNgen { get; set; }
        public Version? NgenVersion { get; set; }

        /// <summary>
        /// Gets whether the <see cref="ImageCor20Header.ManagedNativeHeader"/> points to a <see cref="ReadyToRunHeader"/>.
        /// </summary>
        public bool IsR2R { get; set; }

        /// <summary>
        /// Gets the version listed in <see cref="ReadyToRunHeader.MajorVersion"/> and <see cref="ReadyToRunHeader.MinorVersion"/>.
        /// </summary>
        public Version? R2RHeaderVersion { get; set; }

        /// <summary>
        /// Gets whether a <see cref="ImageCor20Header"/> is present.
        /// </summary>
        public bool IsManaged { get; set; }

        /// <summary>
        /// Gets the version listed in <see cref="ImageCor20Header.MajorRuntimeVersion"/> and <see cref="ImageCor20Header.MinorRuntimeVersion"/>.
        /// </summary>
        public Version? Cor20HeaderVersion { get; set; }

        #region Debug

        //NGEN images can have more than one
        public SymbolFile[] PDBFile { get; set; }
        public CodeViewSig? CodeViewSig { get; set; }

        public SymbolFile? DBGFile { get; set; }

        //If an external *.pdb or *.dbg file was found, gets the path to that file.
        //This path may be different to what is embedded inside the file and listed in PDBFile/DBGFile
        public string? LocalSymbolFile { get; set; }

        public bool HasEmbeddedPortablePDB { get; set; }

        public bool HasEmbeddedCoffSymbols { get; set; }

        public bool HasEmbeddedCodeViewSymbols { get; set; }

        //Gets either the type of the embedded code view symbols, or the type
        //of code view symbols pointed to by the ImageDebugType.CodeView section

        /// <summary>
        /// Gets whether any <see cref="ImageDebugDirectory"/> entries were found of type <see cref="IMAGE_DEBUG_TYPE_REPRO"/>.
        /// </summary>
        public bool IsReproducible { get; set; }

        #endregion
        #endregion
        #endregion

        public PEFileOverview(PEFile peFile, ISymbolAccessor symbolAccessor)
        {
            //Note: LocalSymbolFile is causing the window to become slightly too large and a scrollbar appears
            if (symbolAccessor is ExternalFileSymbolAccessor e)
                LocalSymbolFile = e.FileName;

            ref readonly var optionalHeader = ref peFile.OptionalHeader;

            //ImageFileHeader / ImageOptionalHeader
            Is64Bit = optionalHeader.Magic == PEMagic.IMAGE_NT_OPTIONAL_HDR64_MAGIC;
            Machine = peFile.FileHeader.Machine;
            Subsystem = optionalHeader.Subsystem;
            LinkerVersion = new LinkerVersion(optionalHeader.MajorLinkerVersion, optionalHeader.MinorLinkerVersion);
            SubsystemVersion = new OSVersion(optionalHeader.MajorSubsystemVersion, optionalHeader.MinorSubsystemVersion);
            OSVersion = new OSVersion(optionalHeader.MajorOperatingSystemVersion, optionalHeader.MinorOperatingSystemVersion);

            //It's just noise creationg a version with 0.0
            if (optionalHeader.MajorImageVersion != 0 || optionalHeader.MinorImageVersion != 0)
                ImageVersion = new Version(optionalHeader.MajorImageVersion, optionalHeader.MinorImageVersion);

            EntryPoint = GetNativeSymbol(optionalHeader.AddressOfEntryPoint, symbolAccessor);

            ProcessCor20Header(peFile, optionalHeader.Magic, symbolAccessor);
            ProcessNativeAOT(peFile);
            ProcessAppHost(peFile);
            ProcessNGEN(peFile);
            ProcessR2R(peFile);
            ProcessDebug(peFile, optionalHeader);
            ProcessRichHeader(peFile);
        }

        #region Cor20Header

        private void ProcessCor20Header(PEFile peFile, PEMagic magic, ISymbolAccessor symbolAccessor)
        {
            var cor20Header = peFile.Cor20Header;

            if (cor20Header == null)
                return;

            IsManaged = true;

            //corflags references this. /UpgradeCLRHeader sets it to 2.5, /RevertCLRHeader sets it to 2.0
            Cor20HeaderVersion = new Version(cor20Header.MajorRuntimeVersion, cor20Header.MinorRuntimeVersion);

            CorPlatform = new FileOverview.CorPlatform(cor20Header.Flags, magic);

            //My interop sample has a managed entry point, whereas my interop-core sample has a native one.
            //Not sure how to detect if we're C++/CLI reliably

            var compressedModelHeap = peFile.EcmaMetadata?.CompressedModelHeap;

            ProcessManagedEntryPoint(cor20Header, compressedModelHeap, symbolAccessor);
            ProcessCustomAttributes(compressedModelHeap);
        }

        private void ProcessManagedEntryPoint(ImageCor20Header cor20Header, CompressedModelHeap compressedModelHeap, ISymbolAccessor symbolAccessor)
        {
            var rva = cor20Header.EntryPointTokenOrRVA;

            if (rva == 0)
                return;

            if ((cor20Header.Flags & COMIMAGE_FLAGS.NATIVE_ENTRYPOINT) != 0)
                Cor20NativeEntryPoint = GetNativeSymbol(rva, symbolAccessor);
            else
            {
                var token = (mdToken) rva;

                if (compressedModelHeap != null)
                {
                    if (token.Type == CorTokenType.mdtMethodDef)
                    {
                        var methodDef = compressedModelHeap.MethodDefTable[token.Rid];

                        Cor20ManagedEntryPoint = new FileOverview.ManagedSymbol(token, methodDef.ToString());
                    }
                    else
                    {
                        Debug.Assert(false); //What is it then?
                        Cor20ManagedEntryPoint = new FileOverview.ManagedSymbol(token, null);
                    }
                }
                else
                    Cor20ManagedEntryPoint = new FileOverview.ManagedSymbol(token, null);
            }
        }

        private void ProcessCustomAttributes(CompressedModelHeap compressedModelHeap)
        {
            if (compressedModelHeap == null)
                return;

            var assemblyTable = compressedModelHeap.AssemblyTable;

            if (assemblyTable != null && assemblyTable.Count > 0)
            {
                var assemblyDef = assemblyTable[1];

                var hasTargetFrameworkAttribute = false;
                var hasDebuggableAttribute = false;

                foreach (var customAttributeRow in assemblyDef.CustomAttributes)
                {
                    if (customAttributeRow.TryGetName(out var namespaceHandle, out var nameHandle))
                    {
                        if (namespaceHandle.Equals("System.Runtime.Versioning") && nameHandle.Equals("TargetFrameworkAttribute"))
                        {
                            var value = customAttributeRow.DecodeValue();

                            if (value.FixedArgs.Length > 0)
                            {
                                var str = value.FixedArgs[0].Value?.ToString();

                                if (str != null)
                                {
                                    //If we have a FrameworkDisplayName, include that

                                    foreach (var namedArg in value.NamedArgs)
                                    {
                                        if (namedArg.Name == "FrameworkDisplayName" && !string.IsNullOrEmpty(namedArg.Value?.ToString()))
                                        {
                                            str = $"{namedArg.Value} ({str})";
                                            break;
                                        }
                                    }
                                }

                                TargetFrameworkAttribute = str;
                            }

                            hasTargetFrameworkAttribute = true;
                        }
                        else if (namespaceHandle.Equals("System.Diagnostics") && nameHandle.Equals("DebuggableAttribute"))
                        {
                            //We don't need to do any fancy decoding, we can just check the bits manually.
                            //https://github.com/dotnet/runtime/blob/7201a39b318e4916f704e3a5c2fa96e3d58bc352/src/coreclr/vm/assembly.cpp#L2381

                            var reader = customAttributeRow.Value.GetReader();

                            /* There are two ctors for DebuggableAttribute
                             * - bool isJITTrackingEnabled, bool isJITOptimizerDisabled
                             * - DebuggingModes modes
                             * 
                             * There are five modes
                             * - None
                             * - Default
                             * - DisableOptimizations
                             * - IgnoreSymbolStoreSequencePoints
                             * - EnableEditAndContinue
                             * 
                             * isJITTrackingEnabled sets Default, and isJITOptimizerDisabled sets
                             * DisableOptimizations
                             * 
                             * Ideally, you want both JIT Tracking Enabled and the JIT Optimizer to be disabled
                             */

                            //This will either be a 6 byte (2x bool) or 8 byte (DebuggingModes) blob
                            //e.g: 01 00 01 01 00 00       | isJITTrackingEnabled = true, isJITOptimizerDisabled = true
                            //e.g. 01 00 02 00 00 00 00 00 | DebuggingModes.IgnoreSymbolStoreSequencePoints
                            if (reader.ReadByte() == 1 && reader.ReadByte() == 0)
                            {
                                if (reader.Length == 6)
                                {
                                    var isJITTrackingEnabled = reader.ReadBoolean();
                                    var isJITOptimizerDisabled = reader.ReadBoolean();

                                    DebuggableAttribute = new FileOverview.DebuggableAttributeInfo(isJITTrackingEnabled, isJITOptimizerDisabled);
                                }
                                else if (reader.Length == 8)
                                {
                                    var debuggingModes = (DebuggingModes) reader.ReadInt32();

                                    DebuggableAttribute = new FileOverview.DebuggableAttributeInfo(debuggingModes);
                                }
                            }

                            hasDebuggableAttribute = true;
                        }

                        if (hasTargetFrameworkAttribute && hasDebuggableAttribute)
                            break;
                    }
                }
            }
        }

        #endregion
        #region NativeAOT

        private void ProcessNativeAOT(PEFile peFile)
        {
            var dotNetRuntimeDebugHeader = peFile.DotNetRuntimeDebugHeader;

            if (dotNetRuntimeDebugHeader != null)
            {
                IsNativeAOT = true;
                NativeAOTHeaderVersion = new Version(dotNetRuntimeDebugHeader.MajorVersion, dotNetRuntimeDebugHeader.MinorVersion);
            }
        }

        #endregion
        #region AppHost / SingleFileApp

        private void ProcessAppHost(PEFile peFile)
        {
            var appHostSignature = peFile.AppHostSignature;

            if (appHostSignature == null)
                return;

            IsAppHost = true;

            if (appHostSignature.BundleHeaderOffset.IsValid)
                IsSingleFileApp = true;
        }

        #endregion
        #region NGEN

        private void ProcessNGEN(PEFile peFile)
        {
            var ngenHeader = peFile.NgenHeader;

            if (ngenHeader != null)
            {
                IsNgen = true;
                NgenVersion = new Version(ngenHeader.MajorVersion, ngenHeader.MinorVersion);
            }
        }

        #endregion
        #region R2R

        private void ProcessR2R(PEFile peFile)
        {
            var r2rHeader = peFile.ReadyToRunHeader;

            if (r2rHeader != null)
            {
                IsR2R = true;
                R2RHeaderVersion = new Version(r2rHeader.MajorVersion, r2rHeader.MinorVersion);
            }
        }

        #endregion
        #region Debug

        private void ProcessDebug(PEFile peFile, in ImageOptionalHeader optionalHeader)
        {
            if (peFile.FileHeader.PointerToSymbolTable.IsValid)
                HasEmbeddedCoffSymbols = true;

            var debugTable = peFile.DebugTable;

            if (debugTable == null)
                return;

            var pdbs = new List<SymbolFile>();

            foreach (var entry in debugTable)
            {
                switch (entry.Type)
                {
                    case IMAGE_DEBUG_TYPE_CODEVIEW:
                        var codeView = (ICodeViewData) entry.Data;

                        CodeViewSig = codeView.Signature;

                        switch (codeView.Signature)
                        {
                            case PESpy.CodeViewSig.NB10:
                                var nb10 = (NB10I) codeView;
                                pdbs.Add(new SymbolFile(nb10.Path.ToString(), SymStoreKey.FromNB10(nb10)));
                                break;

                            case PESpy.CodeViewSig.RSDS:
                                var rsds = (RSDSI) codeView;
                                pdbs.Add(new SymbolFile(rsds.Path.ToString(), SymStoreKey.FromRSDSI(rsds)));
                                break;

                            default:
                                HasEmbeddedCodeViewSymbols = true;
                                break;
                        }

                        break;

                    case IMAGE_DEBUG_TYPE_MISC:
                        //Sometimes the misc points to a *.dbg file, other times it points to an *.exe file.
                        //ImageDebugMiscType.ExeName may be set in both cases regardless

                        var misc = (ImageDebugMisc) entry.Data;
                        var miscPath = misc.Data.ToString();
                        DBGFile = new SymbolFile(miscPath, SymStoreKey.FromMisc(miscPath, peFile.FileHeader.TimeDateStamp, optionalHeader.SizeOfImage));
                        break;

                    case IMAGE_DEBUG_TYPE_EMBEDDED_PORTABLE_PDB:
                        HasEmbeddedPortablePDB = true;
                        break;

                    case IMAGE_DEBUG_TYPE_COFF:
                        HasEmbeddedCoffSymbols = true;
                        break;

                    case IMAGE_DEBUG_TYPE_REPRO:
                        IsReproducible = true;
                        break;
                }
            }

            if (pdbs.Count > 0)
                PDBFile = pdbs.ToArray();
        }

        #endregion
        #region Rich Header

        private void ProcessRichHeader(PEFile peFile)
        {
            var richHeader = peFile.RichHeader;

            if (richHeader == null)
                return;

            HashSet<string> backendNames = new HashSet<string>();
            HashSet<ProductKind> languages = new HashSet<ProductKind>();
            HashSet<string> toolsets = new HashSet<string>();

            foreach (var prodItem in richHeader.Items)
            {
                var productInfoOrDefault = prodItem.ProductInfo;

                if (productInfoOrDefault == null)
                    continue;

                var productInfo = productInfoOrDefault.Value;

                switch (productInfo.ToolKind)
                {
                    case ProductKind.C2:
                        backendNames.Add(productInfo.ToolFullName);
                        break;

                    case ProductKind.LINK:
                        RichLinkerVersion = productInfo.ToolFullName;
                        RichLinkerProdID = prodItem.ProdId;
                        break;
                }

                var toolsetName = productInfo.ToolsetFullName;

                if (toolsetName != null)
                    toolsets.Add(toolsetName);

                var language = productInfo.LanguageKind;

                if (language != ProductKind.None)
                    languages.Add(language);
            }

            RichCompilerBackend = backendNames.ToArray();
            RichLanguage = languages.Select(v => v.ToString()).ToArray();
            RichToolsetVersion = toolsets.ToArray();
        }

        #endregion

        private FileOverview.NativeSymbol? GetNativeSymbol(int rva, ISymbolAccessor symbolAccessor)
        {
            if (rva == 0)
                return null;

            if (symbolAccessor.TryGetNameFromAddress(rva, out var name, out var displacement))
                return new FileOverview.NativeSymbol(rva, name, displacement);
            else
                return new FileOverview.NativeSymbol(rva, default, default);
        }

        //If we didn't have a proper symbol accessor at the point where we were trying to resolve symbols,
        //we may now
        internal void RefreshSymbols(ISymbolAccessor symbolAccessor)
        {
            if (symbolAccessor is ExternalFileSymbolAccessor e)
                LocalSymbolFile = e.FileName;

            if (EntryPoint != null)
            {
                throw new NotImplementedException();
            }

            if (Cor20NativeEntryPoint != null)
            {
                throw new NotImplementedException();
            }
        }

        public struct SymbolFile
        {
            public string Name { get; }

            public SymStoreKey Index { get; }

            internal SymbolFile(string name, SymStoreKey index)
            {
                Name = name;
                Index = index;
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
