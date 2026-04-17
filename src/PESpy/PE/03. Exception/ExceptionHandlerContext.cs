using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

namespace PESpy
{
    internal class ExceptionHandlerContext
    {
        private PEFile _peFile;
        private Dictionary<int, WellKnownExceptionHandlerKind> _knownExceptionHandlers;
        private ISymbolAccessor _symbolAccessor;

        public ExceptionHandlerContext(PEFile peFile)
        {
            _peFile = peFile;
        }

        public WellKnownExceptionHandlerKind GetKind(int rva)
        {
            //If symbols are available, we can avoid the penalty of having to analyze all of
            //our RUNTIME_FUNCTION + UNWIND_INFO entities; we can just probe the symbols directly

            if (_symbolAccessor == null)
            {
                //There's a potential to race between setting the symbolAccessor and then the _knownExceptionHandlers
                lock (this)
                {
                    //We don't know whether or not we have symbols yet
                    _symbolAccessor = _peFile.GetSymbolAccessor(LocatorHttpPolicy.None);

                    if (_symbolAccessor is NullSymbolAccessor)
                    {
                        ComputeKnownExceptionHandlers(); //It's the responsibility of this method to set the dictionary
                        Debug.Assert(_knownExceptionHandlers != null);
                    }
                    else
                        _knownExceptionHandlers = new Dictionary<int, WellKnownExceptionHandlerKind>();
                }
            }

            //If we have symbols, we need to query known exception handlers one at a time.
            //If we don't have symbols, we should have already computed all possible exception
            //handlers and have added them to the list
            if (_knownExceptionHandlers.TryGetValue(rva, out var kind))
                return kind;

            Debug.Assert(_symbolAccessor is not NullSymbolAccessor);

            //We should not have any displacement
            if (_symbolAccessor.TryGetNameFromAddress(rva, out var name, out var displacement) && displacement == 0)
            {
                kind = GetKindFromName(name.AsSpan());
                _knownExceptionHandlers[rva] = kind;
                return kind;
            }
            else
            {
                _knownExceptionHandlers[rva] = WellKnownExceptionHandlerKind.Unknown;
                return WellKnownExceptionHandlerKind.Unknown;
            }
        }

