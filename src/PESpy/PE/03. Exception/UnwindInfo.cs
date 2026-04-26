using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    //Required because we want RuntimeFunction and UnwindInfo to both be value types, which will cause an issue because without this type
    //there would be a recursive link between them
    [DebuggerDisplay("BeginAddress = 0x{BeginAddress.ToString(\"X\"),nq}, EndAddress = 0x{EndAddress.ToString(\"X\"),nq}")] //I had issues with my ReadyToRunHeader_Test wherein when an exception occurs trying to resolve the UnwindData, I start getting NullReferenceException errors in the Visual Studio debugger trying to inspect a RuntimeFunction object. So I'm not including the UnwindData in the DebuggerDisplay
    public class ChainedRuntimeFunction
    {
        private readonly RuntimeFunction runtimeFunction;

        public int BeginAddress => runtimeFunction.BeginAddress;

        public int EndAddress => runtimeFunction.EndAddress;

        public RVA<UnwindInfo> UnwindData => runtimeFunction.UnwindData;

        internal ChainedRuntimeFunction(in MemoryChunk chunk)
        {
            this.runtimeFunction = new RuntimeFunction(chunk);
        }

        public static implicit operator RuntimeFunction(ChainedRuntimeFunction value) => value.runtimeFunction;
    }

    /// <summary>
    /// Represents the <see cref="UNWIND_INFO"/> structure.
    /// </summary>
    public struct UnwindInfo : IValue, IViewable
    {
        internal const int versionAndFlagsOffset = 0;
        private const int SizeOfPrologOffset = 1;
        internal const int CountOfCodesOffset = 2;
        internal const int frameRegisterAndOffsetOffset = 3;
        internal const int UnwindCodeOffset = 4;

        public byte Version => (byte) (versionAndFlags & 0x7); //bottom 3 bits

        public UNW_FLAG Flags => (UNW_FLAG) ((versionAndFlags >> 3) & 0x1f); //top 5 bits

        private byte versionAndFlags => chunk.PeekByte(versionAndFlagsOffset);

        public byte SizeOfProlog => chunk.PeekByte(SizeOfPrologOffset);

        public byte CountOfCodes => chunk.PeekByte(CountOfCodesOffset);

        private byte frameRegisterAndOffset => chunk.PeekByte(frameRegisterAndOffsetOffset);

        public Register FrameRegister => (Register) (frameRegisterAndOffset & 0x0F);

        //The actual frame offset is this 16 * FrameOffset
        public byte FrameOffset => (byte) ((frameRegisterAndOffset & 0xF0) >> 4);

        /// <summary>
        /// Gets the UNWIND_CODE instances associated with this UNWIND_INFO.<para/>
        /// Note that if <see cref="CountOfCodes"/> is not even, this list will end
        /// with a "null" alignment code, whose <see cref="UnwindCode.IsNull"/>
        /// will return <see langword="true"/>.<para/>
        /// To get all unwind codes excluding the "null" alignment code, use <see cref="GetUnwindCode(bool)"/>.
        /// </summary>
        public unsafe UnwindCodeList UnwindCode => new UnwindCodeList(chunk.Pointer, true);

        public unsafe UnwindCodeList GetUnwindCode(bool includeAlignment) => new UnwindCodeList(chunk.Pointer, includeAlignment);

        public int ExceptionHandler { get; }

        //Needs to be indirected via a reference type, because we can't have UnwindInfo and RuntimeFunction both be reference types
        public ChainedRuntimeFunction? FunctionEntry { get; } //UNWIND_INFO says that it's an int, but it's really a RUNTIME_FUNCTION

        private IValue? exceptionData;
        public IValue? ExceptionData => exceptionData ??= GetExceptionDataForHandlerKind(ExceptionHandlerKind);

        private WellKnownExceptionHandlerKind exceptionHandlerKind;

        public WellKnownExceptionHandlerKind ExceptionHandlerKind
        {
            get
            {
                if (exceptionHandlerKind == 0)
                {
                    if (ExceptionHandler != 0)
                    {
                        //Computing the exception handler kind can be fairly computationally expensive.
                        //We may need to lookup symbols, or analyze _all_ RUNTIME_FUNCTION records in order
                        //to compute the maximum possible length of each UNWIND_INFO item's ExceptionData.
                        //We'll leave it to the ExceptionHandlerContext to decide
                        //whether to block the debugger from querying things

                        exceptionHandlerKind = chunk.PEFile().ExceptionHandlerContext.GetKind(ExceptionHandler);
                    }
                    else
                    {
                        exceptionHandlerKind = WellKnownExceptionHandlerKind.None;
                    }
                }

                return exceptionHandlerKind;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal int FixedStructSize
        {
            get
            {
                var size = 4 + (((CountOfCodes + 1) & ~1) * 2); //4 fixed bytes + CountOfCodes aligned to an even number

                if (HasExceptionHandler)
                {
                    size += sizeof(int); //ExceptionHandler
                }
                else if (((int) Flags & (int) UNW_FLAG.CHAININFO) != 0)
                {
                    size += RuntimeFunction.StructSize; //FunctionEntry
                }

                return size;
            }
        }

        internal int StructSize
        {
            get
            {
                if (HasExceptionHandler)
                {
                    var offset = FixedStructSize;

                    return offset + GetExceptionDataSize(ExceptionHandlerKind, chunk.Slice(offset), out _);
                }

                return FixedStructSize;
            }
        }

        /// <summary>
        /// Gets whether the <see cref="Flags"/> of this <see cref="UnwindInfo"/> indicate that it should have an <see cref="ExceptionHandler"/>
        /// and <see cref="ExceptionData"/>.
        /// </summary>
        public bool HasExceptionHandler => ((int) Flags & (int) UNW_FLAG.EHANDLER) != 0 || ((int) Flags & (int) UNW_FLAG.UHANDLER) != 0;

        internal readonly MemoryChunk chunk;
        private readonly int functionAddress;

        //Following the unwind codes is either an ExceptionHandler, FunctionEntry or ExceptionData
        private int ExtraDataStart => 4 + (((CountOfCodes + 1) & ~1) * 2); //4 fixed bytes + CountOfCodes aligned to an even number

        //Should be used by ViewProvider only. FunctionAddress is not written in the view
        internal UnwindInfo(in MemoryChunk chunk) : this(chunk, 0)
        {
        }

        internal UnwindInfo(in MemoryChunk chunk, int functionAddress)
        {
            this.chunk = chunk;
            ExceptionHandler = default;
            FunctionEntry = default;
            exceptionData = default;
            this.functionAddress = functionAddress;

            Debug.Assert(Version is 1 or 2 or 3);

#if STRESS_TEST
            _ = UnwindCode;
#endif

            if (HasExceptionHandler)
            {
                ExceptionHandler = chunk.PeekInt32(ExtraDataStart);

                //ExceptionData is lazily computed
            }
            else if (((int) Flags & (int) UNW_FLAG.CHAININFO) != 0)
            {
                /* https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64?view=msvc-170#chained-unwind-info-structures
                 *
                 * The formula for calculating the address of the chained info is
                 *     PRUNTIME_FUNCTION primaryUwindInfo = (PRUNTIME_FUNCTION)&(unwindInfo->UnwindCode[( unwindInfo->CountOfCodes + 1 ) & ~1]);
                 *
                 * This seems quite complicated, but in effect what it's really saying is, "the chained RUNTIME_FUNCTION is listed after the aligned
                 * list of even-aligned UNWIND_INFO structures". We already read all of the UNWIND_INFO structures out of the way above; as such, the
                 * chained RUNTIME_FUNCTION now follows
                 */

                FunctionEntry = new ChainedRuntimeFunction(chunk.Slice(ExtraDataStart));
            }
        }

        private unsafe IValue GetExceptionDataForHandlerKind(WellKnownExceptionHandlerKind kind)
        {
            /* After the ExceptionHandler is the ExceptionData. Contrary to popular belief, the type of data pointed to by
             * ExceptionHandler is not always a SCOPE_TABLE. Rather, the type of value pointed to by this RVA depends on the
             * exception handler used. Other people have also had the same observation: https://reactos.org/wiki/Techwiki:SEH64
             *
             * The PE file does not need to know what kind of data is contained in the ExceptionData; it does not matter. Whatever it is,
             * a pointer to it will be passed to the ExceptionHandler routine, which will know what to do with it.
             * 
             * The easiest way to detect what handler is used is to use symbols. If symbols are not available however, we can do a
             * pretty good job of heuristically determining the type of handler that is used by collecting all UNWIND_INFO addresses
             * (which we presume will all be sequentially listed within a single given section), calculating the gap between all
             * items that have ExceptionData, and then checking for various patterns based on the expected layout of GS_HANDLER_DATA,
             * SCOPE_TABLE and FuncInfo items.
             *
             * Regardless of what mechanism we use to detect the handler, we'll end up with a WellKnownExceptionHandlerKind,
             * which directly maps to a particular type of data
             */

            var dataChunk = chunk.Slice(ExtraDataStart + 4);

            //I think PEAnatomist determines the function type by looking at how much data is remaining.
            //It then tries all possible heuristics on each piece of data

            int infoRVA;
            PEFile peFile;
            MemoryChunk infoChunk;

            //Note: any new items also need to be added to WriteUnwindInfo below

            switch (kind)
            {
                case WellKnownExceptionHandlerKind.Unknown:
                case WellKnownExceptionHandlerKind.None:
                    return null;

                #region GSHandlerCheck

                case WellKnownExceptionHandlerKind.__GSHandlerCheck: //_GS_HANDLER_DATA
                    return new GsHandlerData(dataChunk.AbsoluteOffset, dataChunk.Pointer);

                case WellKnownExceptionHandlerKind.__GSHandlerCheck_SEH: //SCOPE_TABLE + _GS_HANDLER_DATA
                    return new ScopeTableAndGsHandlerData(dataChunk);

                case WellKnownExceptionHandlerKind.__GSHandlerCheck_EH: //RVA<FuncInfo> + _GS_HANDLER_DATA
                    return new FuncInfoAndGsHandlerData(dataChunk);

                case WellKnownExceptionHandlerKind.__GSHandlerCheck_EH4: //RVA<FuncInfo4> + _GS_HANDLER_DATA
                    return new FuncInfo4AndGsHandlerData(dataChunk, functionAddress);

                #endregion
                #region C Specific Handler

                case WellKnownExceptionHandlerKind.__C_specific_handler:
                case WellKnownExceptionHandlerKind.__C_specific_handler_noexcept:
                    return new ScopeTable(dataChunk);

                #endregion
                #region CxxFrameHandler

                case WellKnownExceptionHandlerKind.__CxxFrameHandler:
                case WellKnownExceptionHandlerKind.__CxxFrameHandler2:
                case WellKnownExceptionHandlerKind.__CxxFrameHandler3:
                    //Value is an RVA to the FuncInfo. FuncInfo struct may or may not be right after its RVA
                    infoRVA = dataChunk.PeekInt32(0);

                    peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(infoRVA, out infoChunk))
                        return new RVA<FuncInfo>(infoRVA, infoChunk.AbsoluteOffset, new FuncInfo(infoChunk));
                    else
                        return new RVA<FuncInfo>(infoRVA);

                case WellKnownExceptionHandlerKind.__CxxFrameHandler4:
                    //Value is an RVA to the FuncInfo4. FuncInfo4 struct may or may not be right after its RVA
                    infoRVA = dataChunk.PeekInt32(0);

                    peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(infoRVA, out infoChunk))
                        return new RVA<FuncInfo4>(infoRVA, infoChunk.AbsoluteOffset, new FuncInfo4(infoChunk, functionAddress));
                    else
                        return new RVA<FuncInfo4>(infoRVA);

                #endregion

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(WellKnownExceptionHandlerKind)} '{kind}'");
            }
        }

        internal static unsafe void WriteUnwindInfo(
            PEViewByteViewWriter viewWriter,
            in MemoryChunk dataChunk,
            ViewByte* pViewByte,
            int structOffset,
            int targetAddress,
            int unwindInfoRVA,
            int fieldOffset,
            int exceptionHandler,
            WellKnownExceptionHandlerKind kind)
        {
            viewWriter.EnterUniqueXRef();

            var structSize = fieldOffset + GetExceptionDataSize(kind, dataChunk, out var dataKind);

            switch (dataKind)
            {
                case SpecialExceptionDataKind.None:
                    break;

                case SpecialExceptionDataKind.ScopeTable:
                    viewWriter._specialUnwindInfos.Add(unwindInfoRVA, dataKind);
                    var scopeTableCount = dataChunk.PeekInt32(0);

                    for (var i = 0; i < scopeTableCount; i++)
                    {
                        var scopeTableRecord = dataChunk.Slice(sizeof(int) + (i * ScopeTable.ScopeRecord.StructSize));

                        viewWriter.RelayGlobals(new ScopeTable.ScopeRecord(scopeTableRecord));
                    }

                    break;

                case SpecialExceptionDataKind.FuncInfo:
                    viewWriter._specialUnwindInfos.Add(unwindInfoRVA, dataKind);
                    WriteFuncInfo(viewWriter, dataChunk, structOffset, fieldOffset);
                    break;

                case SpecialExceptionDataKind.FuncInfo4:
                    viewWriter.WriteUniqueRVAXRef(structOffset, fieldOffset, dataChunk.PeekInt32(0));

                    //In order to write xrefs that need to include the RUNTIME_FUNCTION.BeginAddress, we can't write the FuncInfo4 here; instead, we need to just collect
                    //the addresses of all UNWIND_INFO items that contain a FuncInfo4, and then when we iterate over all RUNTIME_FUNCTION items again later, we'll
                    //write the FuncInfo4 properly for anyone whose UnwindData value in our list

                    viewWriter._specialUnwindInfos.Add(unwindInfoRVA, dataKind);
                    break;

                default:
                    throw new NotImplementedException();
            }

            viewWriter.ExitUniqueXRef();

            viewWriter.RegisterStruct(pViewByte, targetAddress, ViewKind.UnwindInfo);

            for (var i = pViewByte + 1; i < pViewByte + structSize; i++)
                i->Kind = ViewByteKind.Body;

            //We're guaranteed to only call WriteUnwindInfo for each unique item, so we don't need to worry about
            //tracking uniqueness here
            viewWriter.WriteTargetAddressXRef(targetAddress, fieldOffset - sizeof(int), exceptionHandler);
        }

        public enum SpecialExceptionDataKind
        {
            None,
            ScopeTable,
            FuncInfo,
            FuncInfo4
        }

        private static unsafe int GetExceptionDataSize(
            WellKnownExceptionHandlerKind kind,
            in MemoryChunk dataChunk,
            out SpecialExceptionDataKind dataKind)
        {
            //Avoid boxing the data and get the structs directly in here

            int scopeTableCount;
            int scopeTableSize;

            dataKind = default;

            //Note: any new items need to be added to GetExceptionDataForHandlerKind above

            switch (kind)
            {
                case WellKnownExceptionHandlerKind.Unknown:
                case WellKnownExceptionHandlerKind.None:
                    return 0;

                #region GSHandlerCheck

                case WellKnownExceptionHandlerKind.__GSHandlerCheck: //_GS_HANDLER_DATA
                    return new GsHandlerData(0, dataChunk.Pointer).StructSize;

                case WellKnownExceptionHandlerKind.__GSHandlerCheck_SEH: //SCOPE_TABLE + _GS_HANDLER_DATA
                    scopeTableCount = *(int*) dataChunk.Pointer;
                    scopeTableSize = sizeof(int) + (scopeTableCount * ScopeTable.ScopeRecord.StructSize);
                    dataKind = SpecialExceptionDataKind.ScopeTable;
                    return scopeTableSize + new GsHandlerData(0, dataChunk.Pointer + scopeTableSize).StructSize;

                case WellKnownExceptionHandlerKind.__GSHandlerCheck_EH: //RVA<FuncInfo> + _GS_HANDLER_DATA
                    dataKind = SpecialExceptionDataKind.FuncInfo;
                    return sizeof(int) + new GsHandlerData(0, dataChunk.Pointer + sizeof(int)).StructSize;

                case WellKnownExceptionHandlerKind.__GSHandlerCheck_EH4: //RVA<FuncInfo4> + _GS_HANDLER_DATA
                    dataKind = SpecialExceptionDataKind.FuncInfo4;
                    return sizeof(int) + new GsHandlerData(0, dataChunk.Pointer + sizeof(int)).StructSize;

                #endregion
                #region C Specific Handler

                case WellKnownExceptionHandlerKind.__C_specific_handler: //SCOPE_TABLE
                case WellKnownExceptionHandlerKind.__C_specific_handler_noexcept:
                    scopeTableCount = *(int*) dataChunk.Pointer;
                    scopeTableSize = sizeof(int) + (scopeTableCount * ScopeTable.ScopeRecord.StructSize);
                    dataKind = SpecialExceptionDataKind.ScopeTable;
                    return scopeTableSize;

                #endregion
                #region CxxFrameHandler

                case WellKnownExceptionHandlerKind.__CxxFrameHandler: //RVA<FuncInfo>
                case WellKnownExceptionHandlerKind.__CxxFrameHandler2:
                case WellKnownExceptionHandlerKind.__CxxFrameHandler3:
                    dataKind = SpecialExceptionDataKind.FuncInfo;

                    //Whether the RVA was valid or not, it _was_ there
                    return sizeof(int);

                case WellKnownExceptionHandlerKind.__CxxFrameHandler4: //RVA<FuncInfo4>
                    dataKind = SpecialExceptionDataKind.FuncInfo4;

                    //Whether the RVA was valid or not, it _was_ there
                    return sizeof(int);

                #endregion

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(WellKnownExceptionHandlerKind)} '{kind}'");
            }
        }

        private static void WriteFuncInfo(
            PEViewByteViewWriter viewWriter,
            in MemoryChunk dataChunk,
            int structOffset,
            int fieldOffset)
        {
            //Value is an RVA to the FuncInfo. FuncInfo struct may or may not be right after its RVA
            var infoRVA = dataChunk.PeekInt32(0);

            var peFile = dataChunk.PEFile();

            if (peFile.TryGetValueChunkFromSection(infoRVA, out var infoChunk))
            {
                viewWriter.WriteUniqueRVAField(
                    new RVA<FuncInfo>(infoRVA, infoChunk.AbsoluteOffset, new FuncInfo(infoChunk)),
                    structOffset,
                    fieldOffset
                );
            }
        }

        public enum Register : byte
        {
            //These values match the encoding that is used in ModR/M and UNWIND_INFO
            //(ModR/M requires REX.R=1 or REX.B=1 to achieve these values however)
            RAX = 0,  //0 000
            RCX = 1,  //0 001
            RDX = 2,  //0 010
            RBX = 3,  //0 011
            RSP = 4,  //0 100
            RBP = 5,  //0 101
            RSI = 6,  //0 110
            RDI = 7,  //0 111
            R8 = 8,   //1 000
            R9 = 9,   //1 001
            R10 = 10, //1 010
            R11 = 11, //1 011
            R12 = 12, //1 100
            R13 = 13, //1 101
            R14 = 14, //1 110
            R15 = 15  //1 111
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //We need to assert that all xrefs we write descending from the UnwindInfo are written uniquely,
            //as multiple RUNTIME_FUNCTION entries may point to the same UnwindInfo
            writer.EnterUniqueXRef();

            //If the ExceptionData is an RVA, there's globals
            switch (ExceptionHandlerKind)
            {
                case WellKnownExceptionHandlerKind.__CxxFrameHandler:
                case WellKnownExceptionHandlerKind.__CxxFrameHandler2:
                case WellKnownExceptionHandlerKind.__CxxFrameHandler3:
                    writer.WriteRVAField((RVA<FuncInfo>) ExceptionData, Offset, ExtraDataStart + sizeof(int));
                    break;

                case WellKnownExceptionHandlerKind.__CxxFrameHandler4:
                    writer.WriteRVAField((RVA<FuncInfo4>) ExceptionData, Offset, ExtraDataStart + sizeof(int));
                    break;

                default:
                    var data = ExceptionData as IViewable;

                    if (data != null)
                        data.WriteGlobals(writer);

                    break;
            }

            writer.ExitUniqueXRef();
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.UnwindInfo, writer.IsByteViewWriter ? FixedStructSize : StructSize);

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        unsafe void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            var s = structWriter.CreateEagerWriter();

            try
            {
                using (var b = s.WriteBitFields<byte>(2))
                {
                    b.WriteField("Version", Version, 3);
                    b.WriteField("Flags", Flags, 5);
                }

                s.WriteField(nameof(SizeOfProlog), SizeOfProlog);
                s.WriteField(nameof(CountOfCodes), CountOfCodes);

                using (var b = s.WriteBitFields<byte>(2))
                {
                    b.WriteField(nameof(FrameRegister), FrameRegister, 4);
                    b.WriteField(nameof(FrameOffset), FrameOffset, 4);
                }

                //If CountOfCodes is odd, this includes the empty one at the end
                s.WriteUnmanagedInline<UnwindCodeList, UnwindCodeList.Enumerator, UnwindCode>(UnwindCode);

                //What follows next depends on the Flags

                if (((int) Flags & (int) UNW_FLAG.EHANDLER) != 0 || ((int) Flags & (int) UNW_FLAG.UHANDLER) != 0)
                {
                    s.WriteField(nameof(ExceptionHandler), ExceptionHandler);

                    //We don't write UnwindInfo via IViewable; we collect all UnwindInfo items directly
                    //in WriteUnwindInfo. So at the point where we access the ExceptionData, either we're
                    //already inside a FileView and know what the exception handler kind is, or somewone
                    //is using a custom writer, in which case we _do_ need to eagerly compute the kind

                    var dataChunk = chunk.Slice(ExtraDataStart + 4);

                    int infoRVA;
                    PEFile peFile;
                    MemoryChunk infoChunk;

                    switch (ExceptionHandlerKind)
                    {
                        case WellKnownExceptionHandlerKind.Unknown:
                        case WellKnownExceptionHandlerKind.None:
                            break;

                        #region GSHandlerCheck

                        case WellKnownExceptionHandlerKind.__GSHandlerCheck: //_GS_HANDLER_DATA
                            s.WriteInline(new GsHandlerData(dataChunk.AbsoluteOffset, dataChunk.Pointer));
                            break;

                        case WellKnownExceptionHandlerKind.__GSHandlerCheck_SEH: //SCOPE_TABLE + _GS_HANDLER_DATA
                            new ScopeTableAndGsHandlerData(dataChunk).WriteInline(ref s);
                            break;

                        case WellKnownExceptionHandlerKind.__GSHandlerCheck_EH: //RVA<FuncInfo> + _GS_HANDLER_DATA
                            new FuncInfoAndGsHandlerData(dataChunk).WriteInline(ref s);
                            break;

                        case WellKnownExceptionHandlerKind.__GSHandlerCheck_EH4: //RVA<FuncInfo4> + _GS_HANDLER_DATA
                            new FuncInfo4AndGsHandlerData(dataChunk, functionAddress).WriteInline(ref s);
                            break;

                        #endregion
                        #region C Specific Handler

                        case WellKnownExceptionHandlerKind.__C_specific_handler:
                        case WellKnownExceptionHandlerKind.__C_specific_handler_noexcept:
                            s.WriteInline(new ScopeTable(dataChunk));
                            break;

                        #endregion
                        #region CxxFrameHandler

                        case WellKnownExceptionHandlerKind.__CxxFrameHandler:
                        case WellKnownExceptionHandlerKind.__CxxFrameHandler2:
                        case WellKnownExceptionHandlerKind.__CxxFrameHandler3:
                            //Value is an RVA to the FuncInfo. FuncInfo struct may or may not be right after its RVA
                            infoRVA = dataChunk.PeekInt32(0);

                            peFile = chunk.PEFile();

                            if (peFile.TryGetValueChunkFromSection(infoRVA, out infoChunk))
                                s.WriteInline(new RVA<FuncInfo>(infoRVA, infoChunk.AbsoluteOffset, new FuncInfo(infoChunk)), ViewKind.FuncInfoRva);
                            else
                                s.WriteInline(new RVA<FuncInfo>(infoRVA), ViewKind.FuncInfoRva);

                            break;

                        case WellKnownExceptionHandlerKind.__CxxFrameHandler4:
                            //Value is an RVA to the FuncInfo4. FuncInfo4 struct may or may not be right after its RVA
                            infoRVA = dataChunk.PeekInt32(0);

                            peFile = chunk.PEFile();

                            if (peFile.TryGetValueChunkFromSection(infoRVA, out infoChunk))
                                s.WriteInline(new RVA<FuncInfo4>(infoRVA, infoChunk.AbsoluteOffset, new FuncInfo4(infoChunk, functionAddress)), ViewKind.FuncInfo4Rva);
                            else
                                s.WriteInline(new RVA<FuncInfo4>(infoRVA), ViewKind.FuncInfo4Rva);

                            break;

                        #endregion

                        default:
                            throw new NotImplementedException($"Don't know how to handle {nameof(WellKnownExceptionHandlerKind)} '{ExceptionHandlerKind}'");
                    }
                }
                else if (((int) Flags & (int) UNW_FLAG.CHAININFO) != 0)
                {
                    s.WriteInline((RuntimeFunction) FunctionEntry!);
                }

                structWriter.EagerFields = s.ToArray();
            }
            finally
            {
                s.Dispose();
            }            
        }
    }
}
