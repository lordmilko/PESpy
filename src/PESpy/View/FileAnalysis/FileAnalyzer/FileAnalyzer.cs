using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ClrDebug.DIA;
using ClrDebug.OMF;
using ClrDebug.PDB;
using PESpy.PDB;
using static ClrDebug.PDB.SYM_ENUM_e;

namespace PESpy.View
{
    public struct FileAnalyzerOptions
    {
        public IFileDisassembler Disassembler { get; set; }

        public LocatorHttpPolicy HttpPolicy { get; set; }

        public IFileAnalyzerProgress? Progress { get; set; }

        public bool TrackXRefs { get; set; }

        public CancellationToken CancellationToken { get; set; }

        //By default we want to opt in to symbols, so we want a setting wherein setting it to "true"
        //means we don't want to include symbols. This only excludes symbols that might be identified using
        //an ISymbolAccessor; we may still identify certain symbols through heuristics (e.g. things in the load
        //config table)
        public bool ExcludeSymbols { get; set; }
    }

    internal struct SpecialSymbols
    {
        public SymType NativeAOTModulesA;
        public SymType NativeAOTModulesZ;
    }

    public unsafe abstract class FileAnalyzer
    {
        #region Static

        public static bool TryAnalyze(
            string fileName,
            out FileAccessor fileAccessor,
            IFileDisassembler disassembler = null,
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            IFileAnalyzerProgress? progress = null)
        {
            fileAccessor = null;

            if (!Detector.TryOpenFile(fileName, out var file))
                return false;

            try
            {
                fileAccessor = FileAccessor.Create(file);

                Analyze(fileAccessor, new FileAnalyzerOptions
                {
                    Disassembler = disassembler,
                    HttpPolicy = httpPolicy,
                    TrackXRefs = true
                });
            }
            catch
            {
                fileAccessor?.Dispose();
                fileAccessor = null;

                file.Dispose();
            }

            return true;
        }

        //Takes control of file, will dispose it when the accessor is disposed
        public static void Analyze(
            FileAccessor fileAccessor,
            in FileAnalyzerOptions options = default)
        {
            AnalyzeInternal(fileAccessor, options);

            GCLargeObjectHeap();
        }

        private static void AnalyzeInternal(
            FileAccessor fileAccessor,
            in FileAnalyzerOptions options)
        {
            FileAnalyzer fileAnalyzer = fileAccessor.File.Kind switch
            {
                FileKind.PE          => new PEFileAnalyzer((PEFileAccessor) fileAccessor, options),
                FileKind.NE          => new NEFileAnalyzer((NEFileAccessor) fileAccessor, options),
                FileKind.LE          => new LEFileAnalyzer((LEFileAccessor) fileAccessor, options),
                FileKind.DOS         => new DOSFileAnalyzer((DOSFileAccessor) fileAccessor, options),
                FileKind.DBG         => new DBGFileAnalyzer((DataFileAccessor) fileAccessor, options),
                FileKind.PDB         => CreatePDBFileAnalyzer(fileAccessor, options),
                FileKind.PortablePDB => new PortablePDBFileAnalyzer((PortablePDBFileAccessor) fileAccessor, options),
                FileKind.OBJ         => new OBJFileAnalyzer((DataFileAccessor) fileAccessor, options),
                FileKind.LIB         => new LIBFileAnalyzer((LIBFileAccessor) fileAccessor, options),
                FileKind.OMF         => new OMFFileAnalyzer((DataFileAccessor) fileAccessor, options),
                FileKind.OMFLIB      => new OMFLIBFileAnalyzer((DataFileAccessor) fileAccessor, options),
                FileKind.OMFDBG      => new OMFDBGFileAnalyzer((DataFileAccessor) fileAccessor, options),
                FileKind.SYM         => new SYMFileAnalyzer((DataFileAccessor) fileAccessor, options),
                _ => throw new NotImplementedException($"Don't know how to analyze a file of type '{fileAccessor.File.Kind}'")
            };

            static FileAnalyzer CreatePDBFileAnalyzer(FileAccessor fileAccessor, in FileAnalyzerOptions options)
            {
                switch (((PDBFile) fileAccessor.File).PDBKind)
                {
                    case PDBFileKind.V1:
                        return new PDB1FileAnalyzer((DataFileAccessor) fileAccessor, options);

                    case PDBFileKind.V2:
                    case PDBFileKind.V7:
                        return new PDBFileAnalyzer((PDBFileAccessor) fileAccessor, options);

                    default:
                        Debug.Assert(false);
                        return null;
                }
            }

            fileAnalyzer.Execute();
        }

        internal static void GCLargeObjectHeap()
        {
            /* Cleanup the objects on the LOH that were allocated during analysis.
             * It's also important that we compact the LOH as well. It seems that each LOH
             * that exists causes you to pay the full price of having that heap; when I was
             * shrinking the size of ViewInfo down to 24 bytes, I found that packing it by 4
             * was OK, but upon also packing FixedUtf8String by 4 we actually used more memory
             * than baseline, because a 4th heap was being allocated containing the info map keys
             * at the end of it. I don't think it being at the end was the issue, I think the issue was
             * that 4th heap was 500mb large!
             * 
             * There's a big gotcha when it comes to reclaiming the LOH in .NET 7+; .NET 7 made a major redesign
             * of the GC, wherein it's now based on regions instead of segments. For troubleshooting issues potentially
             * relating to the new .NET 7 GC, you can set an environment variable DOTNET_GCName=clrgc.dll to use the
             * old .NET 6.0 GC (which is bundled with .NET Core). However, in order to fix this properly, in .NET 7+
             * we need to perform an _aggressive_ GC, which will attempt to decommit as much memory as possible. Furthermore,
             * it seems that the memory is only really decommitted on the second GC; I have read several times about certain
             * GC-related things requiring two GC's to go through, so this is in line with that
             * 
             * An additional gotcha to be aware of here is that if the application that is hosting PESpy is using a version
             * higher than .NET Standard 2.0 but lower than the highest version we support, the #if will not execute the aggressive
             * GC's, and it will be the responsibility of the caller to do the GC properly for us
             */
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

#if NET7_0_OR_GREATER
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
#else
            GC.Collect();
#endif
        }

        #endregion

        protected readonly FileAccessor _fileAccessor;
        protected readonly IFileDisassembler? _fileDisassembler;

        protected readonly LocatorHttpPolicy _httpPolicy;
        protected readonly IFileAnalyzerProgress? _progress;
        private readonly HashSet<int> _queuedAddresses = new HashSet<int>();
        internal readonly List<RegionBuilder> _extraRegions = new List<RegionBuilder>();
        protected readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        protected long _lastStopwatchCheckpoint;
        private FileAnalyzerProgressPhase _lastPhase;
        protected bool _hasUnknownBodies;

        private SpecialSymbols _specialSymbols;

        //This is super way faster than trying to do everything directly within SpanAllocator
        private bool _trackXRefs;
        private List<XRef> _xrefs;

        protected long GetPhaseTime()
        {
            var newElapsed = _stopwatch.ElapsedMilliseconds;

            var diff = newElapsed - _lastStopwatchCheckpoint;

            _lastStopwatchCheckpoint = newElapsed;

            return diff;
        }

        protected void Log(FileAnalyzerProgressPhase phase)
        {
            if (_progress != null)
            {
                if (_lastPhase != 0)
                    _progress.PhaseComplete(_lastPhase, GetPhaseTime());

                _lastPhase = phase;
                _progress.NotifyPhase(phase);
            }
        }

        private long[] _stringAddresses;

        //The limiting factor on performance now seems to be lock contention. I tried to do away with the lock and use
        //concurrent queue instead (forgetting about checking whether an address has already been processed or not) but that
        //ended up being slower. Checking the Usage before adding to candidate additions also didn't help
        private readonly Queue<WorkItem> _globalWorkQueue = new Queue<WorkItem>();
        private readonly object _globalWorkQueueLock = new object();
        protected readonly ViewWriter _viewWriter;

        protected readonly CancellationToken _cancellationToken;

        protected FileAnalyzer(
            FileAccessor fileAccessor,
            in FileAnalyzerOptions options)
        {
            _fileAccessor = fileAccessor;
            _fileDisassembler = options.Disassembler;
            _httpPolicy = options.HttpPolicy;
            _progress = options.Progress;
            _viewWriter = CreateViewWriter();
            _trackXRefs = options.TrackXRefs;
            _cancellationToken = options.CancellationToken;

            if (options.TrackXRefs)
                _xrefs = new List<XRef>();
        }

        protected abstract ViewWriter CreateViewWriter();

        public abstract void Execute();

        protected void ExecuteCode()
        {
            DiscoverGlobals();

            //We may or may not have symbols. Collect any code locations pointed to by the PEFile
            //so we can at least disassemble something
            DiscoverCodeRoots();

            //Now try and discover symbols. Symbols can come in many forms: we can have a PDB (old, MSF or portable),
            //OMF CodeView symbols, or even COFF symbols in a CoffSymbolTable. We'll use any symbols we discover to expand
            //upon the code addresses we found in our roots, to ensure we disassemble as much as possible in the PEFile
            DiscoverSymbols((ISectionDataAccessor) _fileAccessor);

            //We've done all the preparations we can; work the disasm queue, discovering xrefs and tagging bytes as being code
            var importMap = _fileDisassembler == null ? null : GetImportMap();
            WorkDisasmQueue(importMap);

            Finalize(expandUnknownData: true);
        }

        protected void ExecuteData()
        {
            Log(FileAnalyzerProgressPhase.DiscoverGlobals);

            _cancellationToken.ThrowIfCancellationRequested();

            ((IViewable) _fileAccessor.File).WriteGlobals(_viewWriter);

            //In data files, there shouldn't be random entities (e.g. publics, code) we discovered
            //whose length is unknown; we should already know how big everything is that we've discovered
            Finalize(expandUnknownData: false);
        }

        protected virtual void DiscoverGlobals()
        {
            Log(FileAnalyzerProgressPhase.DiscoverCodeRoots);

            _cancellationToken.ThrowIfCancellationRequested();

            ((IViewable) _fileAccessor.File).WriteGlobals(_viewWriter);
            var symbolAccessor = LocateSymbols();
        }