        internal unsafe void ComputeKnownExceptionHandlers()
        {
            Debugger.NotifyOfCrossThreadDependency();

            var peFile = _peFile;

            var exceptionTableDirectory = peFile.OptionalHeader.ExceptionTableDirectory;

            //If we're being asked for our ExceptionHandlerContext, implicitly we should have an ExcepionTableDirectory
            if (!peFile.TryGetDirectoryChunk(exceptionTableDirectory, out var chunk))
                throw new InvalidOperationException("Attempted to compute exception handlers without an ExceptionTableDirectory");

            var pRuntimeFunction = (RUNTIME_FUNCTION*) chunk.Pointer;

            var numEntries = exceptionTableDirectory.Size / RuntimeFunction.StructSize;

            using var uniqueUnwindInfoRVAs = new UnwindInfoHashSet(numEntries); //Worst case scenario, every RUNTIME_FUNCTION has its own UNWIND_INFO. In practice, the actual count will be much less than this

            var exceptionTableStart = exceptionTableDirectory.VirtualAddress;
            var exceptionTableEnd = exceptionTableStart + exceptionTableDirectory.Size;

            //Iterating a span will result in bounds checks
            for (var i = 0; i < numEntries; i++)
            {
                var runtimeFunction = pRuntimeFunction[i];

                var unwindData = runtimeFunction.UnwindData;

                if (unwindData == 0)
                    continue;

                //Ignore items that point 1 byte into another RUNTIME_FUNCTION entry. PEAnatomist labels these entries
                //"Indirect"
                if (unwindData < exceptionTableStart || unwindData > exceptionTableEnd)
                    uniqueUnwindInfoRVAs.Add(unwindData);
            }

            var knownExceptionHandlers = new Dictionary<int, WellKnownExceptionHandlerKind>();

            if (uniqueUnwindInfoRVAs.Count == 0)
            {
                //I guess we're not going to have any ExceptionData!
                _knownExceptionHandlers = knownExceptionHandlers;
                return;
            }

            var sortedUnwindInfoRVAs = uniqueUnwindInfoRVAs.Entries;
            sortedUnwindInfoRVAs.Sort();

            sortedUnwindInfoRVAs = sortedUnwindInfoRVAs.Slice(uniqueUnwindInfoRVAs.Capacity - uniqueUnwindInfoRVAs.Count);

            //Our expectation is that all UNWIND_INFO items should be listed sequentially after each
            //other. As such, lets get the section that the first item belongs to, and then try and assume
            //that all other entries

            if (!peFile.TryGetSectionContainingRVA(sortedUnwindInfoRVAs[0], out var sectionIndex, out var sectionHeader))
            {
                //We rely on two things: a. the RVA of the first item being valid so we can quickly compute the physical offset
                //of each item, and b. all items being in the same section! If we can't get a section here, neither of those
                //things are true. Fail completely for now
                Debug.Assert(false);
                _knownExceptionHandlers = knownExceptionHandlers;
                return;
            }

            var block = peFile.GetSectionBlock(sectionIndex, sectionHeader);

            var vaStart = sectionHeader.VirtualAddress;
            var vaEnd = vaStart + sectionHeader.VirtualSize;

            //This works for both loaded and unloaded cases
            var pData = block.LocalPointer - vaStart;

#if DEBUG
            var symbolAccessor = peFile.GetSymbolAccessor();
#endif

            //For each UNWIND_INFO RVA, calculate the distance available between the
            //ExceptionHandler of that item and the UNWIND_INFO item that follows it
            for (var i = 0; i < sortedUnwindInfoRVAs.Length - 1; i++)
            {
                var unwindInfoRVA = sortedUnwindInfoRVAs[i];

                if (unwindInfoRVA < vaStart || unwindInfoRVA >= vaEnd)
                {
                    //A critical assumption we've made has failed. We don't currently implement fallback logic.
                    //Skip this item for now
                    Debug.Assert(false);
                    continue;
                }

                var pUnwindInfo = pData + unwindInfoRVA;

                var flags = (UNW_FLAG) ((*(pUnwindInfo + UnwindInfo.versionAndFlagsOffset) >> 3) & 0x1f);
                
                if (((int) flags & (int) UNW_FLAG.EHANDLER) != 0 || ((int) flags & (int) UNW_FLAG.UHANDLER) != 0)
                {
                    var countOfCodes = *(pUnwindInfo + UnwindInfo.CountOfCodesOffset);
                    var extraDataStart = 4 + (((countOfCodes + 1) & ~1) * 2); //4 fixed bytes + CountOfCodes aligned to an even number
                    var exceptionHandler = *(int*) (pUnwindInfo + extraDataStart);

                    //If we're in Debug mode, try and categorize anyway, and then assert that if the kind is already known, we got the same result
#if !DEBUG
                    if (knownExceptionHandlers.ContainsKey(exceptionHandler))
                        continue;
#else
                    //Get the symbol name to assist with debugging
                    symbolAccessor.TryGetNameFromAddress(exceptionHandler, out var handlerName, out var displacement);
#endif
                    if (exceptionHandler == 0)
                    {
                        Debug.Assert(false); //Can you have a null exception handler?
                        continue;
                    }

                    var bytesUsed = extraDataStart + sizeof(int);

                    var nextUnwindInfoRVA = sortedUnwindInfoRVAs[i + 1];
                    uint exceptionDataLength = (uint) (nextUnwindInfoRVA - (unwindInfoRVA + bytesUsed)); //Very important we do unsigned comparisons, e.g. SCOPE_TABLE Count could give us so much data we're negative

                    var pExceptionData = pUnwindInfo + bytesUsed;

                    var allocationSize = GetAllocationSize(pUnwindInfo);

                    var computedKind = CategorizeExceptionData(_peFile, pExceptionData, exceptionDataLength, allocationSize);

#if DEBUG
                    var exceptionDataSpan = new Span<byte>(pExceptionData, (int) exceptionDataLength);

                    if (displacement == 0)
                    {
                        var symbolKind = GetKindFromName(handlerName.AsSpan());

                        switch (symbolKind)
                        {
                            //We can't detect the noexcept variant, so assert we got the normal variant
                            case WellKnownExceptionHandlerKind.__C_specific_handler_noexcept:
                                symbolKind = WellKnownExceptionHandlerKind.__C_specific_handler;
                                break;

                            case WellKnownExceptionHandlerKind.CrashForExceptionInNonABICompliantCodeRange:
                                //Not currently supported
                                symbolKind = WellKnownExceptionHandlerKind.Unknown;
                                break;
                        }

                        Debug.Assert(symbolKind == computedKind);
                    }

                    if (displacement == 0 && knownExceptionHandlers.TryGetValue(exceptionHandler, out var existingKind))
                    {
                        Debug.Assert(existingKind == computedKind, "Computed a different exception handler across different UNWIND_INFO items");
                    }
                    else
                    {
                        knownExceptionHandlers[exceptionHandler] = computedKind;
                    }
#else
                    knownExceptionHandlers[exceptionHandler] = computedKind;
#endif
                }
            }

            _knownExceptionHandlers = knownExceptionHandlers;
        }

