using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClrDebug;

namespace PESpy.View
{
    /// <summary>
    /// Provides facilities for analyzing the contents of a <see cref="PEFile"/>.
    /// </summary>
    internal unsafe class PEFileAnalyzer : FileAnalyzer
    {
        private readonly PEFile _peFile;
        private PESectionLookupCache _lookupCache;

        internal PEFileAnalyzer(
            PEFileAccessor fileAccessor,
            in FileAnalyzerOptions options) : base(fileAccessor, options)
        {
            _peFile = fileAccessor.PEFile;
            _lookupCache = fileAccessor._lookupCache;

            if (options.ExcludeSymbols)
                fileAccessor._symbolAccessor = NullSymbolAccessor.Instance;
        }

        protected override ViewWriter CreateViewWriter()
        {
            var peFileAccessor = (PEFileAccessor) _fileAccessor;

            return new ViewByteViewWriter(
                new PEFileViewWriterHelper(peFileAccessor.PEFile, peFileAccessor.ViewMode),
                peFileAccessor.PEFile.CreateByteViewProvider(_fileAccessor),
                peFileAccessor.ViewMode,
                _fileAccessor,
                _fileDisassembler,
                this,
                _httpPolicy,
                _progress
            );
        }

        protected override void DiscoverGlobals()
        {
            Log(FileAnalyzerProgressPhase.DiscoverGlobals);

            _cancellationToken.ThrowIfCancellationRequested();

            var exceptionTable = _peFile.ExceptionTable;

            ViewByteViewWriter viewWriter = null;

            if (exceptionTable != null)
            {
                viewWriter = (ViewByteViewWriter) _viewWriter;
                viewWriter._unwindInfos = new UnwindInfoHashSet(exceptionTable.Count); //Worst case scenario
            }

            ISymbolAccessor symbolAccessor = null;

            var success = false;

            try
            {
                //Mark all data structures that our PEFile knows about as being data
                ((IViewable) _peFile).WriteGlobals(_viewWriter); //Not passing our local since I feel like that might cause a type conversion in NativeAOT? Not sure

                symbolAccessor = LocateSymbols();

                success = true;
            }
            finally
            {
                if (exceptionTable != null)
                {
                    if (success)
                    {
                        Log(FileAnalyzerProgressPhase.DiscoverExceptionData);

                        ExceptionHandlerContext.WriteUnwindInfos(_peFile, viewWriter, symbolAccessor);

                        ProcessSpecialUnwindInfos(viewWriter, exceptionTable);
                    }

                    viewWriter._unwindInfos.Dispose();
                }
            }
        }

        public override void Execute() => ExecuteCode();

        //Maps the ImageBase + RVA of each import to the target address of that
        //import. Used by the disassembler for the purpose of collecting xrefs that reference
        //the import address table
        protected override Dictionary<long, int> GetImportMap()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            //The only module that would have no imports is ntdll; as such we don't have a cached empty dictionary
            //for the scenario in which there are no imports
            var imports = _peFile.ImportTable;

            var importMap = new Dictionary<long, int>();

            var wantVirtual = ((PEFileAccessor) _fileAccessor).ViewMode == ViewMode.Virtual;
            var imageBase = _fileAccessor.ImageBase;

            if (imports != null)
            {
                for (var i = 0; i < imports.Length; i++)
                {
                    ref var desc = ref imports[i];

                    if (desc.FirstThunk.IsValid)
                    {
                        var thunks = desc.FirstThunk.Value;

                        foreach (var thunk in thunks)
                        {
                            //The last null function
                            if (thunk.Value == 0 || !_peFile.TryGetRVA((int) thunk.Offset, out var rva))
                                continue;

                            importMap[imageBase + rva] = wantVirtual ? rva : (int) thunk.Offset;
                        }
                    }
                }
            }

            var delayImportTable = _peFile.DelayImportTable;

            if (delayImportTable != null)
            {
                for (var i = 0; i < delayImportTable.Length; i++)
                {
                    ref var desc = ref delayImportTable[i];

                    if (desc.ImportAddressTableRVA.IsValid)
                    {
                        var thunks = desc.ImportAddressTableRVA.Value;

                        foreach (var thunk in thunks)
                        {
                            //The last null function
                            if (thunk.Value == 0 || !_peFile.TryGetRVA((int) thunk.Offset, out var rva))
                                continue;

                            importMap[imageBase + rva] = wantVirtual ? rva : (int) thunk.Offset;
                        }
                    }
                }
            }

            return importMap;
        }

