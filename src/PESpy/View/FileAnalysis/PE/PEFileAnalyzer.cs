using System;
﻿using System.Diagnostics;
using ClrDebug;
using PESpy.View.Builder;

namespace PESpy.View
{
    /// <summary>
    /// Provides facilities for analyzing the contents of a <see cref="PEFile"/>.
    /// </summary>
    internal unsafe class PEFileAnalyzer : FileAnalyzer
    {
        private readonly PEFile _peFile;
        private PESectionLookupCache _lookupCache;

        internal PEFileAnalyzer(PEFileAccessor fileAccessor, IFileDisassembler? disassembler, IFileAnalyzerProgress? progress) : base(fileAccessor, disassembler)
        {
            _peFile = fileAccessor.PEFile;
            _lookupCache = new PESectionLookupCache(fileAccessor.PEFile);
        }

        protected override ViewWriter CreateViewWriter() =>
                new PEViewByteViewWriter(((PEFileAccessor) _fileAccessor).PEFile, _fileAccessor, _fileDisassembler);

        public override FileAccessor Execute()
        {
            //Mark all data structures that our PEFile knows about as being data
            ((IViewable) _peFile).WriteGlobals(_viewWriter);

            //We may or may not have symbols. Collect any code locations pointed to by the PEFile
            //so we can at least disassemble something
            DiscoverCodeRoots();

            //Now try and discover symbols. Symbols can come in many forms: we can have a PDB (old, MSF or portable),
            //OMF CodeView symbols, or even COFF symbols in a CoffSymbolTable. We'll use any symbols we discover to expand
            //upon the code addresses we found in our roots, to ensure we disassemble as much as possible in the PEFile
            DiscoverSymbols();

            //We've done all the preparations we can; work the disasm queue, discovering xrefs and tagging bytes as being code
            WorkDisasmQueue();

            //Go through all remaining untagged bytes and mark any repeated sequences of 0x00 or 0xCC as being padding
            MarkPadding();

            var dataDirectories = new PooledList<DirectoryInfo>();

            try
            {
                ((PEViewWriter) _viewWriter).CollectDataDirectories(ref dataDirectories);

                ((PEFileAccessor) _fileAccessor).DataDirectories = dataDirectories.ToArray();
            }
            finally
            {
                dataDirectories.Dispose();
            }

            return _fileAccessor;
        }

        #region DiscoverCodeRoots

        private void DiscoverCodeRoots()
        {
            var entryPoint = _peFile.OptionalHeader.AddressOfEntryPoint;

            if (entryPoint != 0)
            {
                if (_lookupCache.TryGetSectionInfo(entryPoint, out var targetAddress, out var sectionIndex, out _))
                    AddCode(targetAddress, entryPoint, sectionIndex);
            }

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
                                : _fileAccessor.AddData(targetAddress, sectionIndex, ViewByteDataKind.Byte, length: 1); //We don't know how big this data item is yet, so we'll just say it's 1 byte. If we get some symbols, we might be able to do better

                            if (export.Name.Length > 0)
                                _fileAccessor.AddName(targetAddress, info, (FixedUtf8String) export.Name);
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
                        AddCode(targetAddress, item.BeginAddress, sectionIndex);
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
                                    AddCode(targetAddress, record.BeginAddress, sectionIndex);
                                }

                                if (lookupCache.TryGetSectionInfo(record.EndAddress, out targetAddress, out sectionIndex, out _))
                                    AddCode(targetAddress, record.EndAddress, sectionIndex);

                                /* If no custom handler has been specified, this value is EXCEPTION_EXECUTE_HANDLER (1).
                                 * Ostensibly, it should also be possible for this value to be EXCEPTION_CONTINUE_SEARCH (0)
                                 * and EXCEPTION_CONTINUE_EXECUTION (-1) */
                                if (record.HandlerAddress > 1)
                                {
                                    if (lookupCache.TryGetSectionInfo(record.HandlerAddress, out targetAddress, out sectionIndex, out _))
                                        AddCode(targetAddress, record.HandlerAddress, sectionIndex);
                                }

                                if (lookupCache.TryGetSectionInfo(record.JumpTarget, out targetAddress, out sectionIndex, out _))
                                    AddCode(targetAddress, record.JumpTarget, sectionIndex);
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
                            AddCode(targetAddress, (int)address, sectionIndex);
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
                            AddCode(targetAddress, entry.Function, sectionIndex);
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
                            AddCode(targetAddress, entry.Target, sectionIndex);
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
                            AddCode(targetAddress, entry.Function, sectionIndex);
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

                            _fileAccessor.AddName(targetAddress, pILViewByte, (FixedUtf8String) name.Value);
                        }
                    }
                }
            }
        }

        #endregion
        #region DiscoverSymbols

        private void DiscoverSymbols()
        {
        }

        #endregion
    }
}