        internal static unsafe void WriteUnwindInfos(
            PEFile peFile,
            PEViewByteViewWriter viewWriter,
            ISymbolAccessor symbolAccessor)
        {
            var hashSet = viewWriter._unwindInfos;

            if (hashSet.Count == 0)
                return;

            //If we've got symbols, use them. Otherwise, we need to compute what each ExceptionHandler is
            if (symbolAccessor is not NullSymbolAccessor)
            {
                WriteUnwindInfosFromSymbols();
                return;
            }

            var sortedUnwindInfoRVAs = hashSet.Entries;
            sortedUnwindInfoRVAs.Sort();

            sortedUnwindInfoRVAs = sortedUnwindInfoRVAs.Slice(hashSet.Capacity - hashSet.Count);

            var knownExceptionHandlers = new Dictionary<int, WellKnownExceptionHandlerKind>();

            if (!peFile.TryGetSectionContainingRVA(sortedUnwindInfoRVAs[0], out var sectionIndex, out var sectionHeader))
                return;

            ref var sectionAccessor = ref viewWriter._fileAccessor.SectionAccessors[sectionIndex + 1];

            var block = peFile.GetSectionBlock(sectionIndex, sectionHeader);

            var vaStart = sectionHeader.VirtualAddress;
            var vaEnd = vaStart + sectionHeader.VirtualSize;

            var structOffsetBase =
                peFile.IsLoadedImage ? 0 : (sectionHeader.PointerToRawData - sectionHeader.VirtualAddress);

            //This works for both loaded and unloaded cases
            var pData = block.LocalPointer - vaStart;

            for (var i = 0; i < sortedUnwindInfoRVAs.Length - 1; i++)
            {
                var unwindInfoRVA = sortedUnwindInfoRVAs[i];

                if (unwindInfoRVA < vaStart || unwindInfoRVA >= vaEnd)
                {
                    //A critical assumption we've made has failed. We don't currently implement fallback logic.
                    //Skip this item for now
                    Debug.Assert(false);
                    continue;
                }

                var pUnwindInfo = pData + unwindInfoRVA;

                var flags = (UNW_FLAG) ((*(pUnwindInfo + UnwindInfo.versionAndFlagsOffset) >> 3) & 0x1f);

                var countOfCodes = *(pUnwindInfo + UnwindInfo.CountOfCodesOffset);
                var extraDataStart = 4 + (((countOfCodes + 1) & ~1) * 2); //4 fixed bytes + CountOfCodes aligned to an even number

                var unwindInfoRelativeOffset = (int) (pUnwindInfo - block.LocalPointer);

                var pViewByte = sectionAccessor.pViewBytes + unwindInfoRelativeOffset;

                if (((int) flags & (int) UNW_FLAG.EHANDLER) != 0 || ((int) flags & (int) UNW_FLAG.UHANDLER) != 0)
                {
                    var exceptionHandler = *(int*) (pUnwindInfo + extraDataStart);

                    var bytesUsed = extraDataStart + sizeof(int);

#if DEBUG
                    symbolAccessor.TryGetNameFromAddress(exceptionHandler, out var handlerName, out var displacement);
#endif

                    if (!knownExceptionHandlers.TryGetValue(exceptionHandler, out var kind))
                    {
                        if (exceptionHandler == 0)
                        {
                            Debug.Assert(false); //Can you have a null exception handler?
                            continue;
                        }

                        var nextUnwindInfoRVA = sortedUnwindInfoRVAs[i + 1];
                        uint exceptionDataLength = (uint) (nextUnwindInfoRVA - (unwindInfoRVA + bytesUsed)); //Very important we do unsigned comparisons, e.g. SCOPE_TABLE Count could give us so much data we're negative

                        var pExceptionData = pUnwindInfo + bytesUsed;

                        var allocationSize = GetAllocationSize(pUnwindInfo);

                        kind = CategorizeExceptionData(peFile, pExceptionData, exceptionDataLength, allocationSize);

                        //If displacement is not 0, this indicates we potentially added something we're not supposed to have.
                        //We encounter this issue with msedge.dll
#if DEBUG
                        if (displacement != 0 && kind != WellKnownExceptionHandlerKind.Unknown)
                        {
                            //todo: we need to be asserting and figuring out how to avoid false positives
                            Debug.Assert(kind == WellKnownExceptionHandlerKind.__CxxFrameHandler4); //Misattributing FuncInfo4 is common
                            kind = WellKnownExceptionHandlerKind.Unknown;
                        }
#endif

                        knownExceptionHandlers[exceptionHandler] = kind;
                    }

                    if (kind == WellKnownExceptionHandlerKind.Unknown)
                        continue;

                    var dataChunk = new MemoryChunk(block, unwindInfoRelativeOffset + bytesUsed);

                    UnwindInfo.WriteUnwindInfo(
                        viewWriter,
                        dataChunk,
                        pViewByte,
                        structOffsetBase + unwindInfoRelativeOffset + block.RemoteStartOffset,
                        sectionAccessor.StartAddress + unwindInfoRelativeOffset,
                        fieldOffset: bytesUsed,
                        kind
                    );
                }
                else
                {
                    var structSize = extraDataStart;

                    if (((int) flags & (int) UNW_FLAG.CHAININFO) != 0)
                        structSize += sizeof(RUNTIME_FUNCTION);

                    var targetAddress = sectionAccessor.StartAddress + unwindInfoRelativeOffset;
                    viewWriter.RegisterStruct(pViewByte, Strings.UNWIND_INFO, targetAddress, ViewKind.UnwindInfo);

                    for (var j = pViewByte + 1; j < pViewByte + structSize; j++)
                        j->Kind = ViewByteKind.Body;
                }
        {
        private static unsafe WellKnownExceptionHandlerKind CategorizeExceptionData(
            PEFile peFile,
            byte* pExceptionData,
            uint exceptionDataLength,
            int allocationSize)
        {
            var analysis = new ExceptionDataAnalysis();

            DetectGSHandlerData(ref analysis.HasGSHandlerData, pExceptionData, exceptionDataLength, allocationSize);
            DetectScopeTable(ref analysis, pExceptionData, exceptionDataLength, allocationSize);
            DetectFuncInfo(peFile, ref analysis, pExceptionData, exceptionDataLength, allocationSize);

            return analysis.Kind;
        }

        private static unsafe void DetectGSHandlerData(ref bool hasGSHandlerData, byte* pExceptionData, uint exceptionDataLength, int allocationSize)
        {
            /* Consider the following function
             * 
             * push    rbx
             * sub     rsp, 30h
             * mov     rax, cs:__security_cookie
             * xor     rax, rsp
             * mov     [rsp+28h], rax
             * 
             * The size of the frame ix 0x30 bytes, and the security cookie is stored at offset 0x28. As such,
             * if the CookieOffset is outside the bounds of the frame, it can't be a valid _GS_HANDLER_DATA value
             */

            //_GS_HANDLER_DATA consists of a bitfield with 3 known bits. As such, if any bits
            //other than those 3 are defined, it's not _GS_HANDLER_DATA
            if (exceptionDataLength >= 4 && ((*(uint*) pExceptionData) & 0xFFFFFFF8) != 0)
            {
                var gsHandlerData = new GsHandlerData(0, pExceptionData); //Offset is not important

                var cookieOffset = gsHandlerData.CookieOffset;

                //If it's GS_HANDLER_DATA, the offset to the cookie should be within the allocation area
                if (cookieOffset < allocationSize)
                {
                    //There are many different locations where _GS_HANDLER_DATA can be found, so we have the caller tell us
                    //which specific field they want us to fill in the event we find any data
                    hasGSHandlerData = true;
                }
            }
        }

        private static unsafe void DetectScopeTable(ref ExceptionDataAnalysis analysis, byte* pExceptionData, uint exceptionDataLength, int allocationSize)
        {
            //SCOPE_TABLE consists of a 4 byte Count, followed
            //by 1 or more ScopeRecord entries (which are 16 bytes each)

            if (exceptionDataLength >= (sizeof(int) + ScopeTable.ScopeRecord.StructSize)) //20
            {
                var count = *(int*) pExceptionData;

                //Assuming this is infact a SCOPE_TABLE, based on the stated count
                //the following would be the size of the record
                uint requiredSize = (uint) (sizeof(int) + (count * 16));
                var remainingSize = exceptionDataLength - requiredSize;

                //The first two fields of a ScopeTable record are the BeginAddress
                //and EndAddress. As a sanity check, we would expect that the BeginAddress
                //should not be after the EndAddress
                var recordBeginAddress = *(((int*) pExceptionData) + 1); //Skip over SCOPE_TABLE.Count
                var recordEndAddress = *(((int*) pExceptionData) + 2); //Skip over SCOPE_TABLE.Count and the BeginAddress above

                /* You might expect that we should be asserting that the expected size should match the actual
                 * size available, however I've seen a __C_specific_handler that only had a single SCOPE_TABLE
                 * record (meaning it only needed 20 bytes), and yet there were 4 zero bytes after it (padding?)
                 * for a total of 24 bytes */
                if (exceptionDataLength >= requiredSize
                    && recordBeginAddress != 0
                    && recordBeginAddress < recordEndAddress)
                {
                    //Sounds pretty SCOPE_TABLE like to me!
                    analysis.HasScopeTable = true;

                    if (remainingSize >= 4)
                    {
                        DetectGSHandlerData(ref analysis.HasScopeTableGSHandlerData, pExceptionData + requiredSize, remainingSize, allocationSize);
                    }
                }
            }
        }

        private static unsafe void DetectFuncInfo(
            PEFile peFile,
            ref ExceptionDataAnalysis analysis,
            byte* pExceptionData,
            uint exceptionDataLength,
            int allocationSize)
        {
            //Check for an RVA that points to a value that seems to start with an EH_MAGIC_NUMBER
            if (exceptionDataLength >= 4)
            {
                var rva = *(int*) pExceptionData;

                if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                {
                    //At a minimum we need enough space to store a FuncInfoV1
                    if (FuncInfo.StructSizeV1 < valueChunk.Remaining)
                    {
                        var magicNumberAndBBTFlags = valueChunk.PeekInt32(FuncInfo.magicNumberAndBBTFlagsOffset);

                        var magicNumber = (EH_MAGIC_NUMBER) (magicNumberAndBBTFlags & ((1 << 29) - 1));

                        //PEAnatomist checks a range of 0xF values, but only FuncInfo versions 1, 2 and 3 were defined.
                        //V4 omits the magic number entirely as it was deemed to be a waste of space
                        //https://devblogs.microsoft.com/cppblog/making-cpp-exception-handling-smaller-x64/
                        switch (magicNumber)
                        {
                            case EH_MAGIC_NUMBER.EH_MAGIC_NUMBER1:
                            case EH_MAGIC_NUMBER.EH_MAGIC_NUMBER2:
                            case EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3:
                                analysis.HasFuncInfo = true;
                                analysis.FuncInfoMagicNumber = magicNumber;

                                //If the FuncInfo was located right after the RVA, the ValueChunk we got wil simply be 4 bytes
                                //ahead of the pExceptionData, in which case we don't want to try and interpret the data as being
                                //_GS_HANDLER_DATA
                                if (exceptionDataLength >= 8 && (valueChunk.Pointer - pExceptionData) != sizeof(int)) //RVA was 4 bytes and we're saying we need at least anotehr 4 more
                                {
                                    //I've seen some strange data after both __CxxFrameHandler3 and __GSHandlerCheck_EH in msedsge.dll
                                    //that ends in 4 bytes of 0xFF. I'm not sure what that data is yet
                                    DetectGSHandlerData(ref analysis.HasFuncInfoGSHandlerData, pExceptionData + sizeof(int), exceptionDataLength - 4, allocationSize);
                                }
                                return;
                        }
                    }

                    //If it's EH_MAGIC_NUMBER it 100% can't be FuncInfo4, so if we failed to match that above, continue on below.
                    //When FuncInfo4 is present, the exception data contains an RVA to the FuncInfo4. However, often
                    //the data is situated directly after the RVA
                    var funcInfo = new FuncInfo4(valueChunk, functionAddress: 0); //FunctionAddress is not important here

                    /* FuncInfo4 utilizes a variable length encoding system wherein there is a header with certain bits set, and then
                     * optional RVA fields may follow based on this header. Sanity check that all RVAs we have on the object do indeed
                     * point to valid data */

                    var dispUnwindMap = funcInfo.dispUnwindMap;

                    if (!dispUnwindMap.IsEmpty && !dispUnwindMap.IsValid)
                        return; //Bad RVA

                    var dispTryBlockMap = funcInfo.dispTryBlockMap;

                    if (!dispTryBlockMap.IsEmpty && !dispTryBlockMap.IsValid)
                        return; //Bad RVA

                    if (funcInfo.header.isSeparated)
                    {
                        var dispToSegMap = funcInfo.dispToSegMap;

                        if (!dispToSegMap.IsEmpty && !dispToSegMap.IsValid)
                            return; //Bad RVA
                    }
                    else
                    {
                        var dispIPtoStateMap = funcInfo.dispIPtoStateMap;

                        if (!dispIPtoStateMap.IsEmpty && !funcInfo.dispIPtoStateMap.IsValid)
                            return; //Bad RVA
                    }

                    //dispFrame is not an RVA

                    analysis.HasFuncInfo4 = true;

                    //If the FuncInfo4 was located right after the RVA, the ValueChunk we got wil simply be 4 bytes
                    //ahead of the pExceptionData, in which case we don't want to try and interpret the data as being
                    //_GS_HANDLER_DATA

                    if ((valueChunk.Pointer - pExceptionData) != sizeof(int))
                    {
                        //When we're __GSHandlerCheck_EH4, the FuncInfo4 isn't directly after the RVA to it
                        if (exceptionDataLength >= 8) //RVA was 4 bytes and we're saying we need at least anotehr 4 more
                        {
                            DetectGSHandlerData(ref analysis.HasFuncInfoGSHandlerData, pExceptionData + sizeof(int), exceptionDataLength - sizeof(int), allocationSize);
                        }
                    }
                }
            }
        }

        private static unsafe int GetAllocationSize(byte* pUnwindInfo)
        {
            /* When you've got a _GS_HANDLER_DATA, it will contain a CookieOffset that specifies the offset in
             * mov [rsp + ?], __security_cookie that the security cookie is stored in on the stack (note that the security
             * cookie may have been stored into a register prior to moving into its home). The offset specified in "?"
             * will be less than the size of the frame establishment in "sub rsp, ?". Thus, we can check if the CookieOffset
             * lies within the bounds of the frame allocation to see if a given value looks like a possible _GS_HANDLER_DATA */
            var unwindCodes = new UnwindCodeList(pUnwindInfo, false);
            
            foreach (var unwindCode in unwindCodes)
            {
                switch (unwindCode.UnwindOp)
                {
                    case UWOP.UWOP_ALLOC_LARGE:
                        return ((UnwindCode.AllocLarge) unwindCode).Size;

                    case UWOP.UWOP_ALLOC_SMALL:
                        return ((UnwindCode.AllocSmall) unwindCode).Size;
                }
            }

            return 0;
        }

        private static unsafe WellKnownExceptionHandlerKind GetKindFromName(Span<byte> name)
        {
            if (name.SequenceEqual("__C_specific_handler"u8))
                return WellKnownExceptionHandlerKind.__C_specific_handler;

            if (name.SequenceEqual("__C_specific_handler_noexcept"u8))
                return WellKnownExceptionHandlerKind.__C_specific_handler_noexcept;

            if (name.SequenceEqual("__CxxFrameHandler"u8))
                return WellKnownExceptionHandlerKind.__CxxFrameHandler;

            if (name.SequenceEqual("__CxxFrameHandler3"u8))
                return WellKnownExceptionHandlerKind.__CxxFrameHandler3;

            if (name.SequenceEqual("__CxxFrameHandler4"u8))
                return WellKnownExceptionHandlerKind.__CxxFrameHandler4;

            if (name.SequenceEqual("__GSHandlerCheck"u8))
                return WellKnownExceptionHandlerKind.__GSHandlerCheck;

            if (name.SequenceEqual("__GSHandlerCheck_SEH"u8))
                return WellKnownExceptionHandlerKind.__GSHandlerCheck_SEH;

            if (name.SequenceEqual("__GSHandlerCheck_EH"u8))
                return WellKnownExceptionHandlerKind.__GSHandlerCheck_EH;

            if (name.SequenceEqual("__GSHandlerCheck_EH4"u8))
                return WellKnownExceptionHandlerKind.__GSHandlerCheck_EH4;

            if (name.SequenceEqual("LdrpICallHandler"u8))
                return WellKnownExceptionHandlerKind.LdrpICallHandler;

            if (name.SequenceEqual("KiUserApcHandler"u8))
                return WellKnownExceptionHandlerKind.KiUserApcHandler;

            if (name.SequenceEqual("KiUserCallbackDispatcherHandler"u8))
                return WellKnownExceptionHandlerKind.KiUserCallbackDispatcherHandler;

            if (name.SequenceEqual("RtlpUnwindHandler"u8))
                return WellKnownExceptionHandlerKind.RtlpUnwindHandler;

            if (name.SequenceEqual("RtlpExceptionHandler"u8))
                return WellKnownExceptionHandlerKind.RtlpExceptionHandler;

            if (name.SequenceEqual("RtlpEnclaveCallDispatchFilter"u8))
                return WellKnownExceptionHandlerKind.RtlpEnclaveCallDispatchFilter;

            if (name.SequenceEqual("CrashForExceptionInNonABICompliantCodeRange"u8))
                return WellKnownExceptionHandlerKind.CrashForExceptionInNonABICompliantCodeRange;

            if (name.StartsWith("@ILT+"u8))
            {
                var openParen = name.IndexOf((byte) '(');
                var closeParen = name.LastIndexOf((byte) ')');

                if (openParen != -1 && closeParen != -1 && closeParen > openParen)
                {
                    var str = name.Slice(openParen + 1, closeParen - openParen - 1);

                    return GetKindFromName(str);
                }
            }

            //Need to add support for the specified handler. If we decide to remove this assert, we should definitely
            //add asserts for missing C_specific_handler, CxxFrameHandler and GSHandlerCheck
#if DEBUG
            fixed (byte* pName = name)
            {
                var str = new FixedUtf8String(pName, name.Length);
                Debug.Assert(false);
            }
#endif
            return WellKnownExceptionHandlerKind.Unknown;
        }
    }
}