        private void ProcessSpecialUnwindInfos(ViewByteViewWriter viewWriter, RuntimeFunctionList exceptionTable)
        {
            var specialUnwindInfos = viewWriter._specialUnwindInfos;

            if (specialUnwindInfos.Count == 0)
                return;

            var pRuntimeFunction = (RUNTIME_FUNCTION*) exceptionTable.chunk.Pointer;

            var pEnd = pRuntimeFunction + exceptionTable.Count;

            var lookupCache = _lookupCache;

            if (!_peFile.TryGetSectionContainingRVA(specialUnwindInfos.First().Key, out var sectionIndex, out var sectionHeader))
            {
                Debug.Assert(false);
                return;
            }

            //We assume that all UNWIND_INFO items are in the same section

            var block = _peFile.GetSectionBlock(sectionIndex, sectionHeader);

            var vaStart = sectionHeader.VirtualAddress;
            var vaEnd = vaStart + sectionHeader.VirtualSize;

            //This works for both loaded and unloaded cases
            var pData = block.LocalPointer - vaStart;

#if DEBUG
            viewWriter.EnterUniqueXRef();
#endif

            for (; pRuntimeFunction < pEnd; pRuntimeFunction++)
            {
                var unwindInfoRVA = pRuntimeFunction->UnwindData;

                if (unwindInfoRVA == 0)
                    continue;

                //In debug builds, your exception table can be full of items that are all 0

                if (lookupCache.TryGetSectionInfo(pRuntimeFunction->BeginAddress, out var targetAddress, out sectionIndex, out _))
                {
                    //todo: apparently the begin address also always denotes the start of a function, based on ida
                    AddCode(targetAddress, pRuntimeFunction->BeginAddress);
                }

                if (!specialUnwindInfos.TryGetValue(unwindInfoRVA, out var dataKind))
                    continue;

                if (unwindInfoRVA < vaStart || unwindInfoRVA >= vaEnd)
                {
                    //A critical assumption we've made has failed. We don't currently implement fallback logic.
                    //Skip this item for now
                    Debug.Assert(false);
                    continue;
                }

                var pUnwindInfo = pData + unwindInfoRVA;

                //This is an RVA to an UNWIND_INFO whose ExceptionData starts with an RVA<FuncInfo4>

                var countOfCodes = *(pUnwindInfo + UnwindInfo.CountOfCodesOffset);
                var extraDataStart = 4 + (((countOfCodes + 1) & ~1) * 2); //4 fixed bytes + CountOfCodes aligned to an even number
                var exceptionHandler = *(int*) (pUnwindInfo + extraDataStart);

                var exceptionDataStart = (pUnwindInfo + extraDataStart + sizeof(int));

                MemoryChunk valueChunk;

                switch (dataKind)
                {
                    case UnwindInfo.SpecialExceptionDataKind.ScopeTable:
                        valueChunk = new MemoryChunk(block, (int) (exceptionDataStart - block.LocalPointer));
                        var scopeTable = new ScopeTable(valueChunk);
                        DiscoverScopeTable(ref lookupCache, scopeTable);
                        break;

                    case UnwindInfo.SpecialExceptionDataKind.FuncInfo:
                        var rvaOfFuncInfo = *(int*) exceptionDataStart;

                        if (_peFile.TryGetValueChunkFromSection(rvaOfFuncInfo, out valueChunk))
                        {
                            var funcInfo = new FuncInfo(valueChunk);
                            DiscoverFuncInfoCodeRoots(funcInfo);
                        }
                        break;

                    case UnwindInfo.SpecialExceptionDataKind.FuncInfo4:
                        //The xref for the RVA to the FuncInfo4 is written in UnwindInfo.WriteUnwindInfo because we already have
                        //the UnwindInfo struct offset at that point
                        var rvaOfFuncInfo4 = *(int*) exceptionDataStart;

                        if (_peFile.TryGetValueChunkFromSection(rvaOfFuncInfo4, out valueChunk))
                        {
                            var funcInfo4 = new FuncInfo4(valueChunk, pRuntimeFunction->BeginAddress);
                            viewWriter.WriteGlobal(funcInfo4);

                            //While we're here, let's also discover code roots
                            DiscoverFuncInfo4CodeRoots(ref lookupCache, funcInfo4);
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

#if DEBUG
            viewWriter.ExitUniqueXRef();
#endif
        }

        #region DiscoverCodeRoots

        protected override void DiscoverCodeRoots()
        {
            Log(FileAnalyzerProgressPhase.DiscoverCodeRoots);

            _cancellationToken.ThrowIfCancellationRequested();

            var entryPoint = _peFile.OptionalHeader.AddressOfEntryPoint;

            if (entryPoint != 0)
            {
                if (_lookupCache.TryGetSectionInfo(entryPoint, out var targetAddress, out var sectionIndex, out _))
                    AddCode(targetAddress, entryPoint);
            }

            //I don't think we need to do anything special with imports/delay imports. All addresses associated
            //with them should have been handled when we analyzed the structs contained in the PEFile

            ProcessExports();
            ProcessLoadConfigTable();

            ProcessEcmaMetadata();
        }

        private void ProcessExports()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var exports = _peFile.ExportTable?.Exports;

            if (exports != null)
            {
                ref var lookupCache = ref _lookupCache;

                for (var i = 0; i < exports.Length; i++)
                {
                    ref var export = ref exports[i];

                    var forwardOrAddress = export.ForwardOrAddress;

                    //Forwarder strings will have already been written as data while writing globals; all we need to handle here
                    //is collecting the RVAs of the code pointed to by the non-forwarder strings
                    if (!forwardOrAddress.IsForward)
                    {
                        var address = forwardOrAddress.Address;

                        if (lookupCache.TryGetSectionInfo(address, out var targetAddress, out var sectionIndex, out var isCode))
                        {
                            if (isCode)
                            {
                                var pViewByte = AddCode(targetAddress, address, sectionIndex);

                                if (export.Name.Length > 0)
                                {
                                    _fileAccessor.CheckName(targetAddress);
                                    pViewByte->HasName = true;
                                }
                            }
                            else
                            {
                                //We don't know how big this data item is yet, so we'll just say it's 1 byte. If we get some symbols, we might be able to do better
                                if (_fileAccessor.TryAddData(targetAddress, sectionIndex, ViewByteDataKind.Unknown, length: 1, out var pViewByte))
                                {
                                    if (export.Name.Length > 0)
                                    {
                                        _fileAccessor.CheckName(targetAddress);
                                        pViewByte->HasName = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DiscoverScopeTable(
            ref PESectionLookupCache lookupCache,
            in ScopeTable scopeTable)
        {
            foreach (var record in scopeTable)
            {
                if (lookupCache.TryGetSectionInfo(record.BeginAddress, out var targetAddress, out var sectionIndex, out _))
                {
                    //End is normally the same as jump target, but not always
                    AddCode(targetAddress, record.BeginAddress);
                }

                if (lookupCache.TryGetSectionInfo(record.EndAddress, out targetAddress, out sectionIndex, out _))
                    AddCode(targetAddress, record.EndAddress);

                /* If no custom handler has been specified, this value is EXCEPTION_EXECUTE_HANDLER (1).
                 * Ostensibly, it should also be possible for this value to be EXCEPTION_CONTINUE_SEARCH (0)
                 * and EXCEPTION_CONTINUE_EXECUTION (-1) */
                if (record.HandlerAddress > 1)
                {
                    if (lookupCache.TryGetSectionInfo(record.HandlerAddress, out targetAddress, out sectionIndex, out _))
                        AddCode(targetAddress, record.HandlerAddress);
                }

                if (lookupCache.TryGetSectionInfo(record.JumpTarget, out targetAddress, out sectionIndex, out _))
                    AddCode(targetAddress, record.JumpTarget);
            }
        }

        private void DiscoverFuncInfoCodeRoots(in FuncInfo funcInfo)
        {
            ref var lookupCache = ref _lookupCache;

            var dispUnwindMap = funcInfo.dispUnwindMap.ValueOrDefault;

            if (dispUnwindMap != null)
            {
                for (var i = 0; i < dispUnwindMap.Length; i++)
                {
                    ref var unwindMapEntry = ref dispUnwindMap[i];

                    if (lookupCache.TryGetSectionInfo(unwindMapEntry.action, out var targetAddress, out int sectionIndex, out _))
                        AddCode(targetAddress, unwindMapEntry.action);
                }
            }

            var dispTryBlockMap = funcInfo.dispTryBlockMap.ValueOrDefault;

            if (dispTryBlockMap != null)
            {
                for (var i = 0; i < dispTryBlockMap.Length; i++)
                {
                    ref var tryBlockMapEntry = ref dispTryBlockMap[i];

                    var dispHandlerArray = tryBlockMapEntry.dispHandlerArray.ValueOrDefault;

                    if (dispHandlerArray != null)
                    {
                        for (var j = 0; j < dispHandlerArray.Length; j++)
                        {
                            ref var handlerType = ref dispHandlerArray[j];

                            if (lookupCache.TryGetSectionInfo(handlerType.dispOfHandler, out var targetAddress, out int sectionIndex, out _))
                                AddCode(targetAddress, handlerType.dispOfHandler);
                        }
                    }
                }
            }

            var dispIPtoStateMap = funcInfo.dispIPtoStateMap.ValueOrDefault;

            if (dispIPtoStateMap != null)
            {
                for (var i = 0; i < dispIPtoStateMap.Length; i++)
                {
                    ref var ipToStateMapEntry = ref dispIPtoStateMap[i];

                    if (lookupCache.TryGetSectionInfo(ipToStateMapEntry.Ip, out var targetAddress, out int sectionIndex, out _))
                        AddCode(targetAddress, ipToStateMapEntry.Ip);
                }
            }
        }

        private void DiscoverFuncInfo4CodeRoots(ref PESectionLookupCache lookupCache, in FuncInfo4 funcInfo4)
        {
            var maybeDispUnwindMap = funcInfo4.dispUnwindMap;

            if (maybeDispUnwindMap.IsValid)
            {
                var dispUnwindMap = maybeDispUnwindMap.Value;

                foreach (var unwindMapEntry in dispUnwindMap)
                {
                    if (unwindMapEntry.action != 0)
                    {
                        if (lookupCache.TryGetSectionInfo(unwindMapEntry.action, out var targetAddress, out var sectionIndex, out _))
                            AddCode(targetAddress, unwindMapEntry.action);
                    }
                }
            }

            var maybeDispTryBlockMap = funcInfo4.dispTryBlockMap;

            if (maybeDispTryBlockMap.IsValid)
            {
                var dispTryBlockMap = maybeDispTryBlockMap.Value;

                foreach (var tryBlockMapEntry in dispTryBlockMap)
                {
                    var maybeDispHandlerArray = tryBlockMapEntry.dispHandlerArray;

                    if (maybeDispHandlerArray.IsValid)
                    {
                        var dispHandlerArray = tryBlockMapEntry.dispHandlerArray.Value;

                        foreach (var handlerType in dispHandlerArray)
                        {
                            if (lookupCache.TryGetSectionInfo(handlerType.dispOfHandler, out var targetAddress, out var sectionIndex, out _))
                                AddCode(targetAddress, handlerType.dispOfHandler);

                            foreach (var continuationAddress in handlerType.continuationAddresses)
                            {
                                if (lookupCache.TryGetSectionInfo(continuationAddress, out targetAddress, out sectionIndex, out _))
                                    AddCode(targetAddress, continuationAddress);
                            }
                        }
                    }
                }
            }

            if (funcInfo4.header.isSeparated)
            {
                var maybeDispToSegMap = funcInfo4.dispToSegMap;

                if (maybeDispToSegMap.IsValid)
                {
                    var dispToSegMap = maybeDispToSegMap.Value;

                    foreach (var segMapEntry in dispToSegMap)
                    {
                        if (lookupCache.TryGetSectionInfo(segMapEntry.addrStartRVA, out var targetAddress, out var sectionIndex, out _))
                            AddCode(targetAddress, segMapEntry.addrStartRVA);

                        var maybeDispIPtoStateMap = funcInfo4.dispIPtoStateMap;

                        if (maybeDispIPtoStateMap.IsValid)
                        {
                            var dispIPtoStateMap = maybeDispIPtoStateMap.Value;

                            foreach (var stateMapEntry in dispIPtoStateMap)
                            {
                                if (lookupCache.TryGetSectionInfo(stateMapEntry.Ip, out targetAddress, out sectionIndex, out _))
                                    AddCode(targetAddress, stateMapEntry.Ip);
                            }
                        }
                    }
                }
            }
            else
            {
                var maybeDispIPtoStateMap = funcInfo4.dispIPtoStateMap;

                if (maybeDispIPtoStateMap.IsValid)
                {
                    var dispIPtoStateMap = maybeDispIPtoStateMap.Value;

                    foreach (var stateMapEntry in dispIPtoStateMap)
                    {
                        if (lookupCache.TryGetSectionInfo(stateMapEntry.Ip, out var targetAddress, out var sectionIndex, out _))
                            AddCode(targetAddress, stateMapEntry.Ip);
                    }
                }
            }
        }

        private void ProcessLoadConfigTable()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var loadConfigTable = _peFile.LoadConfigTable;

            ref var lookupCache = ref _lookupCache;

            if (loadConfigTable != null)
            {
                //LoadPrefixTable?

                //SEHandlerTable is available for x86 only, which means it's safe to convert each VA to an int
                var seHandlerTable = loadConfigTable.SEHandlerTable;

                if (seHandlerTable.IsValid)
                {
                    var addresses = seHandlerTable.Value;

                    foreach (var address in addresses)
                    {
                        if (lookupCache.TryGetSectionInfo((int) address, out var targetAddress, out var sectionIndex, out _))
                            AddCode(targetAddress, address);
                    }
                }

                //GFIDS

                var gfids = loadConfigTable.GuardCFFunctionTable;

                if (gfids.IsValid)
                {
                    var entries = gfids.Value;

                    foreach (var entry in entries)
                    {
                        if (lookupCache.TryGetSectionInfo(entry.Function, out var targetAddress, out var sectionIndex, out _))
                            AddCode(targetAddress, entry.Function);
                    }
                }

                //IAT

                //Each entry describes the RVA of an item in the IAT. If we're not a loaded image, I guess you would convert each RVA
                //to an offset, and then find the entry that corresponds to that offset via a binary search. But the bottom line here
                //is that these

                //todo: these can also point to delay imports

                var guardIATTable = loadConfigTable.GuardAddressTakenIatEntryTable;

                if (guardIATTable.IsValid)
                {
                    var importTable = _peFile.ImportTable;

                    if (importTable != null)
                    {
                        var entries = guardIATTable.Value;

                        for (var i = 0; i < entries.Count; i++)
                        {
                            var entry = entries[i];

                            if (entry.TryGetImportInfo(out var descriptorIndex, out var thunkInfo, out var isDelayImport))
                            {
                                //todo: add an xref
                            }
                        }
                    }
                }

                //Long Jump

                var guardJmpTable = loadConfigTable.GuardLongJumpTargetTable;

                if (guardJmpTable.IsValid)
                {
                    var entries = guardJmpTable.Value;

                    for (var i = 0; i < entries.Count; i++)
                    {
                        var entry = entries[i];

                        if (lookupCache.TryGetSectionInfo(entry.Target, out var targetAddress, out var sectionIndex, out _))
                            AddCode(targetAddress, entry.Target);
                    }
                }

                //EHCont

                var guardEHContTable = loadConfigTable.GuardEHContinuationTable;

                if (guardEHContTable.IsValid)
                {
                    var entries = guardEHContTable.Value;

                    for (var i = 0; i < entries.Count; i++)
                    {
                        var entry = entries[i];

                        if (lookupCache.TryGetSectionInfo(entry.Function, out var targetAddress, out var sectionIndex, out _))
                            AddCode(targetAddress, entry.Function);
                    }
                }
            }
        }

        private void ProcessEcmaMetadata()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var ecmaMetadata = _peFile.EcmaMetadata;

            if (ecmaMetadata != null)
            {
                var methodDefTable = ecmaMetadata.CompressedModelHeap?.MethodDefTable;
                var stringHeap = ecmaMetadata.StringHeap;

                if (methodDefTable != null && stringHeap != null)
                {
                    ref var lookupCache = ref _lookupCache;

                    var peFileAccessor = (PEFileAccessor) _fileAccessor;

                    var i = -1; //The elementIndex we use to index into the MethodTable needs to be 0-based

                    //For each method, lookup the ByteInfo of its IL Bytes and give them a name
                    foreach (var methodDef in methodDefTable)
                    {
                        i++;

                        //You can have P/Invokes that say they have RVAs but these don't point to valid data.
                        //Conversely, if a given method's implementation is native, there won't be an associated Cor IL Method for it

                        var rva = methodDef.RVA;

                        if (rva == 0 || (methodDef.ImplFlags & CorMethodImpl.miNative) != 0)
                            continue;

                        if (lookupCache.TryGetSectionInfo(rva, out var targetAddress, out var sectionIndex, out _))
                        {
                            //The RVA points to the fat or thin IL method
                            var pILMethodViewByte = _fileAccessor.GetViewByteForSection(targetAddress, sectionIndex);

                            var kind = _fileAccessor.GetStructKind(targetAddress);

                            ViewByte* pILViewByte;

                            if (kind == ViewKind.ImageCorILMethodTiny)
                            {
                                //The IL starts on the next byte
                                pILViewByte = pILMethodViewByte + ImageCorILMethod.TinyStructSize;
                                targetAddress += ImageCorILMethod.TinyStructSize;
                                rva += ImageCorILMethod.TinyStructSize;
                            }
                            else
                            {
                                Debug.Assert(kind == ViewKind.ImageCorILMethodFat);

                                //The IL starts 12 bytes away
                                pILViewByte = pILMethodViewByte + ImageCorILMethod.FatStructSize;
                                targetAddress += ImageCorILMethod.FatStructSize;
                                rva += ImageCorILMethod.FatStructSize;
                            }

                            var name = stringHeap.GetString(methodDef.Name);

                            Debug.Assert(pILViewByte->Kind == ViewByteKind.Code);

                            _fileAccessor.CheckName(targetAddress);
                            pILViewByte->HasName = true;
                            peFileAccessor._rvaToMethodDefMap[rva] = i;
                        }
                    }
                }
            }
        }

        #endregion

        protected override void FinalizeCode()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            //Iterate through all exports again and mark all exports that point to code and don't have more code behind them
            //as functions. You can have exports that point in the middle of a function, so we don't want to mark those as functions

            var exports = _peFile.ExportTable?.Exports;

            if (exports != null)
            {
                ref var lookupCache = ref _lookupCache;

                for (var i = 0; i < exports.Length; i++)
                {
                    ref var export = ref exports[i];

                    var forwardOrAddress = export.ForwardOrAddress;

                    //Forwarder strings will have already been written as data while writing globals; all we need to handle here
                    //is collecting the RVAs of the code pointed to by the non-forwarder strings
                    if (!forwardOrAddress.IsForward)
                    {
                        var address = forwardOrAddress.Address;

                        if (lookupCache.TryGetSectionInfo(address, out var targetAddress, out var sectionIndex, out _))
                        {
                            var pViewByte = _fileAccessor.GetViewByteForSection(targetAddress, sectionIndex);

                            if (!pViewByte->HasFlow && pViewByte->Kind != ViewByteKind.Data && !pViewByte->IsFunction)
                            {
                                pViewByte->IsFunction = true;

                                //Our expectation is that ExpandUnknownCode should get the size of each symbol, and mark the relevant
                                //code regions as code. But if we failed to get the length, we're not going to do anything! That's a bit
                                //of an issue
                                Debug.Assert(pViewByte->Kind == ViewByteKind.Code);
                            }
                        }
                    }
                }
            }
        }

        protected override void MarkRegions()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            //PDB files can tell us that a given area of the PE file is dedicated to storing thunks
            MarkThunksRegion();
            CreateOMFRegion(_peFile.DebugTable);

            //Mark regions containing repeated or related sequences of data

            var sectionAccessors = _fileAccessor.SectionAccessors;

            for (var i = 0; i < sectionAccessors.Length; i++)
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                var pViewByte = sectionAccessor.pViewBytes;
                var pSectionStart = pViewByte;
                var sectionAddress = sectionAccessor.StartAddress;
                var pEnd = pViewByte + sectionAccessor.Length;

                var targetAddress = sectionAddress;

                var largeAddresses = _fileAccessor.LargeAddresses;

                int length;

                ViewKind kind;

                while (pViewByte < pEnd)
                {
                    switch (pViewByte->Kind)
                    {
                        case ViewByteKind.Data:
                            switch (pViewByte->DataKind)
                            {
                                case ViewByteDataKind.Struct:
                                    kind = _fileAccessor.GetStructKind(targetAddress);

                                    switch (kind)
                                    {
                                        //UNWIND_INFO + data is located in the .text section in .NET executables and creates a lot of noise.
                                        //As such, we'll say if we see a sequence of UNWIND_INFO + random bytes, we'll group this up into
                                        //an Unwind Info Region
                                        case ViewKind.UnwindInfo:
                                            MarkUnwindInfoRegions(ref pViewByte, ref targetAddress, pEnd, largeAddresses);
                                            continue;

                                        //IL Methods appear to be listed all in a row. Starting with the first ImageCorILMethodFat/Tiny,
                                        //group the IL Method, IL, and any padding (0xCC only) up into a single region
                                        case ViewKind.ImageCorILMethodTiny:
                                        case ViewKind.ImageCorILMethodFat:
                                            MarkILMethodRegion(ref pViewByte, ref targetAddress, pEnd);
                                            continue;

                                        case ViewKind.ImageImportByName:
                                            MarkImportByNameRegion(ref pViewByte, ref targetAddress, pEnd, largeAddresses);
                                            continue;

                                        default:
                                            if (kind >= ViewKind.SymType && kind < ViewKind.LfAlias)
                                            {
                                                MarkSymbols(ref pViewByte, ref targetAddress, pEnd);
                                                continue;
                                            }
                                            else if (kind >= ViewKind.LfAlias && kind < ViewKind.GSIHashHdr)
                                            {
                                                MarkTypes(ref pViewByte, ref targetAddress, pEnd);
                                                continue;
                                            }
                                            break;
                                    }

                                    break;

                                case ViewByteDataKind.String:
                                    MarkStringRegions(ref pViewByte, ref targetAddress, pEnd, largeAddresses);
                                    continue;

                                case ViewByteDataKind.Padding:
                                    //We may be at the padding at a start of a series of functions. If we're not,
                                    //we won't create a region
                                    MarkFunctionRegions(ref pViewByte, ref targetAddress, pEnd, largeAddresses);
                                    continue;

                                case ViewByteDataKind.Integer:
                                    if (_fileAccessor.TryGetStructKind(targetAddress, out kind))
                                    {
                                        switch (kind)
                                        {
                                            case ViewKind.XFG:
                                                MarkFunctionRegions(ref pViewByte, ref targetAddress, pEnd, largeAddresses);
                                                continue;
                                        }
                                    }
                                    break;
                            }

                            if (!largeAddresses.TryGetValue(targetAddress, out length))
                                length = pViewByte->GetLength(pEnd);

                            pViewByte += length;
                            targetAddress += length;
                            break;

                        case ViewByteKind.Unknown:
                            if (!largeAddresses.TryGetValue(targetAddress, out length))
                                length = pViewByte->GetLength(pEnd); //We should already have unknown bodies at this point

                            pViewByte += length;
                            targetAddress += length;
                            break;

                        case ViewByteKind.Code:
                            MarkFunctionRegions(ref pViewByte, ref targetAddress, pEnd, largeAddresses);
                            break;

                        default:
                            //We should not be encountering Body. If we do, that's a bug
                            throw new InvalidOperationException($"Encountered unexpected {nameof(ViewByte)} '{pViewByte->Kind}'");
                    }
                }
            }
        }

        private void MarkThunksRegion()
        {
            //If we're backed by a PDB, get the area that contains thunks
            var symbolAccessor = _fileAccessor.GetSymbolAccessor();

            if (symbolAccessor is ExternalFileSymbolAccessor e)
                symbolAccessor = e.GetUnderlyingSymbolAccessorUnsafe();

            if (symbolAccessor is PDBFileSymbolAccessor p)
            {
                var hdr = p.PDBFile.PSGSI.PSGsiHdr;

                if (hdr.nThunks > 0)
                {
                    var sectionHeaders = _peFile.SectionHeaders;

                    if (hdr.isectThunkTable <= sectionHeaders.Length)
                    {
                        ref var sectionHeader = ref sectionHeaders[hdr.isectThunkTable - 1];

                        int thunkRegionStart;

                        if (((PEFileAccessor) _fileAccessor).ViewMode == ViewMode.Virtual)
                            thunkRegionStart = sectionHeader.VirtualAddress + hdr.offThunkTable;
                        else
                            thunkRegionStart = sectionHeader.PointerToRawData + hdr.offThunkTable;

                        var thunkRegionEnd = thunkRegionStart + (hdr.nThunks * hdr.cbSizeOfThunk);

                        SplitRegionBounds(thunkRegionStart, thunkRegionEnd);

                        var builder = new RegionBuilder
                        {
                            Name = "Thunks",
                            Kind = ViewKind.Thunks,
                            Start = thunkRegionStart,
                            End = thunkRegionEnd
                        };

                        _extraRegions.Add(builder);
                    }
                }
            }
        }

        private void MarkUnwindInfoRegions(ref ViewByte* pViewByte, ref long targetAddress, ViewByte* pEnd, Dictionary<long, int> largeAddresses)
        {
            var length = pViewByte->GetLength(pEnd);

            var builder = new RegionBuilder
            {
                Name = "Unwind Infos",
                Kind = ViewKind.UnwindInfos,
                Start = targetAddress,
                End = targetAddress + length
            };

            //Cut the end to the next data directory. Even if that data directory contains unknown info, the Unwind Infos region
            //shouldn't extend on top of that

            var topLevelDirectories = _fileAccessor.TopLevelDirectories;

            for (var i = 0; i < topLevelDirectories.Length; i++)
            {
                ref var item = ref topLevelDirectories[i];

                if (item.Start > targetAddress)
                {
                    var distance = item.Start - targetAddress;
                    var directoryStart = pViewByte + distance;

                    if (directoryStart < pEnd)
                        pEnd = directoryStart;

                    break;
                }
            }

            pViewByte += length;
            targetAddress += length;

            var fileAccessor = _fileAccessor;

            Debug.Assert(_hasUnknownBodies);

            var @continue = true;

            while (pViewByte < pEnd && @continue)
            {
                switch (pViewByte->Kind)
                {
                    case ViewByteKind.Data:
                        switch (pViewByte->DataKind)
                        {
                            case ViewByteDataKind.Struct:
                                var kind = fileAccessor.GetStructKind(targetAddress);

                                if (kind >= ViewKind.UnwindInfo && kind < ViewKind.WinCertificate)
                                    length = pViewByte->GetLength(pEnd);
                                else
                                {
                                    @continue = false;
                                    continue;
                                }

                                break;

                            case ViewByteDataKind.String:
                                //This shouldn't be a string then
                                length = ClearString(pViewByte, pEnd);
                                break;

                            case ViewByteDataKind.Padding:
                                if (!largeAddresses.TryGetValue(targetAddress, out length))
                                    length = pViewByte->GetLength(pEnd);
                                break;

                            default:
                                @continue = false;
                                continue;
                        }
                        break;

                    case ViewByteKind.Unknown:
                        if (!largeAddresses.TryGetValue(targetAddress, out length))
                            length = pViewByte->GetLength(pEnd); //We should already have unknown bodies at this point

                        break;

                    default:
                        throw new NotImplementedException();
                }

                pViewByte += length;
                targetAddress += length;
                builder.End += length;
            }

            _extraRegions.Add(builder);
        }

        private void MarkILMethodRegion(ref ViewByte* pViewByte, ref long targetAddress, ViewByte* pEnd)
        {
            var length = pViewByte->GetLength(pEnd);

            var builder = new RegionBuilder
            {
                Name = "IL Methods",
                Kind = ViewKind.ILMethods,
                Start = targetAddress,
                End = targetAddress + length
            };

            pViewByte += length;
            targetAddress += length;

            var fileAccessor = _fileAccessor;

            var @continue = true;

            while (pViewByte < pEnd && @continue)
            {
                switch (pViewByte->Kind)
                {
                    case ViewByteKind.Data:
                        switch (pViewByte->DataKind)
                        {
                            case ViewByteDataKind.Struct:
                                var kind = fileAccessor.GetStructKind(targetAddress);

                                switch (kind)
                                {
                                    case ViewKind.ImageCorILMethodTiny:
                                    case ViewKind.ImageCorILMethodFat:
                                    case ViewKind.ImageCorILMethodSectEHFat:
                                    case ViewKind.ImageCorILMethodSectEHSmall:
                                        //IL is not data, and is handled below
                                        break;

                                    //I would expect that these should be contained _within_ the ImageCorILMethodSectEH
                                    case ViewKind.ImageCorILMethodSectFat:
                                    case ViewKind.ImageCorILMethodSectSmall:
                                    case ViewKind.ImageCorILMethodSectEHClauseFat:
                                    case ViewKind.ImageCorILMethodSectEHClauseSmall:
                                        Debug.Assert(false);
                                        break;

                                    default:
                                        @continue = false;
                                        continue;
                                }
                                break;

                            case ViewByteDataKind.String:
                            case ViewByteDataKind.Unknown:
                                //I initially thought I could just assume that what we encounter next
                                //must be a junk string, and so we should clear it...however I found that
                                //actually that's not true, so we need to stop
                                @continue = false;
                                continue;

                            case ViewByteDataKind.Padding:
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case ViewByteKind.Code:
                        if (!pViewByte->IsIL)
                        {
                            @continue = false;
                            continue;
                        }
                        
                        break;

                    case ViewByteKind.Unknown:
                        //IL Methods often have random bytes between them; I haven't figured out if this is just junk or may mean anything,
                        //but for now to keep things tidy let's just bundle it up along with the IL Methods
                        length = pViewByte->GetLength(pEnd); //We should already have unknown bodies at this point
                        pViewByte += length;
                        targetAddress += length;
                        continue;

                    default:
                        throw new NotImplementedException();
                }

                length = pViewByte->GetLength(pEnd);
                pViewByte += length;
                Debug.Assert(pViewByte->Kind != ViewByteKind.Body);
                targetAddress += length;
            }

            builder.End = targetAddress;

            _extraRegions.Add(builder);
        }

        private void MarkFunctionRegions(ref ViewByte* pViewByte, ref long targetAddress, ViewByte* pEnd, Dictionary<long, int> largeAddresses)
        {
            var builder = new RegionBuilder
            {
                Name = "Functions",
                Kind = ViewKind.Functions,
                Start = targetAddress,
            };

            var @continue = true;

            int length;

            var numFunctions = 0;

            while (pViewByte < pEnd && @continue)
            {
                switch (pViewByte->Kind)
                {
                    case ViewByteKind.Code:
                        numFunctions++;

                        if (!largeAddresses.TryGetValue(targetAddress, out length))
                            length = pViewByte->GetLength(pEnd);

                        pViewByte += length;
                        targetAddress += length;
                        break;

                    case ViewByteKind.Data:
                        switch (pViewByte->DataKind)
                        {
                            //Don't include Data Unknown here; the implication is it's known to be data
                            case ViewByteDataKind.Padding:
                                if (!largeAddresses.TryGetValue(targetAddress, out length))
                                    length = pViewByte->GetLength(pEnd);

                                pViewByte += length;
                                targetAddress += length;
                                break;

                            case ViewByteDataKind.Integer:
                                if (_fileAccessor.TryGetStructKind(targetAddress, out var kind))
                                {
                                    switch (kind)
                                    {
                                        case ViewKind.XFG:
                                            if (!largeAddresses.TryGetValue(targetAddress, out length))
                                                length = pViewByte->GetLength(pEnd);

                                            pViewByte += length;
                                            targetAddress += length;
                                            break;

                                        default:
                                            @continue = false;
                                            break;
                                    }
                                }
                                else
                                    @continue = false;

                                break;

                            default:
                                @continue = false;
                                break;
                        }
                        break;

                    case ViewByteKind.Unknown:
                        if (!largeAddresses.TryGetValue(targetAddress, out length))
                            length = pViewByte->GetLength(pEnd);

                        pViewByte += length;
                        targetAddress += length;
                        break;

                    default:
                        @continue = false;
                        continue;
                }
            }

            if (numFunctions > 1)
            {
                builder.End = targetAddress;

                var writer = (ViewByteViewWriter) _viewWriter;

                _extraRegions.Add(builder);
            }
        }

        private void MarkImportByNameRegion(ref ViewByte* pViewByte, ref long targetAddress, ViewByte* pEnd, Dictionary<long, int> largeAddresses)
        {
            var builder = new RegionBuilder
            {
                Name = "Import Strings",
                Kind = ViewKind.ImportStrings,
                Start = targetAddress,
            };

            var fileAccessor = _fileAccessor;

            var @continue = true;

            int length;

            var numStrings = 0;

            while (pViewByte < pEnd && @continue)
            {
                switch (pViewByte->Kind)
                {
                    case ViewByteKind.Data:
                        switch (pViewByte->DataKind)
                        {
                            case ViewByteDataKind.Struct:
                                var kind = fileAccessor.GetStructKind(targetAddress);

                                if (kind != ViewKind.ImageImportByName)
                                {
                                    @continue = false;
                                    continue;
                                }

                                numStrings++;

                                length = pViewByte->GetLength(pEnd);
                                pViewByte += length;
                                targetAddress += length;
                                break;

                            case ViewByteDataKind.Padding:
                                if (!largeAddresses.TryGetValue(targetAddress, out length))
                                    length = pViewByte->GetLength(pEnd);

                                pViewByte += length;
                                targetAddress += length;
                                break;

                            default:
                                @continue = false;
                                continue;
                        }
                        break;

                    default:
                        @continue = false;
                        continue;
                }
            }

            if (numStrings > 1)
            {
                builder.End = targetAddress;

                _extraRegions.Add(builder);
            }
        }

        private void MarkStringRegions(ref ViewByte* pViewByte, ref long targetAddress, ViewByte* pEnd, Dictionary<long, int> largeAddresses)
        {
            var builder = new RegionBuilder
            {
                Name = "Strings",
                Kind = ViewKind.Strings,
                Start = targetAddress,
            };

            var @continue = true;

            int length;

            var numStrings = 0;

            while (pViewByte < pEnd && @continue)
            {
                switch (pViewByte->Kind)
                {
                    case ViewByteKind.Data:
                        switch (pViewByte->DataKind)
                        {
                            case ViewByteDataKind.String:
                                numStrings++;

                                length = pViewByte->GetLength(pEnd);

                                pViewByte += length;
                                targetAddress += length;
                                break;

                            case ViewByteDataKind.Padding:
                                //Strings are commonly interspersed with padding, so include padding in the area
                                if (!largeAddresses.TryGetValue(targetAddress, out length))
                                    length = pViewByte->GetLength(pEnd);

                                pViewByte += length;
                                targetAddress += length;
                                break;

                            default:
                                @continue = false;
                                continue;
                        }
                        break;

                    default:
                        @continue = false;
                        continue;
                }
            }

            if (numStrings > 1)
            {
                builder.End = targetAddress;

                _extraRegions.Add(builder);
            }
        }

        protected override void MarkNestedFiles()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var rawRanges = ((ViewByteViewWriter) _viewWriter)._nestedFileRanges;

            for (var i = 0; i < rawRanges.Count; i++)
            {
                var range = rawRanges[i];

                SplitRegionBounds(range.start, range.end);
            }

            ((PEFileAccessor) _fileAccessor).InstallNestedFileRanges((ViewByteViewWriter) _viewWriter);
        }
    }
}
