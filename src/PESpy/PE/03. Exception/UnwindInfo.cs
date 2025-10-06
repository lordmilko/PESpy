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
        public byte Version => (byte) (versionAndFlags & 0x7); //bottom 3 bits

        public UNW_FLAG Flags => (UNW_FLAG) ((versionAndFlags >> 3) & 0x1f); //top 5 bits

        private int versionAndFlags => chunk.PeekByte(0);

        public byte SizeOfProlog => chunk.PeekByte(1);

        public byte CountOfCodes => chunk.PeekByte(2);

        private byte frameRegisterAndOffset => chunk.PeekByte(3);

        public byte FrameRegister => (byte) (frameRegisterAndOffset & 0x0F);

        //The actual frame offset is this 16 * FrameOffset
        public byte FrameOffset => (byte) ((frameRegisterAndOffset & 0xF0) >> 4);

        private UnwindCode[]? unwindCode;

        public UnwindCode[] UnwindCode
        {
            get
            {
                if (unwindCode == null)
                {
                    //For alignment purposes, this array always has an even number of entries, and the final entry is
                    //potentially unused. In that case, the array is one longer than indicated by the count of unwind
                    //codes field
                    var alignedCount = (CountOfCodes + 1) & ~1;

                    //CountOfCodes represents a count of "slots" that follow. A "slot" is a 16 bit value
                    //that either contains an UNWIND_CODE, or some additional data relating to the previous
                    //UNWIND_CODE. Thus, we don't know how many top level "codes" we'll actually have
                    using var unwindCodes = new PooledList<UnwindCode>();

                    for (var i = 0; i < CountOfCodes; i++)
                    {
                        var offset = 4 + (i * 2);
                        var codeOffset = chunk.PeekByte(offset);

                        var unwindOpAndInfo = chunk.PeekByte(offset + 1);

                        var unwindOp = (UWOP) (unwindOpAndInfo & 0x0F);
                        var opInfo = (byte) ((unwindOpAndInfo & 0xF0) >> 4);

                        //https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64?view=msvc-170#struct-unwind_code
                        unwindCodes.Add(GetUnwindCodeInfo(offset, codeOffset, unwindOp, opInfo, chunk.Slice(offset), ref i));
                    }

                    if (alignedCount > CountOfCodes)
                    {
                        var offset = 4 + (CountOfCodes * 2);
                        var codeOffset = chunk.PeekByte(offset);
                        var unwindOpAndInfo = chunk.PeekByte(offset + 1);

                        var unwindOp = (UWOP) (unwindOpAndInfo & 0x0F);
                        var opInfo = (byte) ((unwindOpAndInfo & 0xF0) >> 4);

                        unwindCodes.Add(new UnwindCode.NullUnwindCode(chunk.AbsoluteOffset + offset, codeOffset, (UWOP) unwindOp, opInfo));
                    }

                    unwindCode = unwindCodes.ToArray();
                }

                return unwindCode;
            }
        }

        public int ExceptionHandler { get; }

        //Needs to be indirected via a reference type, because we can't have UnwindInfo and RuntimeFunction both be reference types
        public ChainedRuntimeFunction? FunctionEntry { get; } //UNWIND_INFO says that it's an int, but it's really a RUNTIME_FUNCTION
        public IValue? ExceptionData { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize
        {
            get
            {
                var size = 4 + (((CountOfCodes + 1) & ~1) * 2); //4 fixed bytes + CountOfCodes aligned to an even number

                if (((int) Flags & (int) UNW_FLAG.EHANDLER) != 0 || ((int) Flags & (int) UNW_FLAG.UHANDLER) != 0)
                {
                    size += sizeof(int); //ExceptionHandler

                    var data = ExceptionData;

                    if (data != null)
                    {
                        if (data is RawValue<int>)
                            size += sizeof(int);
                        else if (data is ScopeTable s)
                            size += s.StructSize;
                        else if (data is RVA<FuncInfoV1> || data is RVA<FuncInfo> || data is RVA<FuncInfo4>)
                            size += sizeof(int);
                        else
                            throw new NotImplementedException();
                    }
                }
                else if (((int) Flags & (int) UNW_FLAG.CHAININFO) != 0)
                {
                    size += RuntimeFunction.StructSize; //FunctionEntry
                }

                return size;
            }
        }

        private readonly MemoryChunk chunk;

        internal UnwindInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            unwindCode = default;
            ExceptionHandler = default;
            FunctionEntry = default;
            ExceptionData = default;

            Debug.Assert(Version is 1 or 2 or 3);

#if STRESS_TEST
            _ = UnwindCode;
#endif

            //Following the unwind codes is either an ExceptionHandler, FunctionEntry or ExceptionData
            var extraDataStart = 4 + (((CountOfCodes + 1) & ~1) * 2); //4 fixed bytes + CountOfCodes aligned to an even number

            if (((int) Flags & (int) UNW_FLAG.EHANDLER) != 0 || ((int) Flags & (int) UNW_FLAG.UHANDLER) != 0)
            {
                ExceptionHandler = chunk.PeekInt32(extraDataStart);

                /* After the ExceptionHandler is the ExceptionData. Contrary to popular belief, the type of data pointed to by
                 * ExceptionHandler is not always a SCOPE_TABLE. Rather, the type of value pointed to by this RVA depends on the
                 * exception handler used. Other people have also had the same observation: https://reactos.org/wiki/Techwiki:SEH64
                 *
                 * The PE file does not need to know what kind of data is contained in the ExceptionData; it does not matter. Whatever it is,
                 * a pointer to it will be passed to the ExceptionHandler routine, which will know what to do with it.
                 *
                 * There are two ways to identify function that is pointed to by ExceptionHandler
                 * 1. Using symbols
                 * 2. Using byte signatures
                 *
                 * Using symbols will obviously be the most reliable, however it will also complicate our API design. At the same time,
                 * any time there's any minor variations that violate a pattern, we won't be able to get a match. As such, we support
                 * both strategies: if we can't match via patterns, fallback to using symbols. We opt to prefer patterns, as we don't
                 * want to force a symbol load if we don't have to. Fortunately, because the ExceptionTable will be lazily loaded,
                 * we can load a PEFile for the purposes of locating symbols prior to attempting to access the ExceptionTable. */

                var peFile = chunk.PEFile();

                if (ByteMatcher.TryMatch(peFile, ExceptionHandler, out var kind))
                {
                    var dataChunk = chunk.Slice(extraDataStart + 4);

                    //I think PEAnatomist determines the function type by looking at how much data is remaining.
                    //It then tries all possible heuristics on each piece of data

                    switch (kind)
                    {
                        case ByteMatchKind.__GSHandlerCheck:
                            //Data is an Int32, whose meaning is unknown. Possibly _GS_HANDLER_DATA, but GS_HANDLER_DATA seems to be at least two bytes (alignment is optional). AlignedBaseOffset would also need to be optional for that to work
                            //Maybe the data is the security cookie, or the offset to the cookie?
                            //PEAnatomist considers there to be a __GSHandlerCheck if the last 3 bits of the data value are not set
                            //and there's at least 4 bytes of data between this value and the value after it

                            ExceptionData = new RawValue<int>(dataChunk.AbsoluteOffset, dataChunk.PeekInt32(0));
                            break;

                        case ByteMatchKind.__C_specific_handler:
                        case ByteMatchKind.__C_specific_handler_noexcept:
                            ExceptionData = new ScopeTable(dataChunk);
                            break;

                        case ByteMatchKind.__CxxFrameHandler:
                        {
                            //Value is an RVA to the FuncInfo. Not typically right after the unwind info
                            var infoRVA = dataChunk.PeekInt32(0);

                            if (peFile.TryGetValueChunkFromSection(infoRVA, out var infoChunk))
                            {
                                ExceptionData = new RVA<FuncInfoV1>(infoRVA, infoChunk.AbsoluteOffset, new FuncInfoV1(infoChunk));
                            }
                            else
                                ExceptionData = new RVA<FuncInfoV1>(infoRVA);

                            break;
                        }

                        case ByteMatchKind.__CxxFrameHandler3:
                        {
                            //Value is an RVA to the FuncInfo. Not typically right after the unwind info
                            var infoRVA = dataChunk.PeekInt32(0);

                            if (peFile.TryGetValueChunkFromSection(infoRVA, out var infoChunk))
                            {
                                ExceptionData = new RVA<FuncInfo>(infoRVA, infoChunk.AbsoluteOffset, new FuncInfo(infoChunk));
                            }
                            else
                                ExceptionData = new RVA<FuncInfo>(infoRVA);

                            break;
                        }

                        case ByteMatchKind.__CxxFrameHandler4:
                        case ByteMatchKind.__GSHandlerCheck_EH4:
                        {
                            //Value is an RVA to the FuncInfo4 (which is typically right after the unwind info)
                            /*var infoRVA = (RVA) reader.ReadInt32();

                            if (peFile.TryGetOffset(infoRVA, out var infoOffset))
                            {
                                reader.Seek(infoOffset);
                                ExceptionData = new RVA<FuncInfo4>(infoRVA, infoOffset, new FuncInfo4(reader));
                            }
                            else
                                ExceptionData = new RVA<FuncInfo4>(infoRVA);*/
                            //FuncInfo4 is not supported yet

                            break;
                        }

                        case ByteMatchKind.__GSHandlerCheck_SEH: //Apparently it's a ScopeTable and the Int32 GS Data from GSHandlerCheck
                            //GSHandlerCheck_SEH_noexcept too?

                            //http://www.hexblog.com/wp-content/uploads/2012/06/Recon-2012-Skochinsky-Compiler-Internals.pdf
                            ExceptionData = new ScopeTable(dataChunk);
                            break;

                        //case ByteMatch.__GSHandlerCheck_EH: //Apparently it's an RVA to a FuncInfo and the Int32 GS Data from GSHandlerCheck

                        default:
                            throw new NotImplementedException($"Don't know how to handle {nameof(ByteMatchKind)} '{kind}'");
                    }
                }
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

                FunctionEntry = new ChainedRuntimeFunction(chunk.Slice(extraDataStart));
            }
        }

        private UnwindCode GetUnwindCodeInfo(
            int relativeOffset,
            byte codeOffset,
            UWOP unwindOp,
            byte opInfo,
            in MemoryChunk chunk,
            ref int i)
        {
            var absoluteOffset = chunk.AbsoluteOffset;

            switch (unwindOp)
            {
                case UWOP.PUSH_NONVOL:
                    return new UnwindCode.PushNonVolatile(absoluteOffset, codeOffset, (X64Register) opInfo);

                case UWOP.ALLOC_LARGE:
                    if (opInfo == 0)
                    {
                        //If the operation info equals 0, then the size of the allocation divided by 8 is recorded in the next slot,
                        //allowing an allocation up to 512K - 8
                        var size = chunk.PeekUInt16(relativeOffset + 2); //Slots are 16

                        i++;
                        return new UnwindCode.AllocLarge(absoluteOffset, codeOffset, opInfo, size);
                    }
                    else
                    {
                        //If the operation info equals 1, then the unscaled size of the allocation is recorded in the next two slots
                        //in little-endian format, allowing allocations up to 4GB - 8

                        var sizeLo = chunk.PeekInt16(relativeOffset + 2);
                        var sizeHi = chunk.PeekInt16(relativeOffset + 4);

                        var size = sizeLo + (sizeHi << 16);

                        i += 2;
                        return new UnwindCode.AllocLarge(absoluteOffset, codeOffset, opInfo, size);
                    }

                case UWOP.ALLOC_SMALL:
                    //The size of the allocation is the operation info field * 8 + 8, allowing allocations from 8 to 128 bytes
                    return new UnwindCode.AllocSmall(absoluteOffset, codeOffset, opInfo * 8 + 8);

                case UWOP.SET_FPREG:
                    //The offset is equal to the Frame Register offset (scaled) field in the UNWIND_INFO * 16
                    return new UnwindCode.SetFpReg(absoluteOffset, codeOffset, (X64Register) FrameRegister, opInfo, FrameOffset * 16);

                case UWOP.SAVE_NONVOL:
                    i++;
                    return new UnwindCode.SaveNonVolatile(absoluteOffset, codeOffset, (X64Register) opInfo, chunk.PeekUInt16(2));

                case UWOP.SAVE_NONVOL_FAR:
                    //The operation info is the number of the register. The unscaled stack offset is recorded in the next two unwind operation code slots,
                    i += 2;
                    return new UnwindCode.SaveNonVolatileFar(absoluteOffset, codeOffset, (X64Register) opInfo, chunk.PeekInt16(2) + (chunk.PeekInt16(4) << 16));

                case UWOP.UWOP_EPILOG:
                    //Contrary to what https://www.winehq.org/pipermail/wine-devel/2019-August/149669.html says,
                    //regardless of whether opInfo was 0 or 1 it didn't seem like there was another slot after this one
                    //that needed to be read
                    if (Version == 1 || Version == 2)
                        return new UnwindCode.Epilog(absoluteOffset, codeOffset, opInfo);
                    else
                        throw new InvalidOperationException($"Don't know how to handle UWOP_EPILOG when using version {Version}");

                case UWOP.SAVE_XMM128:
                    i++;
                    return new UnwindCode.SaveXmm128(absoluteOffset, codeOffset, (X64Register) opInfo, chunk.PeekUInt16(2));

                case UWOP.SAVE_XMM128_FAR:
                    i += 2;
                    return new UnwindCode.SaveXmm128Far(absoluteOffset, codeOffset, (X64Register) opInfo, chunk.PeekInt16(2) + (chunk.PeekInt16(4) << 16));

                case UWOP.PUSH_MACHFRAME:
                    return new UnwindCode.PushMachFrame(absoluteOffset, codeOffset, opInfo);

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(UWOP)} '{unwindOp}'.");
            }
        }

        public enum X64Register : byte
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
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.UNWIND_INFO, this, ViewKind.UnwindInfo, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            using (var b = s.WriteBitFields<byte>())
            {
                b.WriteField("Version", Version, 3);
                b.WriteField("Flags", Flags, 5);
            }

            s.WriteField(nameof(SizeOfProlog), SizeOfProlog);
            s.WriteField(nameof(CountOfCodes), CountOfCodes);

            using (var b = s.WriteBitFields<byte>())
            {
                b.WriteField(nameof(FrameRegister), FrameRegister, 4);
                b.WriteField(nameof(FrameOffset), FrameOffset, 4);
            }

            s.WriteInline(UnwindCode);

            //What follows next depends on the Flags

            if (((int) Flags & (int) UNW_FLAG.EHANDLER) != 0 || ((int) Flags & (int) UNW_FLAG.UHANDLER) != 0)
            {
                s.WriteField(nameof(ExceptionHandler), ExceptionHandler);

                if (ExceptionData is IViewable v)
                    s.WriteInline(v);
            }
            else if (((int) Flags & (int) UNW_FLAG.CHAININFO) != 0)
            {
                s.WriteInline((RuntimeFunction) FunctionEntry!);
            }

            return s.ToArray();
        }
    }
}
