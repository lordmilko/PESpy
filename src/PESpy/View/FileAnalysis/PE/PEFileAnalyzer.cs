using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            IFileDisassembler? disassembler,
            LocatorHttpPolicy httpPolicy,
            IFileAnalyzerProgress? progress) : base(fileAccessor, disassembler, httpPolicy, progress)
        {
            _peFile = fileAccessor.PEFile;
            _lookupCache = new PESectionLookupCache(fileAccessor.PEFile);
        }

        protected override ViewWriter CreateViewWriter() =>
                new PEViewByteViewWriter(((PEFileAccessor) _fileAccessor).PEFile, _fileAccessor, _fileDisassembler, this);

        public override void Execute()
        {
            Log(FileAnalyzerProgressPhase.DiscoverGlobals);

            //Mark all data structures that our PEFile knows about as being data
            ((IViewable) _peFile).WriteGlobals(_viewWriter);

            //We may or may not have symbols. Collect any code locations pointed to by the PEFile
            //so we can at least disassemble something
            DiscoverCodeRoots();

            //Now try and discover symbols. Symbols can come in many forms: we can have a PDB (old, MSF or portable),
            //OMF CodeView symbols, or even COFF symbols in a CoffSymbolTable. We'll use any symbols we discover to expand
            //upon the code addresses we found in our roots, to ensure we disassemble as much as possible in the PEFile
            DiscoverSymbols((PEFileAccessor) _fileAccessor);

            //We've done all the preparations we can; work the disasm queue, discovering xrefs and tagging bytes as being code
            var importMap = GetImportMap();
            WorkDisasmQueue(importMap);

            DiscoverDirectories();

            Finalize(expandUnknownData: true);
        }

        private Dictionary<long, int> GetImportMap()
        {
            var imports = _peFile.ImportTable;

            if (imports == null)
                return new Dictionary<long, int>(); //The only module that would have no imports is ntdll; as such we don't have a cached empty dictionary

            var importMap = new Dictionary<long, int>();

            Debug.Assert(!_peFile.IsLoadedImage);

            var imageBase = _fileAccessor.ImageBase;

            for (var i = 0; i < imports.Length; i++)
            {
                ref var desc = ref imports[i];

                if (desc.FirstThunk.IsValid)
                {
                    var thunks = desc.FirstThunk.Value;

                    for (var j = 0; j < thunks.Length; j++)
                    {
                        ref var thunk = ref thunks[j];

                        //The last null function
                        if (thunk.Value == 0 || !_peFile.TryGetRVA(thunk.Offset, out var rva))
                            continue;

                        importMap[imageBase + rva] = thunk.Offset;
                    }
                }
            }

            return importMap;
        }

        #region DiscoverCodeRoots

        private void DiscoverCodeRoots()
        {
            Log(FileAnalyzerProgressPhase.DiscoverCodeRoots);

            var entryPoint = _peFile.OptionalHeader.AddressOfEntryPoint;

            if (entryPoint != 0)
            {
                if (_lookupCache.TryGetSectionInfo(entryPoint, out var targetAddress, out var sectionIndex, out _))
                    AddCode(targetAddress, entryPoint);
            }

            //I don't think we need to do anything special with imports/delay imports. All addresses associated
            //with them should have been handled when we analyzed the structs contained in the PEFile

            ProcessExports();
            ProcessExceptionTable();
            ProcessLoadConfigTable();

            ProcessEcmaMetadata();
        }

        private void ProcessExports()
        {
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
                            var info = isCode
                                ? AddCode(targetAddress, address, sectionIndex)
                                : _fileAccessor.AddData(targetAddress, sectionIndex, ViewByteDataKind.Unknown, length: 1); //We don't know how big this data item is yet, so we'll just say it's 1 byte. If we get some symbols, we might be able to do better

                            if (export.Name.Length > 0)
                                AddName(targetAddress, info, (FixedUtf8String) export.Name);
                        }
                    }
                }
            }
        }

        private void ProcessExceptionTable()
        {
            var exceptionTable = _peFile.ExceptionTable;

            if (exceptionTable != null)
            {
                //I expect every value should be in the pdata section

                ref var lookupCache = ref _lookupCache;

                foreach (var item in _peFile.ExceptionTable)
                {
                    //In debug builds, your exception table can be full of items that are all 0

                    if (item.BeginAddress != 0 && lookupCache.TryGetSectionInfo(item.BeginAddress, out var targetAddress, out int sectionIndex, out _))
                    {
                        //todo: apparently the begin address also always denotes the start of a function, based on ida
                        AddCode(targetAddress, item.BeginAddress);
                    }

                    var unwindData = item.UnwindData;

                    if (unwindData.IsValid)
                    {
                        var exceptionData = unwindData.Value.ExceptionData;

                        if (exceptionData is ScopeTable s)
                        {
                            var records = s.Records;

                            for (var j = 0; j < records.Length; j++)
                            {
                                ref var record = ref records[j];

                                if (lookupCache.TryGetSectionInfo(record.BeginAddress, out targetAddress, out sectionIndex, out _))
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
                    }
                }
            }
        }

        private void ProcessLoadConfigTable()
        {
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
            var ecmaMetadata = _peFile.EcmaMetadata;

            if (ecmaMetadata != null)
            {
                var methodDefTable = ecmaMetadata.CompressedModelHeap?.MethodDefTable;
                var stringHeap = ecmaMetadata.StringHeap;

                if (methodDefTable != null && stringHeap != null)
                {
                    ref var lookupCache = ref _lookupCache;

                    //For each method, lookup the ByteInfo of its IL Bytes and give them a name
                    foreach (var methodDef in methodDefTable)
                    {
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
                                pILViewByte = pILMethodViewByte + 1;
                                targetAddress++;
                            }
                            else
                            {
                                Debug.Assert(kind == ViewKind.ImageCorILMethodFat);

                                //The IL starts 12 bytes away
                                pILViewByte = pILMethodViewByte + 12;
                                targetAddress += 12;
                            }

                            var name = stringHeap.GetString(methodDef.Name);

                            Debug.Assert(pILViewByte->Kind == ViewByteKind.Code);

                            AddName(targetAddress, pILViewByte, (FixedUtf8String) name.Value);
                        }
                    }
                }
            }
        }

        #endregion

        protected override void FinalizeCode()
        {
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

                            if (!pViewByte->HasFlow && !pViewByte->IsFunction)
                                pViewByte->IsFunction = true;
                        }
                    }
                }
            }
        }
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

                while (pViewByte < pEnd)
                {
                    switch (pViewByte->Kind)
                    {
                        case ViewByteKind.Data:
                            switch (pViewByte->DataKind)
                            {
                                case ViewByteDataKind.Struct:
                                    var kind = _fileAccessor.GetStructKind(targetAddress);

                                    switch (kind)
                                    {
                                        case ViewKind.UnwindInfo:
                                            ReadUnwindInfo(ref pViewByte, ref targetAddress, pEnd, largeAddresses);
                                            continue;

                                        case ViewKind.ImageCorILMethodTiny:
                                        case ViewKind.ImageCorILMethodFat:
                                            ReadILMethodRegion(ref pViewByte, ref targetAddress, pEnd);
                                            continue;
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
                                length = pViewByte->GetUnknownLength(pEnd);

                            pViewByte += length;
                            targetAddress += length;
                            break;

                        case ViewByteKind.Code:
                            if (!largeAddresses.TryGetValue(targetAddress, out length))
                                length = pViewByte->GetLength(pEnd);

                            pViewByte += length;
                            targetAddress += length;
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
            }

            var writer = ((PEViewByteViewWriter) _viewWriter);
            _fileAccessor.InstallRegions(writer._topLevelRegions, writer._firstRegionByAddress);
        }

        private void ReadUnwindInfo(ref ViewByte* pViewByte, ref int targetAddress, ViewByte* pEnd, Dictionary<int, int> largeAddresses)
        {
            var length = pViewByte->GetLength(pEnd);

            var builder = new RegionBuilder
            {
                Name = "Unwind Infos",
                Kind = ViewKind.UnwindInfos,
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
                                    case ViewKind.UnwindInfo:
                                        length = pViewByte->GetLength(pEnd);
                                        break;

                                    default:
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
                            length = pViewByte->GetUnknownLength(pEnd);

                        break;

                    default:
                        throw new NotImplementedException();
                }

                pViewByte += length;
                targetAddress += length;
                builder.End += length;
            }

            var writer = (PEViewByteViewWriter) _viewWriter;

            writer._firstRegionByAddress.Add(builder);
            writer._topLevelRegions.Add(builder);
        }

        private void ReadILMethodRegion(ref ViewByte* pViewByte, ref int targetAddress, ViewByte* pEnd)
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
                                        break;

                                    default:
                                        @continue = false;
                                        continue;
                                }
                                break;

                            case ViewByteDataKind.String:
                            case ViewByteDataKind.Unknown:
                                @continue = false;
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
                        length = pViewByte->GetUnknownLength(pEnd);
                        pViewByte += length;
                        targetAddress += length;
                        builder.End += length;
                        continue;

                    default:
                        throw new NotImplementedException();
                }

                length = pViewByte->GetLength(pEnd);
                pViewByte += length;
                targetAddress += length;
                builder.End += length;
            }

            var writer = (PEViewByteViewWriter) _viewWriter;

            writer._firstRegionByAddress.Add(builder);
            writer._topLevelRegions.Add(builder);
        }

        private int ClearString(ViewByte* pViewByte, ViewByte* pEnd)
        {
            pViewByte->Kind = ViewByteKind.Unknown;

            var start = pViewByte;

            pViewByte++;

            while (pViewByte < pEnd && pViewByte->Kind == ViewByteKind.Body)
            {
                pViewByte->Kind = ViewByteKind.Unknown;
                pViewByte++;
            }

            var length = (int) (pViewByte - start);

            return length;
        }
    }
}