        protected virtual void DiscoverCodeRoots() => throw new NotSupportedException("If this method is called, it means a derived type has failed to override it");

        protected virtual Dictionary<long, int> GetImportMap() => null;

        //AddCode can't be on the FileAccessor because it needs to interact with members specific to performing analysis

        //The value we're being passed is definitely an RVA. If the PE File is not loaded, we need to convert the RVA to a physical offset
        protected void AddCode(int address, int rva)
        {
            if (!_queuedAddresses.Add(address))
                return;

            //Don't mark it as code yet; we'll mark it as code if we successfully disassemble it. Our code reader
            //also asserts that anything it tries to disassemble into has status Unknown
            EnqueueWork(address, address, rva);
        }

        protected ViewByte* AddCode(int address, int rva, int sectionIndex)
        {
            AddCode(address, rva);

            return _fileAccessor.GetViewByteForSection(address, sectionIndex);
        }

        private void EnqueueWork(int owner, int address, int rva)
        {
#if DEBUG
            Debug.Assert(owner != 0);
            Debug.Assert(address != 0);

            //Object files have not been laid out yet, so the concept of an RVA is meaningless
            //if (this is not OBJFileAnalyzer)
            //    Debug.Assert(rva != 0);
#endif

            _globalWorkQueue.Enqueue(new WorkItem(owner, address, rva));
        }

        #region DiscoverSymbols

        protected ISymbolAccessor LocateSymbols()
        {
            Log(FileAnalyzerProgressPhase.LocateSymbols);

            _cancellationToken.ThrowIfCancellationRequested();

            return _fileAccessor.GetSymbolAccessor(load: true, _httpPolicy, _progress, _cancellationToken);
        }

        protected void DiscoverSymbols(ISectionDataAccessor sectionDataAccessor)
        {
            var symbolAccessor = _fileAccessor.GetSymbolAccessor();

            Log(FileAnalyzerProgressPhase.ProcessSymbols);

            if (symbolAccessor is ExternalFileSymbolAccessor e)
                symbolAccessor = e.GetUnderlyingSymbolAccessorUnsafe();

            switch (symbolAccessor.Kind)
            {
                case SymbolAccessorKind.PDB:
                    ProcessPDBSymbols(((PDBFileSymbolAccessor) symbolAccessor).PDBFile, sectionDataAccessor);
                    break;

                case SymbolAccessorKind.Coff:
                    ProcessCoffSymbols(((CoffSymbolAccessor) symbolAccessor).Externals, sectionDataAccessor);
                    break;

                case SymbolAccessorKind.DNRB:
                    ProcessDNRBSymbols((DNRBData) symbolAccessor);
                    break;

                case SymbolAccessorKind.NB02:
                    ProcessNB02Symbols((NB02Data) symbolAccessor);
                    break;

                case SymbolAccessorKind.NB05:
                    //Note that NB09 derives from NB05
                    ProcessNB05Symbols(((NB05SymbolAccessor) symbolAccessor).data, sectionDataAccessor);
                    break;

                case SymbolAccessorKind.SYM:
                    ProcessSYMSymbols(((SYMFileSymbolAccessor) symbolAccessor).SYMFile, sectionDataAccessor);
                    break;

                case SymbolAccessorKind.PortablePDB:
                case SymbolAccessorKind.Null:
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle a symbol accessor of type '{symbolAccessor.Kind}'");
            }
        }

