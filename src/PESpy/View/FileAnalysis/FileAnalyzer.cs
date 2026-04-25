using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ClrDebug.DIA;
using ClrDebug.OMF;
using PESpy.PDB;
using PESpy.View.Builder;
using static ClrDebug.PDB.SYM_ENUM_e;

namespace PESpy.View
{
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

                Analyze(fileAccessor, disassembler, httpPolicy, progress);
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
            IFileDisassembler disassembler = null,
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            IFileAnalyzerProgress? progress = null,
            bool trackXRefs = true,
            CancellationToken cancellationToken = default)
        {
            AnalyzeInternal(fileAccessor, disassembler, httpPolicy, progress, trackXRefs, cancellationToken);

            GCLargeObjectHeap();
        }

        private static void AnalyzeInternal(
            FileAccessor fileAccessor,
            IFileDisassembler disassembler,
            LocatorHttpPolicy httpPolicy,
            IFileAnalyzerProgress? progress,
            bool trackXRefs,
            CancellationToken cancellationToken)
        {
            FileAnalyzer fileAnalyzer = fileAccessor.File.Kind switch
            {
                FileKind.PE => new PEFileAnalyzer((PEFileAccessor) fileAccessor, disassembler, httpPolicy, progress, trackXRefs, cancellationToken),
                //FileKind.NE          => new NEFileAnalyzer((NEFileAccessor) fileAccessor, disassembler, progress),
                //FileKind.LE          => new LEFileAnalyzer((LEFileAccessor) fileAccessor, disassembler, progress),
                //FileKind.DOS         => new DOSFileAnalyzer((DOSFileAccessor) fileAccessor, disassembler, progress),
                //FileKind.DBG         => new DBGFileAnalyzer((DBGFileAccessor) fileAccessor, disassembler, progress),
                FileKind.PDB => new PDBFileAnalyzer((PDBFileAccessor) fileAccessor, progress, trackXRefs, cancellationToken),
                //FileKind.PortablePDB => new PortablePDBFileAnalyzer((PortablePDBFileAccessor) fileAccessor, disassembler, progress),
                //FileKind.OBJ         => new OBJFileAnalyzer((OBJFileAccessor) fileAccessor, disassembler, progress),
                //FileKind.LIB         => new LIBFileAnalyzer((LIBFileAccessor) fileAccessor, disassembler, progress),
                //FileKind.OMF         => new OMFFileAnalyzer((OMFFileAccessor) fileAccessor, disassembler, progress),
                //FileKind.OMFLIB      => new OMFLIBFileAnalyzer((OMFLIBFileAccessor) fileAccessor, disassembler, progress),
                _ => throw new NotImplementedException($"Don't know how to analyze a file of type '{fileAccessor.File.Kind}'")
            };

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
        protected readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        protected long _lastStopwatchCheckpoint;
        private FileAnalyzerProgressPhase _lastPhase;

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

        //Parallel arrays that map a given name to who uses that name
        private Dictionary<FixedUtf8String, int> _nameToIndexMap = new Dictionary<FixedUtf8String, int>();
        private List<(FixedUtf8String name, List<int> refs)> _names = new List<(FixedUtf8String name, List<int> refs)>();
        private int _numNameRefs;

        private int[] _stringAddresses;

        //The limiting factor on performance now seems to be lock contention. I tried to do away with the lock and use
        //concurrent queue instead (forgetting about checking whether an address has already been processed or not) but that
        //ended up being slower. Checking the Usage before adding to candidate additions also didn't help
        private readonly Queue<WorkItem> _globalWorkQueue = new Queue<WorkItem>();
        private readonly object _globalWorkQueueLock = new object();
        protected readonly ViewWriter _viewWriter;

        protected readonly CancellationToken _cancellationToken;

        protected FileAnalyzer(
            FileAccessor fileAccessor,
            IFileDisassembler? fileDisassembler,
            LocatorHttpPolicy httpPolicy,
            IFileAnalyzerProgress? progress,
            bool trackXRefs,
            CancellationToken cancellationToken)
        {
            _fileAccessor = fileAccessor;
            _fileDisassembler = fileDisassembler;
            _httpPolicy = httpPolicy;
            _progress = progress;
            _viewWriter = CreateViewWriter();
            _trackXRefs = trackXRefs;
            _cancellationToken = cancellationToken;

            if (trackXRefs)
                _xrefs = new List<XRef>();
        }

        protected abstract ViewWriter CreateViewWriter();

        public abstract void Execute();

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

        internal void AddName(int targetAddress, ViewByte* pViewByte, FixedUtf8String name)
        {
            Debug.Assert(name.Length > 0);

            if (_nameToIndexMap.TryGetValue(name, out var nameIndex))
            {
                _names[nameIndex - 1].refs.Add(targetAddress);
                _numNameRefs++;
            }
            else
            {
                _names.Add((name, new List<int>
                {
                    targetAddress
                }));
                _numNameRefs++;

                nameIndex = _names.Count; //Indices start at 1

                _nameToIndexMap[name] = nameIndex;
            }

            _fileAccessor.AddName(targetAddress, nameIndex);

            pViewByte->HasName = true;
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

                case SymbolAccessorKind.CodeView:
                    //We don't currently have an NB02 symbol accessor; that would break this.
                    //Note that NB09 derives from NB05
                    ProcessCodeViewSymbols(((NB05SymbolAccessor) symbolAccessor).data, sectionDataAccessor);
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

            //Another complicating factor we have is that symbols for managed assemblies often seem to contain complete gargage
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
                            ProcessPubSym32(thunkEntry.Thunk, pdbFile, sectionDataAccessor);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
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

        internal void ProcessCodeViewSymbols(NB05Data data, ISectionDataAccessor sectionDataAccessor)
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
                        throw new NotImplementedException();
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
                    throw new NotImplementedException();

                default:
                    //Some symbols point to sections that don't exist, so their RVAs are 0
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
                    AddName(targetAddress, pViewByte, name);
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
                var pViewByte = AddCode(targetAddress, rva, sectionIndex);

                if (name.Length > 0)
                    AddName(targetAddress, pViewByte, name);
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
                    AddName(targetAddress, pViewByte, name);
            }
        }

        private void ProcessPubSym32(
            SymType symType,
            ICodeViewAccessor codeViewAccessor,
            ISectionDataAccessor sectionDataAccessor)
        {
            //Some symbols point to sections that don't exist, so their RVAs are 0
            if (!symType.TryGetRVA(codeViewAccessor, out var rva) || rva == 0)
                return;

            if (!_fileAccessor.TryGetTargetAddress(rva, out var targetAddress, out var sectionIndex))
                return;

            var pubSym32 = (PubSym32) symType;

            var name = (FixedUtf8String) symType.GetName(codeViewAccessor);

            //Things without names might still be code, so we can't just bail out, as we want
            //to collect all code addresses

            if (TryHandleSpecialPublic(name, rva, targetAddress, sectionIndex))
                return;

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

        private bool TryHandleSpecialPublic(
            FixedUtf8String name,
            int rva,
            int targetAddress,
            int sectionIndex)
        {
            //We're going to be doing multiple name checks, so get the length once
            var span = name.AsSpan();

            /* If we're NativeAOT, we should also check for a __modules_a symbol. This marks the start of
             * the area where pseudo ReadyToRunReader entries live. InitializeModules() is then called with the difference between __modules_z and __modules_a being
             * the count. (need to consider the size of a pointer here). Watch out, because InitializeRuntime + InitializeModules may be inlined into main
             * 
             * These are not the same thing as READYTORUN_HEADER (which PESpy calls "ReadyToRunHeader"), these are literally also called
             * "ReadyToRunHeader" and have a slightly different layout. The end of these headers is demarcated by a __modules_z symbol.
             * Following the header are a number of ModuleInfoRow items (defined in TypeManager.h)
             * https://github.com/dotnet/runtime/blob/e5ae1f68938942fd6a65c65fecc7cafda8835e82/src/coreclr/nativeaot/Bootstrap/main.cpp#L189
             * Rather than spend time checking this in the main symbol processing loop, we should special case it afterwards if we know we're NativeAOT */

#if NET
            if (span.StartsWith("??_C@_"u8))
            {
                //It's a string literal. If a 1 follows it's wide, if a 0 follows it's ANSI.
                //Then, following this is a length

                var textWindow = new Demangler.TextWindow(name.Value, span.Length, true);
                textWindow.AdvanceChar(6);

                if (textWindow.TryNextChar(out var stringKind) && Demangler.TryParseNumber(ref textWindow, out _, out var length))
                {
                    //The length should be the true length of the string. The symbol name only includes up to the first 64 characters
                    _fileAccessor.AddString(targetAddress, sectionIndex, isWide: stringKind == '1', numBytes: (int) length);
                }

                return true;
            }
            else if (span.StartsWith("??_7"u8))
            {
                //It's a vftable. Add each entry as code in the work queue, and
                //also add xrefs from each slot to the target function
                throw new NotImplementedException();
            }
            else if (span.StartsWith("__IMPORT_DESCRIPTOR"u8))
            {
                //Any time you have an __IMPORT_DESCRIPTOR symbol, this points to an ImageImportDescriptor struct.
                //I would expect that we already tagged all of these
#if DEBUG
                var pViewByte = _fileAccessor.GetViewByteForSection(targetAddress, sectionIndex);

                //todo: should we also add the symbol name?
                Debug.Assert(pViewByte->Kind == ViewByteKind.Data);
#endif
            else if (span.StartsWith("__real@"u8))
            {
                //Ostensibly it's going to be __real@ followed by either 8 or 16 hex digits. I'm not sure if you could ever
                //have any other characters at the end; for now; we'll just assume it'll always be the simple case

                ViewByte* pViewByte;

                switch (span.Length)
                {
                    case 8 + 7: //__real@ + 8 chars
                        pViewByte = _fileAccessor.AddData(targetAddress, sectionIndex, ViewByteDataKind.Decimal, 4); //float (single)
                        AddName(targetAddress, pViewByte, new FixedUtf8String(name.Value, span.Length));
                        break;

                    case 16 + 7: //__real@ + 16 chars
                        pViewByte = _fileAccessor.AddData(targetAddress, sectionIndex, ViewByteDataKind.Decimal, 8); //float (double)
                        AddName(targetAddress, pViewByte, new FixedUtf8String(name.Value, span.Length));
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle symbol '{name}'");
                }

                return true;
            }
#endif

            return false;
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

            var dataDirectories = new PooledList<DirectoryInfo>();

            try
            {
                _viewWriter.CollectDataDirectories(ref dataDirectories);

                /* In rare circumstances, you can have _nested_ directories. e.g. you can have
                 * the ImportAddressTableDirectory actually be located _inside_ the ImportTableDirectory
                 * This happens in C:\Program Files\Microsoft Visual Studio\18\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git-receive-pack.exe.
                 * As such, we need to do the following
                 * 1. Split values overlapping the end of directories (as we normally would)
                 * 2. Construct a hierarchy of directories in the event one directory contains another */

                using var topLevelDirectories = new PooledList<RegionBuilder>();
                using var firstDirectoryByAddress = new PooledList<RegionBuilder>();
                using var stack = new ValueStack<RegionBuilder>();

                //Split any data that overlaps the start/end of each directory
                for (var i = 0; i < dataDirectories.Count; i++)
                {
                    ref var directoryInfo = ref dataDirectories.ItemRef(i);

                    //Watch out for directories that don't exist in the current ViewMode
                    if (!_fileAccessor.TryGetViewByte(directoryInfo.Start, out var pStartByte, out var sectionAccessorIndex))
                        continue;

                    SplitDirectoryStart(pStartByte, sectionAccessorIndex);

                    var pEndByte = pStartByte + directoryInfo.Length - 1;

                    //Watch out for directories that say they're bigger than the actual size available!
                    ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionAccessorIndex];

                    var limit = sectionAccessor.pViewBytes + sectionAccessor.Length;

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

        protected void SplitDirectoryStart(ViewByte* pStartByte, int sectionAccessorIndex)
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
                if (pEntityStart->Kind == ViewByteKind.Data && pEntityStart->DataKind == ViewByteDataKind.Unknown)
                {
                    //Junk; perfect! Let's split it
                    pStartByte->Kind = ViewByteKind.Data;
                }
                else
                    throw new NotImplementedException();
            }
            else if (startKind == ViewByteKind.Unknown)
            {
                //Be careful not to run off the start of the section!

                var pEntityStart = pStartByte;

                var pViewBytes = _fileAccessor.SectionAccessors[sectionAccessorIndex].pViewBytes;

                do
                {
                    pEntityStart--;
                } while (pEntityStart >= pViewBytes && pEntityStart->Kind == startKind);

                pEntityStart++;

                pStartByte->Kind = ViewByteKind.Data;

                //Convert all unknowns after us into a body

                ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionAccessorIndex];

                var limit = sectionAccessor.pViewBytes + sectionAccessor.Length;

                var pByte = pStartByte + 1;

                while (pByte < limit)
                {
                    if (pByte->Kind != ViewByteKind.Unknown)
                        break;

                    pByte->Kind = ViewByteKind.Body;
                    pByte++;
                }
            }
        }

        protected void SplitDirectoryEnd(ViewByte* pEndByte, ViewByte* limit, int sectionAccessorIndex, ref int endOffset)
        {
            //pEndByte is the very last byte of the directory. Sometimes the listed size of a directory doesn't
            //actually match the size of the data within it. If we're in the middle of reading a valid value,
            //expand the directory to the end of it. In rare circumstances, there might be a 1 byte value at the end
            //of the current directoryl that's OK

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

                        //If this check fails, the next value is different from us so we don't
                        //need to split anything
                        if (nextByte->Kind == ViewByteKind.Unknown)
                        {
                            //This is a bit unfortunate, but I don't think we have any other choice
                            //to split the data
                            nextByte->Kind = ViewByteKind.Data;

                            for (var j = nextByte + 1; j < limit; j++)
                            {
                                if (j->Kind != ViewByteKind.Unknown)
                                    break;

                                j->Kind = ViewByteKind.Body;
                            }
                        }
                    }
                }
                else
                {
                    var length = pEndByte->GetLength(limit);

                    if (length == 1)
                        return; //The last byte in the directory is its own standalone value. This is OK

                    if (pEndByte->Kind == ViewByteKind.Data && pEndByte->DataKind == ViewByteDataKind.Unknown)
                    {
                        //If this check fails, we're at the end of the section; nothing to do
                        if (pEndByte < limit - 1)
                        {
                            //There's still more data to go!
                            var nextByte = pEndByte + 1;

                            if (nextByte->Kind != ViewByteKind.Body)
                                throw new NotImplementedException();

                            //Just mark the next byte as unknown data too and we're done
                            nextByte->Kind = ViewByteKind.Data;
                            nextByte->DataKind = ViewByteDataKind.Unknown;
                        }

                        return;
                    }

                    //We're at the start of a known value, read to the end and we'll make that the "real end"

                    throw new NotImplementedException();
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
                                        Debug.Assert(entity.Kind == ViewKind.ImageLoadConfigDirectory);
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

                                    default:
                                        throw new NotImplementedException();
                                }
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
            }
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

            if (expandUnknownData)
                ExpandUnknownData();

            if (_fileDisassembler == null)
                ExpandUnclaimedCode();

            FinalizeCode();

            //Go through all remaining untagged bytes and mark any repeated sequences of 0x00 or 0xCC as being padding
            MarkPadding();

            //Pre-calculate the length any regions containing large structs/sequences of unknown bytes
            //so we're not constantly spinning trying to re-calculate this each time we try and inspect the entities that we have
            MarkLargeAreas();

            //Virtual so we can use our precomputed large areas
            MarkRegions();
            MarkNestedFiles();

            //Must do this before attempting to validate names below
            _fileAccessor.Finalize(
                _xrefs,
                _names,
                _numNameRefs,
                _stringAddresses
            );

            _progress?.PhaseComplete(_lastPhase, GetPhaseTime());
            _progress?.PhaseComplete(FileAnalyzerProgressPhase.Max, _stopwatch.ElapsedMilliseconds);
        }

        private void CollectStrings()
        {
            Log(FileAnalyzerProgressPhase.CollectStrings);

            _cancellationToken.ThrowIfCancellationRequested();

            var sectionAccessors = _fileAccessor.SectionAccessors;

            var strings = new List<int>();

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
            int sectionAddress,
            List<StringRange> ranges)
        {
            var pStart = pViewByte;

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

            ranges.Add(new StringRange(pStart, pBytes + (pStart - pSectionStart), length, sectionAddress, pSectionStart));
            pViewByte--; //Don't double skip at the end
        }

        internal unsafe struct StringRange
        {
            public ViewByte* pStart;
            public byte* pBytes;
            public int Length;
            public int SectionAddress;
            public ViewByte* SectionStart;

            public int StartAddress => SectionAddress + (int) (pStart - SectionStart);

            internal StringRange(ViewByte* pStart, byte* pBytes, int length, int sectionAddress, ViewByte* pSectionStart)
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
                    var pViewByte = _fileAccessor.GetViewByte(item.Address, out var sectionAccessorIndex);

                    ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionAccessorIndex];
                    var limit = sectionAccessor.pViewBytes + sectionAccessor.Length;

                    //If we erroneously detected this sequence of bytes as being a string, we need to convert it back to code
                    //and skip over its bytes before we continue eating regular bytes

                Parallel.ForEach(queue, item =>
                {
                    if (symbolAccessor.TryGetLengthFromAddress(item.RVA, _fileAccessor as ISectionDataAccessor, out var length))
                    {
                        var pViewByte = _fileAccessor.GetViewByte(item.Address, out var sectionAccessorIndex);
                        var start = pViewByte;

                        ref var sectionAccessor = ref _fileAccessor.SectionAccessors[sectionAccessorIndex];
                        var limit = sectionAccessor.pViewBytes + sectionAccessor.Length;
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
                                    if (pViewByte->DataKind == ViewByteDataKind.String)
                                    {
                                        //Get rid of this string
                                        pViewByte->DataKind = default;
                                    }
                                }
                                else if (pViewByte->Kind == ViewByteKind.Code)
                                    break; //e.g. the previous function ended with a jmp and the next function started right after it
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

                        if (end < limit && end->Kind == ViewByteKind.Body)
                        {
                            //I'm going to assume we just wrote in the middle of a Data Unknown area. Set the next byte to be Data Unknown too
                            end->Kind = ViewByteKind.Data;
                            end->DataKind = ViewByteDataKind.Unknown;
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "We were told this address contains code, but we can't get a length for it; what should we do?");
                    }
                });
            }

            queue.Clear();
        }

        #region ExpandUnknownData

        protected void ExpandUnknownData()
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
                    if (pViewByte->Kind == ViewByteKind.Data && pViewByte->DataKind == ViewByteDataKind.Unknown)
                    {
                        pViewByte++;

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

        #endregion

        protected virtual void FinalizeCode()
        {
        }

        #region MarkPadding

        protected void MarkPadding()
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
                                //We will treat 0 as padding when there are at least 4 0's in a row. This means that it's not a trailing 0 and a null terminator
                                //from a UTF-16 string
                                if (pViewByte + 3 < pEnd && pBytes[1] == 0 && pBytes[2] == 0 && pBytes[3] == 0)
                                {
                                    //We're potentially willing to mark this data as padding...IF it's not also in the middle of an unknown section.
                                    //If there are other types of unknown bytes either side of this padding, it's noise to say that this is "padding", because
                                    //there isn't anything concrete that's being padded

                                    goto case 0xCC;
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
            var largeAddresses = new Dictionary<int, int>();

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
                            length = pViewByte->GetUnknownLength(pEnd);
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

#if DEBUG
        protected void ValidateNames()
        {
            Log(FileAnalyzerProgressPhase.ValidateNames);

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
                        _ = _fileAccessor.GetName(address);
                    }

                    pViewByte++;
                    address++;
                }
            }
        }

        protected void ValidateBodyReferences()
        {
            Log(FileAnalyzerProgressPhase.ValidateBodyReferences);

            //We should not have any body references preceeded by Unknown

            var sectionAccessors = _fileAccessor.SectionAccessors;

            for (var i = 0; i < sectionAccessors.Length; i++)
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                _fileAccessor.GetRawSectionData(sectionAccessor, out var pBytes, out _, out _);

                var pViewByte = sectionAccessor.pViewBytes;
                var pEnd = pViewByte + sectionAccessor.Length;

                while (pViewByte < pEnd - 1)
                {
                    if (pViewByte->Kind == ViewByteKind.Unknown && (pViewByte + 1)->Kind == ViewByteKind.Body)
                    {
                        var relativeOffset = pViewByte - sectionAccessor.pViewBytes;

                        var targetAddress = sectionAccessor.StartAddress + relativeOffset;

                        var fakeLen = pViewByte->GetLength(pViewByte + 100);

                        while (true)
                        {
                            var me = pViewByte - 1;

                            while (me->Kind != ViewByteKind.Data)
                                me--;

                            var diff = sectionAccessor.StartAddress + (me - sectionAccessor.pViewBytes);

                            var name = _fileAccessor.GetName((int) diff);
                        }
    }
}