        internal void ProcessPDBSymbols(PDBFile pdbFile, ISectionDataAccessor sectionDataAccessor)
        {
            /* Add top level symbols from Globals and Publics. Note that we can't just iterate snSymRecs directly,
             * because this can contain junk not actually referenced from any location! e.g. this can occur if during
             * compilation some symbols were written and then later re-written somewhere else and the old data was just
             * left inactive */

            //I think Mod1::fAddSymRefToGSI shows all of the possible things you can get in globals

            var globals = pdbFile.GSI?.Symbols;

            //Another complicating factor we have is that symbols for managed assemblies often seem to contain complete garbage
            //that points halfway into the ImageCorILMethod. We fix this by firstly ignoring any symbols that are a tokenref,
            //and secondly by ignoring any publics that are fMSIL
            if (globals != null)
                ProcessGlobalSymbols(globals, pdbFile, sectionDataAccessor);

            var publics = pdbFile.PSGSI?.Symbols;

            if (publics != null)
                ProcessGlobalSymbols(publics, pdbFile, sectionDataAccessor);

            //Not sure if you could have thunks if you didn't have globals
            var thunks = pdbFile.PSGSI?.ThunkEntries;

            if (thunks != null)
            {
                foreach (var thunkEntry in thunks)
                {
                    switch (thunkEntry.Thunk.rectyp)
                    {
                        case S_PUB32:
                        case S_PUB32_ST:
                            ProcessPubSym32(thunkEntry.Thunk, pdbFile, sectionDataAccessor);
                            break;

                        case S_PUB32_16t:
                        case S_PUB16:
                            ProcessPubSym16(true, thunkEntry.Thunk, pdbFile, sectionDataAccessor);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
            }

            if (_fileAccessor.File.Kind == FileKind.PE)
            {
                SymbolReader.Builder.ProcessNativeAOTModules((PEFile) _fileAccessor.File, _specialSymbols.NativeAOTModulesA, _specialSymbols.NativeAOTModulesZ, out var nativeAOTModules);

                if (nativeAOTModules != null)
                {
                    _viewWriter.WriteGlobal(nativeAOTModules);
                }
            }
        }

        enum SymSegmentKind
        {
            Other,
            Code,
            Data,
            Imports
        }

        internal void ProcessCoffSymbols((ImageSymbol symbol, int length)[] externals, ISectionDataAccessor sectionDataAccessor)
        {
            for (var i = 0; i < externals.Length; i++)
            {
                ref var external = ref externals[i];

                switch (external.symbol.DerivedType)
                {
                    case IMAGE_SYM_DTYPE.IMAGE_SYM_DTYPE_FUNCTION:
                        ProcessFunctionSymbol((int) external.symbol.Value, (FixedUtf8String) external.symbol.Name.Name, sectionDataAccessor);
                        break;

                    case IMAGE_SYM_DTYPE.IMAGE_SYM_DTYPE_NULL:
                        //This can be an import
                        if (external.symbol.Name.Name.StartsWith("__imp__"))
                            continue;

                        if (external.symbol.Name.Name.StartsWith("\u007f") && external.symbol.Name.Name.EndsWith("_NULL_THUNK_DATA"))
                            continue;

                        ProcessDataSymbol((int) external.symbol.Value, (FixedUtf8String) external.symbol.Name.Name, sectionDataAccessor);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        //CodeView (DNRB)
        internal void ProcessDNRBSymbols(DNRBData data)
        {
            //Don't currently know what to do with these symbols
            foreach (var symbol in data.Symbols.Value)
            {
                var str = symbol.ToString();
            }
        }

        //CodeView (NB02)
        internal void ProcessNB02Symbols(NB02Data data)
        {
            for (var i = 0; i < data.DirEntries.Length; i++)
            {
                ref var entry = ref data.DirEntries[i];

                switch (entry.SubSection)
                {
                    case SST.SSTPUBLIC:
                        var publics = (RawValue<pbi[]>) entry.Data;

                        //Not sure what to do with these
                        break;

                    case SST.SSTSYMBOLS:
                        var symbols = (RawValue<OldSymType[]>) entry.Data;

                        //Not sure what to do with these
                        break;
                }
            }
        }

        //CodeView (NB05)
        internal void ProcessNB05Symbols(NB05Data data, ISectionDataAccessor sectionDataAccessor)
        {
            for (var i = 0; i < data.DirEntries.Length; i++)
            {
                ref var entry = ref data.DirEntries[i];

                //DbgHelp will use the sections listed in sstSegMap if there's an OMAP FROM debug directory entry.
                //Otherwise, it uses the section headers listed in the PE File
                switch (entry.SubSection)
                {
                    case SST.sstGlobalPub:
                    case SST.sstGlobalSym:
                    case SST.sstStaticSym: //I don't think there'll be anything in these, but we'll process it anyway
                        ProcessSymTypeList(((OMFHashedSymbols) data.DirEntries[i].Data).Symbols, data.GetCodeViewAccessor(), sectionDataAccessor);
                        break;

                    case SST.sstSymbols:
                    case SST.sstPublicSym:
                    case SST.sstAlignSym:
                        var symbols = ((OMFModuleSymbols) data.DirEntries[i].Data!).List;
                        ProcessSymTypeList(symbols, data.GetCodeViewAccessor(), sectionDataAccessor);
                        break;

                    default:
                        break;
                }
            }
        }

        private void ProcessSymTypeList(
            SymTypeList symTypeList,
            ICodeViewAccessor codeViewAccessor,
            ISectionDataAccessor sectionDataAccessor)
        {
            foreach (var symType in symTypeList)
                ProcessSymType(symType, codeViewAccessor, sectionDataAccessor);
        }

        internal void ProcessSYMSymbols(SYMFile symFile, ISectionDataAccessor sectionDataAccessor)
        {
            var segments = symFile.Segments;

            for (var i = 0; i < segments.Length; i++)
            {
                ref var segment = ref segments[i];

                SymSegmentKind segmentKind;

                //Note: it's possible to have random data symbols in the code segment.
                //e.g. SetLastError is preceded by _fSetLastError

                if (segment.gd_achname == "_TEXT")
                    segmentKind = SymSegmentKind.Code;
                else if (segment.gd_achname == "DGROUP")
                    segmentKind = SymSegmentKind.Data;
                else if (segment.gd_achname == "IMPORT_THUNKS")
                    segmentKind = SymSegmentKind.Imports;
                else
                {
                    Debug.Assert(false);
                    segmentKind = default;
                }

                //It's not really a "load segment address" but I'm guessing this is the segment index
                var seg = segment.gd_lsa;

                var symbols = segment.Symbols;

                if (symbols.Symbols32 != null)
                {
                    var symbols32 = symbols.Symbols32;

                    for (var j = 0; j < symbols.Symbols32.Length; j++)
                    {
                        ref var symbol = ref symbols32[j];

                        var off = symbol.sd_lval;

                        ProcessSYMSymbol(seg, off, segmentKind, symbol.sd_achname, sectionDataAccessor);
                    }
                }
                else
                {
                    var symbols16 = symbols.Symbols16;

                    for (var j = 0; j < symbols.Symbols16.Length; j++)
                    {
                        ref var symbol = ref symbols16[j];

                        var off = symbol.sd16_val;

                        ProcessSYMSymbol(seg, off, segmentKind, symbol.sd16_achname, sectionDataAccessor);
                    }
                }
            }
        }

        private void ProcessSYMSymbol(ushort seg, int off, SymSegmentKind segmentKind, FixedAnsiString name, ISectionDataAccessor sectionDataAccessor)
        {
            var sectionHeaders = ((PEFileAccessor) _fileAccessor).PEFile.SectionHeaders;

            //The section number we're given is 1 based
            if (seg > sectionHeaders.Length)
                return;

            ref var sectionHeader = ref sectionHeaders[seg - 1];

            var rva = sectionHeader.VirtualAddress + off;

            switch (segmentKind)
            {
                case SymSegmentKind.Code:
                    ProcessFunctionSymbol(rva, (FixedUtf8String) name, sectionDataAccessor);
                    break;

                case SymSegmentKind.Data:
                    ProcessDataSymbol(rva, (FixedUtf8String) name, sectionDataAccessor);
                    break;
            }
        }

        internal void ProcessGlobalSymbols(
            GlobalSymTypeList symTypeList,
            ICodeViewAccessor codeViewAccessor,
            ISectionDataAccessor sectionDataAccessor)
        {
            foreach (var symType in symTypeList)
                ProcessSymType(symType, codeViewAccessor, sectionDataAccessor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessSymType(
            SymType symType,
            ICodeViewAccessor codeViewAccessor,
            ISectionDataAccessor sectionDataAccessor)
        {
            //We support all "addressable" symbols, meaning those that are capable of having an RVA
            switch (symType.rectyp)
            {
                //RefSym
                case S_PROCREF_ST:
                case S_DATAREF_ST:
                case S_LPROCREF_ST:
                    symType = ((RefSym) symType).GetSymbol(codeViewAccessor);
                    goto default;

                //RefSym2
                case S_PROCREF:
                case S_DATAREF:
                case S_LPROCREF:
                case S_ANNOTATIONREF:
                    symType = ((RefSym2) symType).GetSymbol(codeViewAccessor);

                    switch (symType.rectyp)
                    {
                        case S_GMANPROC:
                        case S_GMANPROC_ST:
                        case S_LMANPROC:
                        case S_LMANPROC_ST:
                            //S_GMANPROC symbols cannot be trusted; their off/seg values may point in the middle of UNWIND_INFO items
                            return;
                    }

                    goto default;

                case S_TOKENREF:
                    //TokenRef symbols cannot be trusted; they might resolve to a S_GMANPROC that has a bogus off/seg that may point in the middle of an UNWIND_INFO item
                    return;

                case S_PUB32:
                case S_PUB32_ST:
                    if (((PubSym32) symType).pubsymflags.fMSIL)
                        return; //This symbol might point to the ImageCorILMethod. We don't want to convert this to code, as this will mean clearing out the leading Data byte to make it Unknown for the purposes of tracking disassembly

                    ProcessPubSym32(symType, codeViewAccessor, sectionDataAccessor);
                    break;

                case S_PUB16:
                case S_PUB32_16t:
                    ProcessPubSym16(false, symType, codeViewAccessor, sectionDataAccessor);
                    break;

                default:
                    //Some symbols point to sections that don't exist, so their RVAs are 0.
                    //Note that as of writing we have an issue in that NE/LE/DOS files don't have section headers
                    //and we don't know how to get the RVAs for them
                    if (!symType.TryGetRVA(codeViewAccessor, out var rva) || rva == 0)
                        return;

                    var symTagEnum = symType.GetSymTagEnum();

                    ProcessSymbol(symTagEnum, symType, rva, codeViewAccessor, sectionDataAccessor);
                    break;
            }
        }

        private void ProcessSymbol(
            SymTagEnum symTagEnum,
            SymType symType,
            int rva,
            ICodeViewAccessor codeViewAccessor,
            ISectionDataAccessor sectionDataAccessor)
        {
            SymString name;

            switch (symTagEnum)
            {
                case SymTagEnum.Function:
                case SymTagEnum.Thunk:
                    symType.TryGetName(out name);

                    ProcessFunctionSymbol(rva, name, sectionDataAccessor);
                    break;

                case SymTagEnum.Block:
                case SymTagEnum.Label:
                case SymTagEnum.FuncDebugStart:
                case SymTagEnum.FuncDebugEnd:
                    symType.TryGetName(out name);

                    ProcessCodeSymbol(symType, rva, name, codeViewAccessor, sectionDataAccessor);
                    break;

                case SymTagEnum.Data:
                case SymTagEnum.VTable:
                    //S_LDATA32 can have no name
                    if (symType.TryGetName(out name) && name.Length > 0)
                    {
                        //Sometimes globals assigns data symbols to areas that publics says are functions.
                        //ProcessDataSymbol will correctly handle this
                        ProcessDataSymbol(rva, name, sectionDataAccessor);
                    }
                    break;

                //case SymTagEnum.Annotation:
                //    throw new NotImplementedException();

                //We should not be getting public symbols; the caller should be special casing those
            }
        }

        private void ProcessFunctionSymbol(
            int rva,
            FixedUtf8String name,
            ISectionDataAccessor sectionDataAccessor)
        {
            if (sectionDataAccessor.TryGetTargetAddress(rva, out var targetAddress, out var sectionIndex))
            {
                //In devenv.exe there's a symbol S_LPROC32 "`CVsActivityLogFile::GetLogFilePath'::`1'::dtor$5"
                //that doesn't actually exist, and it points halfway through an IMAGE_THUNK_DATA symbol.
                //As such, we defer trying to mark this as code until we know it's not bogus

                var pViewByte = _fileAccessor.GetViewByteForSection(targetAddress, sectionIndex);

                if (pViewByte->Kind == ViewByteKind.Body)
                    return; //Bogus

                AddCode(targetAddress, rva);

                //We defer trying to get the name until we know this is actually a viable result
                if (name.Length > 0)
                {
                    _fileAccessor.CheckName(targetAddress);
                    pViewByte->HasName = true;
                    pViewByte->IsFunction = true;
                }
            }
        }

        private void ProcessCodeSymbol(
            SymType symType,
            int rva,
            FixedUtf8String name,
            ICodeViewAccessor codeViewAccessor,
            ISectionDataAccessor sectionDataAccessor)
        {
            if (sectionDataAccessor.TryGetTargetAddress(rva, out var targetAddress, out var sectionIndex))
            {
                var pViewByte = _fileAccessor.GetViewByteForSection(targetAddress, sectionIndex);

                if (pViewByte->Kind == ViewByteKind.Data)
                    return; //Already known to be data; don't mess it up by treating it like code

                AddCode(targetAddress, rva);

                if (name.Length > 0)
                {
                    _fileAccessor.CheckName(targetAddress);
                    pViewByte->HasName = true;
                }
            }
        }

        private void ProcessDataSymbol(
            int rva,
            FixedUtf8String name,
            ISectionDataAccessor sectionDataAccessor)
        {
            //Things without names are noise. The caller should have already checked that.
            //We only allow things without names when it comes to code, because we want to make
            //sure we cover all code paths. You could potentially make the point however
            //that it's useful saying that the bytes at a given address are not unknown?
            Debug.Assert(name.Length != 0);

            if (sectionDataAccessor.TryGetTargetAddress(rva, out var targetAddress, out var sectionIndex))
            {
                if (_fileAccessor.TryAddData(targetAddress, sectionIndex, ViewByteDataKind.Unknown, 1, out var pViewByte) && name.Length > 0)
                {
                    _fileAccessor.CheckName(targetAddress);
                    pViewByte->HasName = true;
                }
            }
        }

        private void ProcessPubSym32(
            SymType symType,
            ICodeViewAccessor codeViewAccessor,
            ISectionDataAccessor sectionDataAccessor)
        {
            if (!TryGetPubSymInfo(symType, codeViewAccessor, sectionDataAccessor, out var rva, out var name))
                return;

            var pubSym32 = (PubSym32) symType;

            //Note that if our heuristic is wrong and in fact the public points to data already (e.g. an import or delay import)
            //we'll catch this and abort in ProcessCodeSymbol

            if (pubSym32.pubsymflags.fFunction)
            {
                ProcessFunctionSymbol(rva, name, sectionDataAccessor);
            }
            else if (pubSym32.pubsymflags.fCode || SymType.TryGetSectionCharacteristics(symType, pubSym32.seg, pubSym32.off, codeViewAccessor, out var characteristics) && (characteristics & ClrDebug.IMAGE_SCN.CNT_CODE) != 0)
            {
                ProcessCodeSymbol(symType, rva, name, codeViewAccessor, sectionDataAccessor);
            }
            else
            {
                ProcessDataSymbol(rva, name, sectionDataAccessor);
            }
        }

        private void ProcessPubSym16(
            bool isKnownCode,
            SymType symType,
            ICodeViewAccessor codeViewAccessor,
            ISectionDataAccessor sectionDataAccessor)
        {
            if (!TryGetPubSymInfo(symType, codeViewAccessor, sectionDataAccessor, out var rva, out var name))
                return;

            Debug.Assert(symType.rectyp == S_PUB16 || symType.rectyp == S_PUB32_16t);

            int off;
            ISECT seg;

            if (symType.rectyp == S_PUB16)
            {
                var dataSym16 = (DataSym16) symType;
                off = dataSym16.off;
                seg= dataSym16.seg;
            }
            else
            {
                var dataSym3216t = (DataSym3216t) symType;
                off = dataSym3216t.off;
                seg = dataSym3216t.seg;
            }

            //It seems like not all addresses are in section contribs. .data doesn't seem to be, nor are thunks;
            //but that's OK, we now prefer to get characteristic information from the associated IMAGE_SECTION_HEADER
            //where available
            if (isKnownCode || SymType.TryGetSectionCharacteristics(symType, seg, off, codeViewAccessor, out var characteristics) && (characteristics & ClrDebug.IMAGE_SCN.CNT_CODE) != 0)
            {
                ProcessCodeSymbol(symType, rva, name, codeViewAccessor, sectionDataAccessor);
            }
            else
            {
                ProcessDataSymbol(rva, name, sectionDataAccessor);
            }
        }

        private unsafe bool TryGetPubSymInfo(
            SymType symType,
            ICodeViewAccessor codeViewAccessor,
            ISectionDataAccessor sectionDataAccessor,
            out int rva,
            out FixedUtf8String name)
        {
            Unsafe.SkipInit(out rva);
            Unsafe.SkipInit(out name);

            if (!symType.TryGetRawOffSeg(out var off, out var seg))
                return false;

            var rawRva = SymType.GetOmapRelativeVirtualAddress((SYMTYPE*) symType, seg, off, codeViewAccessor);

            if (rawRva == null)
                return false;

            rva = rawRva.Value;

            if (rva == 0)
                return false;

            if (!_fileAccessor.TryGetTargetAddress(rva, out var targetAddress, out var sectionIndex))
                return false;

            name = (FixedUtf8String) symType.GetName(codeViewAccessor);

            //Things without names might still be code, so we can't just bail out, as we want
            //to collect all code addresses

            if (TryHandleSpecialPublic(symType, name, codeViewAccessor, targetAddress, sectionIndex, off, seg))
                return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryHandleSpecialPublic(
            SymType symType,
            FixedUtf8String name,
            ICodeViewAccessor codeViewAccessor,
            int targetAddress,
            int sectionIndex,
            int off,
            ISECT seg)
        {
            //Keep in sync with SymbolReader

            //We're going to be doing multiple name checks, so get the length once
            var span = name.AsSpan();

            if (span.StartsWith("??_C@_"u8))
                return ProcessStringSymbol(symType, name, targetAddress, sectionIndex);
            else if (span.StartsWith("??_7"u8))
                return ProcessVftableSymbol(symType, codeViewAccessor, targetAddress, sectionIndex);
            else if (span.StartsWith("??_R4"u8)) //While each RTTI entity may have a symbol associated with it, we're only interested in matching the top level object locator type, which should point to all the rest
                return ProcessRTTISymbol(off, seg);
            else if (span.StartsWith("__real@"u8))
                return ProcessFloatSymbol(name, span, targetAddress, sectionIndex);
            else if (span.StartsWith("__modules_"u8) && span.Length == 11)
            {
                switch (span[10])
                {
                    case (byte) 'a':
                        _specialSymbols.NativeAOTModulesA = symType;
                        break;

                    case (byte) 'z':
                        _specialSymbols.NativeAOTModulesZ = symType;
                        break;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ProcessStringSymbol(
            SymType symType,
            FixedUtf8String name,
            int targetAddress,
            int sectionIndex)
        {
            //It's a string literal. If a 1 follows it's wide, if a 0 follows it's ANSI.
            //Then, following this is a length

            var textWindow = new Demangler.TextWindow(name.Value, name.Length, true);
            textWindow.AdvanceChar(6);

            if (textWindow.TryNextChar(out var stringKind) && Demangler.TryParseNumber(ref textWindow, out _, out var strLength))
            {
                //The length should be the true length of the string. The symbol name only includes up to the first 64 characters
                _fileAccessor.AddString(targetAddress, sectionIndex, isWide: stringKind == '1', numBytes: (int) strLength);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ProcessVftableSymbol(
            SymType symType,
            ICodeViewAccessor codeViewAccessor,
            int targetAddress,
            int sectionIndex)
        {
            if (!symType.TryGetLength(out var length, codeViewAccessor))
                return false;

            var peFileAccessor = (PEFileAccessor) _fileAccessor;

            var lookupCache = peFileAccessor._lookupCache;

            lookupCache.GetRawSectionDataFromTargetAddress(targetAddress, sectionIndex, out var pVftable, out var remainingLength);

            var imageBase = peFileAccessor.PEFile.OptionalHeader.ImageBase;

            var viewWriter = _viewWriter;

            //You can have multiple vftable symbols that target a specific address
            if (!_fileAccessor.AddStruct(this, targetAddress, sectionIndex, ViewKind.Vftable, length))
                return true;

            switch (_fileAccessor.Bitness)
            {
                case 32:
                    var slots32 = new NativeSpan<int>(pVftable, length / sizeof(int));

                    for (var i = 0; i < slots32.Length; i++)
                    {
                        var va = slots32[i];

                        if (va == 0)
                            continue;

                        var rva = (int) (va - imageBase);

                        if (lookupCache.TryGetSectionInfo(rva, out var functionTargetAddress, out _, out _))
                            AddXRef(targetAddress + (i * sizeof(int)), functionTargetAddress);
                    }

                    break;

                case 64:
                    var slots64 = new NativeSpan<long>(pVftable, length / sizeof(long));

                    for (var i = 0; i < slots64.Length; i++)
                    {
                        var va = slots64[i];

                        if (va == 0)
                            continue;

                        var rva = (int) (va - imageBase);

                        if (lookupCache.TryGetSectionInfo(rva, out var functionTargetAddress, out _, out _))
                            AddXRef(targetAddress + (i * sizeof(long)), functionTargetAddress);
                    }

                    break;

                default:
                    throw new NotImplementedException();
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ProcessRTTISymbol(int off, ISECT seg)
        {
            //We should implement support for detecting RTTI + vftables without symbols
            //https://github.com/kweatherman/IDA_ClassInformer_PlugIn/blob/master/RTTI.cpp
            //https://github.com/kweatherman/IDA_ClassInformer_PlugIn/blob/master/Main.cpp#L1328

            if (!((PEFileAccessor) _fileAccessor).PEFile.TryGetValueChunkFromSection(off, seg - 1, out var chunk))
                return false;

            var rttiCompleteObjectLocator = new RTTICompleteObjectLocator(chunk);
            _viewWriter.WriteGlobal(rttiCompleteObjectLocator);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ProcessFloatSymbol(FixedUtf8String name, Span<byte> span, int targetAddress, int sectionIndex)
        {
            ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionIndex + 1];
            var pViewByte = sectionAccessor.pViewBytes + (targetAddress - sectionAccessor.StartAddress);

            long l;
            ViewByte* pEnd;

            switch (span.Length)
            {
                case 8 + 7: //__real@ + 8 chars
                    if (!Utf8Parser.TryParse(span.Slice(7), out l, out _, 'X'))
                        return false;

                    var f = *(float*) &l;

                    pViewByte->Kind = ViewByteKind.Data;
                    pViewByte->DataKind = ViewByteDataKind.Decimal;

                    pEnd = pViewByte + 4;

                    for (var i = pViewByte + 1; i < pEnd; i++)
                        i->Kind = ViewByteKind.Body;

                    break;

                case 16 + 7: //__real@ + 16 chars
                    if (!Utf8Parser.TryParse(span.Slice(7), out l, out _, 'X'))
                        return false;

                    var d = *(double*) &l;

                    pViewByte->Kind = ViewByteKind.Data;
                    pViewByte->DataKind = ViewByteDataKind.Decimal;

                    pEnd = pViewByte + 8;

                    for (var i = pViewByte + 1; i < pEnd; i++)
                        i->Kind = ViewByteKind.Body;

                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle symbol '{name}'");
            }

            return true;
        }

        #endregion
        #region WorkDisasmQueue

        protected void WorkDisasmQueue(Dictionary<long, int> importMap)
        {
            Log(FileAnalyzerProgressPhase.WorkDisasmQueue);

            _cancellationToken.ThrowIfCancellationRequested();

            var disassembler = _fileDisassembler;

            if (disassembler == null)
            {
                //We don't have a disassembler, but our symbols may tell us how big each function is. But we'll defer utilizing our symbols
                //for now, because in the case of publics we might just be told the bounds of the section contrib which may be wrong
                return;
            }

            //var numThreads = Environment.ProcessorCount;
            var numThreads = 1;

            var exceptions = new List<Exception>();

            var threads = new Thread[numThreads];
            for (var i = 0; i < threads.Length; i++)
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        disassembler.WorkThreadProc(_fileAccessor, this, importMap, _globalWorkQueue, _globalWorkQueueLock, numThreads, _cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        lock (this)
                            exceptions.Add(ex);
                    }
                })
                {
                    Name = $"Disasm {i}",
                    IsBackground = true
                };

                thread.Start();

                threads[i] = thread;
            }

            //Now just wait for all threads to finish
            foreach (var thread in threads)
                thread.Join();
        }

        #endregion

        protected void DiscoverDirectories()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var dataDirectories = new ValueList<DirectoryInfo>();

            try
            {
                _viewWriter.CollectDataDirectories(ref dataDirectories);

                /* In rare circumstances, you can have _nested_ directories. e.g. you can have
                 * the ImportAddressTableDirectory actually be located _inside_ the ImportTableDirectory
                 * This happens in C:\Program Files\Microsoft Visual Studio\18\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git-receive-pack.exe.
                 * As such, we need to do the following
                 * 1. Split values overlapping the end of directories (as we normally would)
                 * 2. Construct a hierarchy of directories in the event one directory contains another */

                using var topLevelDirectories = new ValueList<RegionBuilder>();
                using var firstDirectoryByAddress = new ValueList<RegionBuilder>();
                using var stack = new ValueStack<RegionBuilder>();

                //Split any data that overlaps the start/end of each directory
                for (var i = 0; i < dataDirectories.Count; i++)
                {
                    ref var directoryInfo = ref dataDirectories.ItemRef(i);

                    //Watch out for directories that don't exist in the current ViewMode
                    if (!_fileAccessor.TryGetViewByte(directoryInfo.Start, out var pStartByte, out var sectionAccessorIndex))
                        continue;

                    //Watch out for directories that say they're bigger than the actual size available!
                    ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionAccessorIndex];

                    SplitDirectoryStart(pStartByte, sectionAccessor);

                    var pEndByte = pStartByte + directoryInfo.Length - 1;

                    var limit = sectionAccessor.pViewBytesEnd;

                    if (pEndByte >= limit)
                    {
                        var extraLength = (int) (pEndByte - limit + 1);
                        directoryInfo.End -= extraLength;
                        pEndByte = pStartByte + directoryInfo.Length - 1;
                    }

                    //Passing the ref'd DirectoryInfo on the stack _does_ update the ref in the array
                    SplitDirectoryEnd(pEndByte, limit, sectionAccessorIndex, ref directoryInfo.End);

                    while (stack.Count > 0 && directoryInfo.Start >= stack.PeekRef().End)
                        stack.Pop();

                    var newRegion = new RegionBuilder
                    {
                        Name = directoryInfo.Name,
                        Kind = ViewKind.DataDirectory,
                        Start = directoryInfo.Start,
                        End = directoryInfo.End,
                        Depth = stack.Count
                    };

                    if (stack.Count > 0)
                    {
                        ref var current = ref stack.PeekRef();

                        if (current.Children == null)
                            current.Children = new List<RegionBuilder>();

                        current.Children.Add(newRegion);

                        if (newRegion.Start != current.Start)
                            firstDirectoryByAddress.Add(newRegion);
                    }
                    else
                    {
                        topLevelDirectories.Add(newRegion);
                        firstDirectoryByAddress.Add(newRegion);
                    }

                    stack.Push(newRegion);
                }

                _fileAccessor.InstallDataDirectories(topLevelDirectories.ToArray(), firstDirectoryByAddress.ToArray());
            }
            finally
            {
                dataDirectories.Dispose();
            }
        }

        //Split any values that overlap the start and end positions of a region we're trying to create
        protected void SplitRegionBounds(long start, long end)
        {
            var pStartByte = _fileAccessor.GetViewByte(start, out var sectionAccessorIndex);

            ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionAccessorIndex];

            SplitDirectoryStart(pStartByte, sectionAccessor);

            var pEndByte = pStartByte + (end - start) - 1;

            var limit = sectionAccessor.pViewBytesEnd;

            var originalEnd = end;
            SplitDirectoryEnd(pEndByte, limit, sectionAccessorIndex, ref end);
            Debug.Assert(originalEnd == end); //I wouldn't expect that they would be modifying this
        }

        protected void SplitDirectoryStart(ViewByte* pStartByte, in SectionAccessor sectionAccessor)
        {
            var startKind = pStartByte->Kind;

            //Rewind to the start of this value and try and split it. We can split random data,
            //and don't have to worry about strings (that comes next)

            if (startKind == ViewByteKind.Body)
            {
                var pEntityStart = pStartByte;

                do
                {
                    pEntityStart--;
                } while (pEntityStart->Kind == startKind);

                //What kind of entity is overlapping the start of the section?
                switch (pEntityStart->Kind)
                {
                    case ViewByteKind.Data:
                        switch (pEntityStart->DataKind)
                        {
                            case ViewByteDataKind.Unknown:
                                //Junk; perfect! Let's split it
                                pStartByte->Kind = ViewByteKind.Data;
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ViewByteKind.Unknown:
                        //Junk; perfect! Let's split it
                        pStartByte->Kind = ViewByteKind.Unknown;

                        pStartByte++;

                        //This seems a bit risky but I guess it's safe assuming we're not the last section
                        //that only has a single byte?
                        //todo: maybe we should do a bounds check regardless
                        if (pStartByte < (sectionAccessor.pViewBytesEnd) && pStartByte->Kind == ViewByteKind.Unknown)
                            pStartByte->Kind = ViewByteKind.Body;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        protected void SplitDirectoryEnd(ViewByte* pEndByte, ViewByte* limit, int sectionAccessorIndex, ref long endOffset)
        {
            //pEndByte is the very last byte of the directory. Sometimes the listed size of a directory doesn't
            //actually match the size of the data within it. If we're in the middle of reading a valid value,
            //expand the directory to the end of it. In rare circumstances, there might be a 1 byte value at the end
            //of the current directory that's OK

            if (pEndByte->Kind != ViewByteKind.Body)
            {
                //We're either at the start of a value, or on some unknown data.

                if (pEndByte->Kind == ViewByteKind.Unknown)
                {
                    //If the next byte after us is unknown, we need to make it not unknown so we don't
                    //subsume it

                    //If this check fails, we're at the end of the section; nothing to do
                    if (pEndByte < limit - 1)
                    {
                        //There's still more data to go!
                        var nextByte = pEndByte + 1;

                        Debug.Assert(nextByte->Kind != ViewByteKind.Unknown); //We should have already converted consecutive unknowns to bodies

                        //If this check fails, the next value is different from us so we don't
                        //need to split anything
                        if (nextByte->Kind == ViewByteKind.Body)
                        {
                            //We've already converted unknown bodies, so we can just set the next byte to Unknown
                            //and now it's instantly a separate value
                            nextByte->Kind = ViewByteKind.Unknown;
                        }
                    }
                }
                else
                {
                    var length = pEndByte->GetLength(limit);

                    if (length == 1)
                        return; //The last byte in the directory is its own standalone value. This is OK

                    if (pEndByte->Kind == ViewByteKind.Data)
                    {
                        switch (pEndByte->DataKind)
                        {
                            case ViewByteDataKind.Unknown:
                            case ViewByteDataKind.Padding:
                                //If this check fails, we're at the end of the section; nothing to do
                                if (pEndByte < limit - 1)
                                {
                                    //There's still more data to go!
                                    var nextByte = pEndByte + 1;

                                    if (nextByte->Kind != ViewByteKind.Body)
                                        throw new NotImplementedException();

                                    //Just mark the next byte as padding or unknown data too and we're done.
                                    //Note that this doesn't interfere with our unknown bodies logic;
                                    //this is a legit data unknown value
                                    nextByte->Kind = ViewByteKind.Data;
                                    nextByte->DataKind = pEndByte->DataKind;
                                    return;
                                }
                                break;
                        }
                    }

                    //We're at the start of a known value, read to the end and we'll make that the "real end"

                    var pViewByte = pEndByte + 1;

                    while (pViewByte < limit)
                    {
                        if (pViewByte->Kind != ViewByteKind.Body)
                            break;

                        pViewByte++;
                    }

                    var distance = (pViewByte - pEndByte) - 1;

                    endOffset += distance;
                }
            }
            else
            {
                //We're on a body; is this the end of the current value?

                //If this check fails, this is a different entity; all good
                if (pEndByte < limit - 1)
                {
                    //There's still more data to go!
                    var nextByte = pEndByte + 1;

                    //If this check fails, this is a different entity; all good
                    if (nextByte->Kind == ViewByteKind.Body)
                    {
                        //Need to split this value if we can. Rewind to find out what our head is

                        var pEntityStart = pEndByte;

                        do
                        {
                            pEntityStart--;
                        } while (pEntityStart->Kind == ViewByteKind.Body);

                        switch (pEntityStart->Kind)
                        {
                            case ViewByteKind.Data:
                                switch (pEntityStart->DataKind)
                                {
                                    case ViewByteDataKind.Unknown:
                                        //Junk; perfect! Let's split it
                                        nextByte->Kind = ViewByteKind.Data;

                                        //No need to tag the body; all bytes after us are already body
                                        break;

                                    case ViewByteDataKind.Struct:
                                        //We're in a struct that extends past the end of the section. This commonly occurs
                                        //in the load config and resource directories
#if DEBUG
                                        var entity = _fileAccessor.GetEntity(pEntityStart, sectionAccessorIndex);
                                        Debug.Assert(entity.Kind == ViewKind.ImageLoadConfigDirectory || entity.Kind == ViewKind.UnknownResource);
#endif
                                        //Extend the length of the current directory to be the end of the current struct
                                        //todo: in the case of resources, might there be even more structs out of bounds?
                                        var structLength = pEntityStart->GetLength(limit);
                                        var lengthInsideDirectory = (int) (pEndByte - pEntityStart + 1);
                                        var lengthOutsideDirectory = structLength - lengthInsideDirectory;
                                        endOffset += lengthOutsideDirectory;
                                        break;

                                    case ViewByteDataKind.Padding:
                                        //Split the padding in two
                                        nextByte->Kind = ViewByteKind.Data;
                                        nextByte->DataKind = ViewByteDataKind.Padding;

                                        //No need to tag the body; all bytes after us are already body
                                        break;

                                    case ViewByteDataKind.Integer:
                                        //Probably a ByteBlob
                                        goto case ViewByteDataKind.Struct;

                                    case ViewByteDataKind.String:
                                        ClearString(pEntityStart, limit);

                                        ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionAccessorIndex];

                                        var origin = pEntityStart;

                                        pEntityStart--;

                                        if (pEntityStart > sectionAccessor.pViewBytes)
                                        {
                                            do
                                            {
                                                pEntityStart--;
                                            } while (pEntityStart->Kind == ViewByteKind.Body && pEntityStart->BodyKind != ViewByteBodyKind.SplitHead);

                                            if (pEntityStart->Kind == ViewByteKind.Unknown)
                                            {
                                                //Merge the start of the string we just split with the previous unknown in this section
                                                origin->Kind = ViewByteKind.Body;
                                            }
                                        }

                                        break;

                                    default:
                                        throw new NotImplementedException();
                                }
                                break;

                            case ViewByteKind.Unknown:
                                //An unknown body is overlapping the end; all good, we can just split it
                                nextByte->Kind = ViewByteKind.Unknown;
                                break;

                            case ViewByteKind.Code:
                                /* I had a ctor in the NGEN CodeManager Code Directory. This is a public,
                                 * which means we've likely overestimated the total length we need. We've already
                                 * set unknown bodies at this point, so we need to set everything after the end of
                                 * the directory to unknown + body */
                                nextByte->Kind = ViewByteKind.Unknown;

                                /* If we don't have a file disassembler, all we had was Code + Body, which means we can just
                                 * insert an Unknown byte in the middle and we're done. But if we did have a file disassembler,
                                 * then we're going to have sequences of Code + Body, Code + Body, etc. We can't just change a single
                                 * byte to Unknown, we need to loop through the whole lot and change them all to body? But we could have
                                 * had a valid piece of code after all the garbage, so it's hard to say what the bounds of the garbage
                                 * actually is */
                                if (_fileDisassembler != null)
                                    throw new NotImplementedException();

                                break;

                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
            }
        }

        protected int ClearString(ViewByte* pViewByte, ViewByte* pEnd)
        {
            pViewByte->DataKind = default;
            pViewByte->Kind = ViewByteKind.Unknown;

            var start = pViewByte;

            pViewByte++;

            Debug.Assert(_hasUnknownBodies);

            while (pViewByte < pEnd && pViewByte->Kind == ViewByteKind.Body)
            {
                pViewByte->Kind = ViewByteKind.Body;
                pViewByte++;
            }

            var length = (int) (pViewByte - start);

            return length;
        }

        public unsafe void AddXRef(int source, int target)
        {
            if (!_trackXRefs)
                return;

            _xrefs.Add(new XRef(self: source, other: target, kind: XRefKind.From));
            _xrefs.Add(new XRef(self: target, other: source, kind: XRefKind.To));
        }

        protected void Finalize(bool expandUnknownData)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            //Important to do this prior to expanding unknown data, as we may discover that a given head
            //actually points to a string
            CollectStrings();

            //I think we have to do this before expanding unknown data, because that is going to want to set
            //all unknown bytes that follow to a body; but we may have function heads in there!
            if (_fileDisassembler == null)
                ExpandUnclaimedCode();

            if (expandUnknownData)
                ExpandUnknownData();

            FinalizeCode();

            //Go through all remaining untagged bytes and mark any repeated sequences of 0x00 or 0xCC as being padding
            MarkPadding();

            SetUnknownBodies();

            DiscoverDirectories();

            //Pre-calculate the length any regions containing large structs/sequences of unknown bytes
            //so we're not constantly spinning trying to re-calculate this each time we try and inspect the entities that we have
            MarkLargeAreas();

            //Virtual so we can use our precomputed large areas
            MarkRegions();

            var writer = ((ViewByteViewWriter) _viewWriter);
            _fileAccessor.InstallRegions(writer._topLevelRegions, writer._firstRegionByAddress, _extraRegions);

            MarkNestedFiles();

            //Must do this before attempting to validate names below
            _fileAccessor.Finalize(
                _xrefs,
                _stringAddresses
            );

            _progress?.PhaseComplete(_lastPhase, GetPhaseTime());
            _progress?.PhaseComplete(FileAnalyzerProgressPhase.Max, _stopwatch.ElapsedMilliseconds);
        }

        internal void CreateOMFRegion(ImageDebugDirectory[]? debugTable)
        {
            if (debugTable == null)
                return;

            for (var i = 0; i < debugTable.Length; i++)
            {
                ref var debugDir = ref debugTable[i];

                if (debugDir.Type == IMAGE_DEBUG_TYPE.IMAGE_DEBUG_TYPE_CODEVIEW)
                {
                    //While NGEN files can contain multiple CodeView sections, for anything with OMF
                    //you would expect it to only have a single CodeView entry
                    CreateOMFRegion((ICodeViewData) debugDir.Data!);
                    break;
                }
            }
        }

        internal void CreateOMFRegion(ICodeViewData? codeViewData)
        {
            if (codeViewData == null)
                return;

            switch (codeViewData.Signature)
            {
                case CodeViewSig.DNRB:
                    var dnrb = (DNRBData) codeViewData;

                    CreateOMFRegion(dnrb.Offset, dnrb.Length, dnrb.Signature);
                    break;

                case CodeViewSig.NB00:
                case CodeViewSig.NB01:
                case CodeViewSig.NB02:
                    var nb02 = (NB02Data) codeViewData;

                    CreateOMFRegion(nb02.Offset, nb02.LfoBase, nb02.Signature);
                    break;

                case CodeViewSig.NB05:
                case CodeViewSig.NB06:
                case CodeViewSig.NB07:
                case CodeViewSig.NB08:
                case CodeViewSig.NB09:
                case CodeViewSig.NB11:
                    var nb05 = (NB05Data) codeViewData;

                    CreateOMFRegion(nb05.Offset, nb05.LfoBase, nb05.Signature);
                    break;
            }
        }

        internal void CreateOMFRegion(long start, int sizeOfData, CodeViewSig sig)
        {
            if (!_viewWriter.TryGetViewOffset(start, out var targetAddress))
                return;

            var builder = new RegionBuilder
            {
                Name = $"{sig} OMF Data",
                Kind = ViewKind.NB05Data,
                Start = targetAddress,
                End = targetAddress + sizeOfData
            };

            _extraRegions.Add(builder);
        }

        private void CollectStrings()
        {
            Log(FileAnalyzerProgressPhase.CollectStrings);

            _cancellationToken.ThrowIfCancellationRequested();

            var sectionAccessors = _fileAccessor.SectionAccessors;

            var strings = new List<long>();

            var ranges = new List<StringRange>();

            //Collect ranges where strings may exist, and then parse in parallel

            for (var i = 0; i < sectionAccessors.Length; i++)
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                _fileAccessor.GetRawSectionData(sectionAccessor, out var pBytes, out _, out _);

                var pViewByte = sectionAccessor.pViewBytes;
                var pSectionStart = pViewByte;
                var sectionAddress = sectionAccessor.StartAddress;
                var pEnd = pViewByte + sectionAccessor.Length;

                while (pViewByte < pEnd)
                {
                    //It's 25% faster using a switch than a single if statement
                    switch (pViewByte->Kind)
                    {
                        case ViewByteKind.Unknown:
                            ReadStrings(ref pViewByte, pEnd, pBytes, pSectionStart, sectionAddress, ranges);
                            break;

                        case ViewByteKind.Data:
                            switch (pViewByte->DataKind)
                            {
                                case ViewByteDataKind.String:
                                    strings.Add(sectionAddress + (int) (pViewByte - pSectionStart));
                                    break;

                                case ViewByteDataKind.Unknown:
                                    ReadStrings(ref pViewByte, pEnd, pBytes, pSectionStart, sectionAddress, ranges);
                                    break;
                            }

                            break;
                    }

                    pViewByte++;
                }
            }

            //Now actually try and find strings

            Parallel.For(0, ranges.Count, i =>
            {
                var range = ranges[i];

                StringParser.GetStrings(range.pStart, range.pBytes, range.Length, range.SectionAddress, range.SectionStart, strings);
            });

            _stringAddresses = strings.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ReadStrings(
            ref ViewByte* pViewByte,
            ViewByte* pEnd,
            byte* pBytes,
            ViewByte* pSectionStart,
            long sectionAddress,
            List<StringRange> ranges)
        {
            var pStart = pViewByte;

            if (pViewByte->Kind == ViewByteKind.Unknown && pViewByte->IsFunction)
            {
                //It's very dubious that anywhere near the start of this unknown region will in fact contain strings,
                //but we don't really know; clearly we didn't disassemble anything, and for all we know it'll be a few bytes
                //of code followed by some actual strings. So all we can really do here is just skip over the head byte
                //and pretend we didn't see anything
                pStart++;

                //We should not be immediately followed by another Unknown + IsFunction byte
            }

            do
            {
                pViewByte++;
            } while (pViewByte < pEnd && (((pViewByte->Kind == ViewByteKind.Unknown && !pViewByte->IsFunction) || pViewByte->Kind == ViewByteKind.Body)));

            //Don't think it should be possible to have body at this stage, unless we're dealing with
            //known data of an unknown kind. Note: at least one way you _could_ get Body is if we had a struct
            //and then encountered a symbol that blasted away its head to make it code instead, giving you Unknown
            //followed by a Body
            Debug.Assert(pViewByte >= pEnd || pViewByte->Kind != ViewByteKind.Body || pStart->Kind == ViewByteKind.Data);

            var length = (int) (pViewByte - pStart);

            if (length > 1)
                ranges.Add(new StringRange(pStart, pBytes + (pStart - pSectionStart), length, sectionAddress, pSectionStart));

            pViewByte--; //Don't double skip at the end
        }

        internal unsafe struct StringRange
        {
            public ViewByte* pStart;
            public byte* pBytes;
            public int Length;
            public long SectionAddress;
            public ViewByte* SectionStart;

            public long StartAddress => SectionAddress + (int) (pStart - SectionStart);

            internal StringRange(ViewByte* pStart, byte* pBytes, int length, long sectionAddress, ViewByte* pSectionStart)
            {
                this.pStart = pStart;
                this.pBytes = pBytes;
                Length = length;
                SectionAddress = sectionAddress;
                SectionStart = pSectionStart;
            }
        }

        private void ExpandUnclaimedCode()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var queue = _globalWorkQueue;

            var symbolAccessor = _fileAccessor.GetSymbolAccessor();

            if (symbolAccessor is NullSymbolAccessor)
            {
                //We're not going to know the length of each item, so we need to at least eagerly tag each item as code
                
                foreach (var item in queue)
                {
                    var pViewByte = _fileAccessor.GetViewByte(item.Address, out var sectionAccessorIndex);

                    pViewByte->Kind = ViewByteKind.Code;
                }

                //Now that we've done that, eagerly expand until we hit something else
                Parallel.ForEach(queue, item =>
                {
                    //Need to skip over the head, which we already set as code above. Setting it above is
                    //important because we need to have all the boundaries in place that we might stop at before
                    //we start processing items in parallel
                    var pViewByte = _fileAccessor.GetViewByte(item.Address, out var sectionAccessorIndex) + 1;

                    ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionAccessorIndex];
                    var limit = sectionAccessor.pViewBytesEnd;

                    while (pViewByte < limit)
                    {
                        if (pViewByte->Kind == ViewByteKind.Unknown)
                            pViewByte->Kind = ViewByteKind.Body;
                        else if (pViewByte->Kind == ViewByteKind.Data)
                        {
                            if (pViewByte->DataKind == ViewByteDataKind.Unknown)
                                pViewByte->Kind = ViewByteKind.Body;
                            else
                                break;
                        }
                        else
                            break;
                    }
                });
            }
            else
            {
                /* For each address, expand it up to the next item that follows it. We need to be careful however because when we only have
                 * public symbols, we rely on the size of the section contrib to get the length of each method, and this can sometimes (often?)
                 * be incorrect. While we could allocate a new buffer, sort the queue into it, then read it backwards, in practice I feel like
                 * it's probably fine just to write the queue in whatever order its in and then just overwrite body bytes with code as needed.
                 * This only messes up our ability to do reliable asserts */

                Parallel.ForEach(queue, item =>
                {
                    var pViewByte = _fileAccessor.GetViewByte(item.Address, out var sectionAccessorIndex);
                    var start = pViewByte;

                    ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionAccessorIndex];
                    var limit = sectionAccessor.pViewBytesEnd;

                    if (symbolAccessor.TryGetLengthFromAddress(item.RVA, _fileAccessor as ISectionDataAccessor, out var length))
                    {
                        var end = (ViewByte*) Math.Min((long) (pViewByte + length), (long) limit);

                        //If we erroneously detected this sequence of bytes as being a string, we need to convert it back to code
                        //and skip over its bytes before we continue eating regular bytes

                        if (pViewByte->Kind == ViewByteKind.Data && (pViewByte->DataKind == ViewByteDataKind.String || pViewByte->DataKind == ViewByteDataKind.Unknown))
                        {
                            pViewByte->DataKind = default;
                            pViewByte->Kind = ViewByteKind.Code;
                            pViewByte++;

                            while (pViewByte < end)
                            {
                                if (pViewByte->Kind != ViewByteKind.Body)
                                    break;

                                pViewByte++;
                            }
                        }
                        else
                        {
                            //We allow any type _but_ Data here
                            Debug.Assert(pViewByte->Kind != ViewByteKind.Data); //Kind may aleady be code if we ran into an unknown other than body above
                            pViewByte->Kind = ViewByteKind.Code;

                            pViewByte++;
                        }

                        while (pViewByte < end)
                        {
                            //In the event we're dealing with public symbols, and we only had a section contrib to tell us our length,
                            //if we're in the same section contrib and have to change the Kind from Body to Code above, we expect
                            //our length will go to the end of the same section contrib as well, thus I think it's safe to abort early
                            if (pViewByte->Kind != ViewByteKind.Unknown)
                            {
                                if (pViewByte->Kind == ViewByteKind.Data)
                                {
                                    switch (pViewByte->DataKind)
                                    {
                                        case ViewByteDataKind.String:
                                            //Get rid of this string
                                            pViewByte->DataKind = default; //Note that it's possible for us to race with another thread also trying to clear the data kind of this byte
                                            break;

                                        case ViewByteDataKind.Unknown:
                                        case ViewByteDataKind.Padding:
                                            break;

                                        default:
                                            //We've run into a real entity. Likely we had a public whose length was way too much.
                                            //It's the end of the line for us
                                            return;
                                    }
                                }
                                else if (pViewByte->Kind == ViewByteKind.Code)
                                {
                                    /* It's possible that the previous function ended with a jmp, the previous function
                                     * is right after it and we had a section contrib that overestimated the actual length
                                     * of the function. But it's just as possible we had a race with another thread that
                                     * was processing an S_BLOCK symbol, and if we abort now we won't cover the entire length
                                     * of the function. As such, we'll only abort if we know that this address we're on is
                                     * also a function */
                                    if (pViewByte->IsFunction)
                                        break;
                                }
                            }
                            else
                            {
                                if (pViewByte->IsFunction)
                                    break; //There's a function that hasn't been claimed yet; it should still be in the queue
                            }

                            pViewByte->Kind = ViewByteKind.Body;
                            pViewByte++;
                            Debug.Assert(pViewByte >= end || pViewByte->BodyKind == ViewByteBodyKind.None);
                        }

                        /* By the time we get to the end, we ostensibly shouldn't be inside of another entity. If end
                         * is set to Body, this means we're we trampled over something (which may or may not have been junk)
                         * or we had a label inside of this function, and that body belongs to the label. e.g. __commit and "good"
                         * in our VC50 sample. Setting the remaining body to unknown could be problematic...because the label
                         * might have covered some 0xCC bytes after it while the function name may not have, and depending on
                         * the order in which these work items are processed we'll get different results. So perhaps for now
                         * just subsume any body bytes that may follow us */
                    }
                    else
                    {
                        /* If we don't have any symbols at all, we'll handle this in the if statement above. But
                         * even if we do have symbols, it's still conceivable that we might not be able to get the length
                         * of a given area of code.
                         * - If we're a managed executable, we'll have added some code for the EntryPoint
                         *   that jumps into mscoree.dll. However our managed symbols don't know anything
                         *   about this native stub
                         * - There literally might not be any symbols associated with a given area of code, so it's not
                         *   our fault we can't say anything about it (and depending on our symbol provider, section
                         *   contribs may not be available?)
                         * 
                         * As such, fallback to just claiming code up until the next non-body item we encounter */

                        pViewByte->Kind = ViewByteKind.Code;
                        pViewByte++;

                        while (pViewByte < limit)
                        {
                            if (pViewByte->Kind == ViewByteKind.Unknown)
                                pViewByte->Kind = ViewByteKind.Body;
                            else if (pViewByte->Kind == ViewByteKind.Data)
                            {
                                if (pViewByte->DataKind == ViewByteDataKind.Unknown)
                                    pViewByte->Kind = ViewByteKind.Body;
                                else
                                    break;
                            }
                            else
                                break;
                        }
                    }
                });
            }

            queue.Clear();
        }

        #region ExpandUnknownData

        private void ExpandUnknownData()
        {
            Log(FileAnalyzerProgressPhase.ExpandUnknownData);

            _cancellationToken.ThrowIfCancellationRequested();

            //Try and expand all unknown data items up to the start of the next item that follows them.
            //Any items that are xref'd to from other values should already have been tagged

            var sectionAccessors = _fileAccessor.SectionAccessors;

            for (var i = 0; i < sectionAccessors.Length; i++)
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                var pViewByte = sectionAccessor.pViewBytes;
                var pEnd = pViewByte + sectionAccessor.Length;

                while (pViewByte < pEnd)
                {
#if DEBUG
                    var targetAddress = sectionAccessor.StartAddress + (pViewByte - sectionAccessor.pViewBytes);
#endif

                    if (pViewByte->Kind == ViewByteKind.Data && pViewByte->DataKind == ViewByteDataKind.Unknown)
                    {
                        pViewByte++;

                        //I don't know that we necessarily need to mark split heads/tails. We don't know whether a given piece of data will span multiple pages
                        //or not. It's kind of hard to say. Also I know we made a comment in ViewByte.GetUnknownLength about the fact we rely on unknown data _not_ being split
                        //over multiple pages

                        //Seize all unknown bytes that follow
                        while (pViewByte < pEnd)
                        {
                            if (pViewByte->Kind == ViewByteKind.Unknown)
                            {
                                if (pViewByte->IsFunction)
                                {
                                    //This is some code we didn't process because we didn't have a decompiler. We need to mark it as code,
                                    //not a data body
                                    pViewByte->Kind = ViewByteKind.Code;
                                }
                                else
                                {
                                    pViewByte->Kind = ViewByteKind.Body;
                                }

                                pViewByte++;
                            }
                            else
                                break;
                        }
                    }
                    else
                        pViewByte++;

                    //An additional enhancement we can potentially apply here is to try and detect RVAs. What constitutes an RVA?
                    //Perhaps use some heuristics: for each 32-bit aligned offset, if there's at least 4 bytes remaining, try and interpret
                    //the bytes as an RVA. If the value is 32-bit aligned and points to a valid section after the header, treat it
                    //like an RVA? If the RVA points into code, or the body of something, that might be a bit dubious
                    //Looks like you can't rely on the source being even, I've confirmed the source can be misaligned
                    //e.g. in GFIDS
                }
            }
        }

        private void SetUnknownBodies()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var sectionAccessors = _fileAccessor.SectionAccessors;

            Parallel.For(0, sectionAccessors.Length, i =>
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                _fileAccessor.GetRawSectionData(sectionAccessor, out var pBytes, out _, out _);

                var pViewByte = sectionAccessor.pViewBytes;
                var pEnd = pViewByte + sectionAccessor.Length;

                while (pViewByte < pEnd)
                {
                    if (pViewByte->Kind == ViewByteKind.Unknown)
                    {
                        //Set all subsequent unknown bytes to be body

                        pViewByte++;

                        while (pViewByte < pEnd && pViewByte->Kind == ViewByteKind.Unknown)
                        {
                            pViewByte->Kind = ViewByteKind.Body;
                            pViewByte++;
                        }
                    }
                    else
                        pViewByte++;
                }
            });

            _hasUnknownBodies = true;
        }

        #endregion

        protected virtual void FinalizeCode()
        {
        }

        #region MarkPadding

        protected virtual void MarkPadding()
        {
            Log(FileAnalyzerProgressPhase.MarkPadding);

            _cancellationToken.ThrowIfCancellationRequested();

            var sectionAccessors = _fileAccessor.SectionAccessors;

            Parallel.For(0, sectionAccessors.Length, i =>
            {
                ViewByteKind lastKind = ViewByteKind.Unknown;

                ref var sectionAccessor = ref sectionAccessors[i];

                _fileAccessor.GetRawSectionData(sectionAccessor, out var pBytes, out _, out _);

                var pViewByte = sectionAccessor.pViewBytes;
                var pEnd = pViewByte + sectionAccessor.Length;

                while (pViewByte < pEnd)
                {
#if DEBUG
                    var targetAddress = sectionAccessor.StartAddress + (pViewByte - sectionAccessor.pViewBytes);
#endif

                    var kind = pViewByte->Kind;

                    if (kind == ViewByteKind.Unknown)
                    {
                        var start = *pBytes;

                        switch (*pBytes)
                        {
                            case 0x90:
                            case 0xCC:
                                if (lastKind == ViewByteKind.Unknown)
                                {
                                    //There's two possibilities: the unknown data is followed by something valid, or
                                    //the unknown data is followed by something that is also unknown. Either way, we have no way
                                    //of telling whether the bytes are padding or not. Skip over all 0 bytes; we can't
                                    //classify them as padding

                                    pViewByte++;
                                    pBytes++;

                                    while (pViewByte < pEnd)
                                    {
                                        if (pViewByte->Kind == ViewByteKind.Unknown && *pBytes == start)
                                        {
                                            pViewByte++;
                                            pBytes++;
                                        }
                                        else
                                            break;
                                    }
                                }
                                else
                                {
                                    //The last kind was not unknown, but the next kind might be!

                                    var pViewStart = pViewByte;
                                    var pBytesStart = pBytes;

                                    while (pViewByte < pEnd)
                                    {
                                        if (pViewByte->Kind == ViewByteKind.Unknown && *pBytes == start)
                                        {
                                            pViewByte++;
                                            pBytes++;
                                        }
                                        else
                                        {
                                            //We've gone past the last targetByte byte, and there's still more data to process. If the next byte
                                            //is Unknown, bail out and leave the bytes as unknown

                                            if (pViewByte->Kind == ViewByteKind.Unknown)
                                            {
                                                lastKind = ViewByteKind.Unknown;
                                            }
                                            else
                                            {
                                                //All good; go ahead and mark these bytes as padding

                                                pViewStart->Kind = ViewByteKind.Data;
                                                pViewStart->DataKind = ViewByteDataKind.Padding;
                                                pViewStart++;
                                                pBytesStart++;

                                                while (pViewStart < pViewByte)
                                                {
                                                    //todo: need to mark splithead/splittail for pdbs
                                                    pViewStart->Kind = ViewByteKind.Body;
                                                    pViewStart++;
                                                    pBytesStart++;
                                                }

                                                lastKind = ViewByteKind.Data;
                                            }

                                            break;
                                        }
                                    }
                                }
                                break;

                            case 0x00:
                                //We will treat 0 as padding when there are at least 4 0's in a row, or when there's very clearly a known entity to the left and right of these bytes. This means that it's not a trailing 0 and a null terminator
                                //from a UTF-16 string
                                if (pViewByte + 3 < pEnd)
                                {
                                    if (pBytes[1] == 0 && pBytes[2] == 0 && pBytes[3] == 0)
                                    {
                                        //We're potentially willing to mark this data as padding...IF it's not also in the middle of an unknown section.
                                        //If there are other types of unknown bytes either side of this padding, it's noise to say that this is "padding", because
                                        //there isn't anything concrete that's being padded

                                        goto case 0xCC;
                                    }
                                    else
                                    {
                                        if (pViewByte > sectionAccessor.pViewBytes && (pViewByte - 1)->Kind == ViewByteKind.Body)
                                        {
                                            var smallEnd = pViewByte + 3;

                                            var pLocal = pViewByte + 1;

                                            var allBytesUpToNextEntityAre0 = true;

                                            var j = 1;

                                            for (; j < 3; j++)
                                            {
                                                if (pBytes[j] != 0)
                                                {
                                                    if (pViewByte[j].Kind == ViewByteKind.Unknown)
                                                        allBytesUpToNextEntityAre0 = false;

                                                    break;
                                                }
                                                else
                                                {
                                                    if (pViewByte[j].Kind != ViewByteKind.Unknown)
                                                        break;
                                                }
                                            }

                                            if (allBytesUpToNextEntityAre0)
                                            {
                                                //All good; go ahead and mark these bytes as padding

                                                var end = pViewByte + j;

                                                pViewByte->Kind = ViewByteKind.Data;
                                                pViewByte->DataKind = ViewByteDataKind.Padding;
                                                pViewByte++;
                                                pBytes++;

                                                while (pViewByte < end)
                                                {
                                                    //todo: need to mark splithead/splittail for pdbs
                                                    pViewByte->Kind = ViewByteKind.Body;
                                                    pViewByte++;
                                                    pBytes++;
                                                }

                                                //We're now on the head of some known entity, so it's OK to do pViewByte++ and pBytes++ below
                                            }
                                        }

                                        pViewByte++;
                                        pBytes++;
                                    }
                                }
                                else
                                {
                                    pViewByte++;
                                    pBytes++;
                                }
                                break;

                            default:
                                lastKind = ViewByteKind.Unknown;
                                pViewByte++;
                                pBytes++;
                                break;
                        }
                    }
                    else
                    {
                        if (kind != ViewByteKind.Body)
                            lastKind = kind;

                        pViewByte++;
                        pBytes++;
                    }
                }
            });
        }

        #endregion

        private void MarkLargeAreas()
        {
            Log(FileAnalyzerProgressPhase.MarkLargeAreas);

            _cancellationToken.ThrowIfCancellationRequested();

            var sectionAccessors = _fileAccessor.SectionAccessors;

            var objLock = new object();
            var largeAddresses = new Dictionary<long, int>();

            Debug.Assert(_hasUnknownBodies);

            Parallel.For(0, sectionAccessors.Length, i =>
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                var offset = sectionAccessor.StartAddress;

                _fileAccessor.GetRawSectionData(sectionAccessor, out var pBytes, out _, out _);

                var pViewByte = sectionAccessor.pViewBytes;
                var pEnd = pViewByte + sectionAccessor.Length;

                while (pViewByte < pEnd)
                {
                    int length;

                    switch (pViewByte->Kind)
                    {
                        case ViewByteKind.Unknown:
                            //We should already have unknown bodies at this point
                            length = pViewByte->GetLength(pEnd);
                            break;

                        default:
                            length = pViewByte->GetLength(pEnd);
                            break;
                    }

                    if (length > 1000)
                    {
                        var off = offset + (int) (pViewByte - sectionAccessor.pViewBytes);

                        lock (objLock)
                            largeAddresses[off] = length;
                    }

                    pViewByte += length;
                }
            });

            _fileAccessor.LargeAddresses = largeAddresses;
        }

        protected virtual void MarkRegions()
        {
        }

        protected virtual void MarkNestedFiles()
        {
        }

        protected void MarkSymbols(ref ViewByte* pViewByte, ref long targetAddress, ViewByte* pEnd) =>
            MarkStructRange(ref pViewByte, ref targetAddress, pEnd, "Symbols", ViewKind.Symbols, ViewKind.SymType, ViewKind.LfAlias);

        protected void MarkTypes(ref ViewByte* pViewByte, ref long targetAddress, ViewByte* pEnd) =>
            MarkStructRange(ref pViewByte, ref targetAddress, pEnd, "Types", ViewKind.Types, ViewKind.LfAlias, ViewKind.GSIHashHdr);

        protected void MarkStructRange(
            ref ViewByte* pViewByte,
            ref long targetAddress,
            ViewByte* pEnd,
            string name,
            ViewKind regionKind,
            ViewKind first,
            ViewKind last) //last should be +1 after end
        {
            var builder = new RegionBuilder
            {
                Name = name,
                Kind = regionKind,
                Start = targetAddress,
            };

            int length;

            while (pViewByte < pEnd)
            {
                if (pViewByte->Kind == ViewByteKind.Data && pViewByte->DataKind == ViewByteDataKind.Struct)
                {
                    var kind = _fileAccessor.GetStructKind(targetAddress);

                    if (kind >= first && kind < last)
                    {
                        length = pViewByte->GetLength(pEnd);

                        pViewByte += length;
                        targetAddress += length;
                    }
                    else
                        break;
                }
                else
                    break;
            }

            builder.End = targetAddress;

            _extraRegions.Add(builder);
        }

        internal void MarkInterSectionData(int lastSectionEnd, int start, bool canHaveRelocations) =>
            MarkInterSectionData(lastSectionEnd, start, canHaveRelocations, _fileAccessor, _extraRegions);

        internal static void MarkInterSectionData(
            int lastSectionEnd,
            int start,
            bool canHaveRelocations,
            FileAccessor fileAccessor,
            List<RegionBuilder> extraRegions)
        {
            if (lastSectionEnd != -1 && start > lastSectionEnd)
            {
                var interSectionLength = start - lastSectionEnd;

                var interSectionName = "Inter-Section Data";
                var interSectionKind = ViewKind.InterSectionData;

                var interSectionEnd = lastSectionEnd + interSectionLength;

                if (canHaveRelocations)
                {
                    //We only have one section
                    var iterator = fileAccessor.EnumerateEntities(sectionAccessorIndex: 0);
                    iterator.MoveTo(lastSectionEnd);

                    //If all entities within the region are relocations, we can say that this is a relocation
                    //region instead
                    if (iterator.MoveNext() && iterator.Current.Kind == ViewKind.ImageRelocation)
                    {
                        var hasOtherData = false;

                        while (iterator.MoveNext() && iterator.Current.TargetAddress < interSectionEnd)
                        {
                            if (iterator.Current.Kind != ViewKind.ImageRelocation)
                            {
                                hasOtherData = true;
                                break;
                            }
                        }

                        if (!hasOtherData)
                        {
                            interSectionName = "Relocations";
                            interSectionKind = ViewKind.Relocations;
                        }
                    }
                }

                extraRegions.Add(new RegionBuilder
                {
                    Name = interSectionName,
                    Kind = interSectionKind,
                    Start = lastSectionEnd,
                    End = interSectionEnd
                });
            }
        }

#if DEBUG
        protected void ValidateNames()
        {
            //Any byte that says it has a name should actually have a name

            var sectionAccessors = _fileAccessor.SectionAccessors;

            for (var i = 0; i < sectionAccessors.Length; i++)
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                var pViewByte = sectionAccessor.pViewBytes;
                var address = sectionAccessor.StartAddress;
                var pEnd = pViewByte + sectionAccessor.Length;

                while (pViewByte < pEnd)
                {
                    if (pViewByte->HasName)
                    {
                        _ = _fileAccessor.GetNameFromViewByte(address, i, pViewByte);
                    }

                    pViewByte++;
                    address++;
                }
            }
        }
#endif
    }
}
