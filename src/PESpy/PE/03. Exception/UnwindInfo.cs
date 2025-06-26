using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
using RVA = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="UNWIND_INFO"/> structure.
    /// </summary>
    public class UnwindInfo : IValue, IViewable
    {
#if PEFAST
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
                    var unwindCodes = new List<UnwindCode>();

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
#else
        public byte Version { get; init; }

        public UNW_FLAG Flags { get; init; }

        public byte SizeOfProlog { get; init; }

        public byte CountOfCodes { get; init; }

        public byte FrameRegister { get; init; }

        public byte FrameOffset { get; init; }

        public UnwindCode[] UnwindCode { get; }
#endif
        public int ExceptionHandler { get; }

        public RuntimeFunction FunctionEntry { get; } //UNWIND_INFO says that it's an int, but it's really a RUNTIME_FUNCTION
        public IValue? ExceptionData { get; }

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

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

#if PEFAST
        private readonly MemoryChunk chunk;

        internal UnwindInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;

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

                if (ByteMatcher.TryMatch(peFile, (RVA) ExceptionHandler, out var kind))
                {
                    var dataChunk = chunk.Slice(extraDataStart + 4);

                    switch (kind)
                    {
                        case ByteMatchKind.__GSHandlerCheck:
                            //Data is an Int32, whose meaning is unknown. Possibly _GS_HANDLER_DATA, but GS_HANDLER_DATA seems to be at least two bytes (alignment is optional). AlignedBaseOffset would also need to be optional for that to work

                            ExceptionData = new RawValue<int>(dataChunk.AbsoluteOffset, dataChunk.PeekInt32(0));
                            break;

                        case ByteMatchKind.__C_specific_handler:
                        case ByteMatchKind.__C_specific_handler_noexcept:
                            ExceptionData = new ScopeTable(dataChunk);
                            break;

                        case ByteMatchKind.__CxxFrameHandler:
                        {
                            //Value is an RVA to the FuncInfo. Not typically right after the unwind info
                            var infoRVA = (RVA) dataChunk.PeekInt32(0);

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
                            var infoRVA = (RVA) dataChunk.PeekInt32(0);

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

                FunctionEntry = new RuntimeFunction(chunk.Slice(extraDataStart));
            }
        }
#else
        internal UnwindInfo(IFileReader reader, PEFile peFile, in ImageDataDirectory exceptionDirectory, ExceptionHandlerContext context)
        {
            Offset = (RawOffset) reader.Position;

            var versionAndFlags = reader.ReadByte();

            Version = (byte) (versionAndFlags & 0x7); //bottom 3 bits
            Debug.Assert(Version == 1 || Version == 2 || Version == 3);
            Flags = (UNW_FLAG) ((versionAndFlags >> 3) & 0x1f); //top 5 bits
            SizeOfProlog = reader.ReadByte();
            CountOfCodes = reader.ReadByte();

            var frameRegisterAndOffset = reader.ReadByte();

            FrameRegister = (byte) (frameRegisterAndOffset & 0x0F);

            //The actual frame offset is this 16 * FrameOffset
            FrameOffset = (byte) ((frameRegisterAndOffset & 0xF0) >> 4);

            //For alignment purposes, this array always has an even number of entries, and the final entry is
            //potentially unused. In that case, the array is one longer than indicated by the count of unwind
            //codes field
            var alignedCount = (CountOfCodes + 1) & ~1;

            //CountOfCodes represents a count of "slots" that follow. A "slot" is a 16 bit value
            //that either contains an UNWIND_CODE, or some additional data relating to the previous
            //UNWIND_CODE. Thus, we don't know how many top level "codes" we'll actually have
            var unwindCodes = new List<UnwindCode>();

            for (var i = 0; i < CountOfCodes; i++)
            {
                var offset = (int) reader.Position;

                var codeOffset = reader.ReadByte();

                var unwindOpAndInfo = reader.ReadByte();

                var unwindOp = (UWOP) (unwindOpAndInfo & 0x0F);
                var opInfo = (byte) ((unwindOpAndInfo & 0xF0) >> 4);

                //https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64?view=msvc-170#struct-unwind_code
                unwindCodes.Add(GetUnwindCodeInfo(offset, codeOffset, unwindOp, opInfo, reader, ref i));
            }

            if (alignedCount > CountOfCodes)
            {
                var offset = (int) reader.Position;

                var codeOffset = reader.ReadByte();
                var unwindOpAndInfo = reader.ReadByte();

                var unwindOp = (UWOP) (unwindOpAndInfo & 0x0F);
                var opInfo = (byte) ((unwindOpAndInfo & 0xF0) >> 4);

                unwindCodes.Add(new UnwindCode.NullUnwindCode(offset, codeOffset, (UWOP) unwindOp, opInfo));
            }

            UnwindCode = unwindCodes.ToArray();

            if (((int) Flags & (int) UNW_FLAG.EHANDLER) != 0 || ((int)Flags & (int)UNW_FLAG.UHANDLER) != 0)
            {
                ExceptionHandler = reader.ReadInt32();

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

                var oldOffset = (RawOffset) reader.Position;

                if (ByteMatcher.TryMatch(reader, peFile, (RVA) ExceptionHandler, context, out var kind))
                {
                    reader.Seek(oldOffset);

                    switch (kind)
                    {
                        case ByteMatchKind.__GSHandlerCheck:
                            //Data is an Int32, whose meaning is unknown. Possibly _GS_HANDLER_DATA, but GS_HANDLER_DATA seems to be at least two bytes (alignment is optional). AlignedBaseOffset would also need to be optional for that to work
                            
                            ExceptionData = new RawValue<int>(oldOffset, reader.ReadInt32());
                            break;

                        case ByteMatchKind.__C_specific_handler:
                        case ByteMatchKind.__C_specific_handler_noexcept:
                            ExceptionData = new ScopeTable(reader);
                            break;

                        case ByteMatchKind.__CxxFrameHandler:
                        {
                            //Value is an RVA to the FuncInfo. Not typically right after the unwind info
                            var infoRVA = (RVA) reader.ReadInt32();

                            if (peFile.TryGetOffset(infoRVA, out var infoOffset))
                            {
                                reader.Seek(infoOffset);
                                ExceptionData = new RVA<FuncInfoV1>(infoRVA, infoOffset, new FuncInfoV1(reader, peFile));
                            }
                            else
                                ExceptionData = new RVA<FuncInfoV1>(infoRVA);

                            break;
                        }

                        case ByteMatchKind.__CxxFrameHandler3:
                        {
                            //Value is an RVA to the FuncInfo. Not typically right after the unwind info
                            var infoRVA = (RVA) reader.ReadInt32();

                            if (peFile.TryGetOffset(infoRVA, out var infoOffset))
                            {
                                reader.Seek(infoOffset);
                                ExceptionData = new RVA<FuncInfo>(infoRVA, infoOffset, new FuncInfo(reader, peFile));
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

                            http://www.hexblog.com/wp-content/uploads/2012/06/Recon-2012-Skochinsky-Compiler-Internals.pdf
                            ExceptionData = new ScopeTable(reader);
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

                FunctionEntry = new RuntimeFunction(reader, peFile, exceptionDirectory, context);
            }
        }
#endif

#if PEFAST
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
                    if (Version == 2)
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
#else
        private UnwindCode GetUnwindCodeInfo(
            int offset,
            byte codeOffset,
            UWOP unwindOp,
            byte opInfo,
            IFileReader reader,
            ref int i)
        {
            switch (unwindOp)
            {
                case UWOP.PUSH_NONVOL:
                    return new UnwindCode.PushNonVolatile(offset, codeOffset, (X64Register) opInfo);

                case UWOP.ALLOC_LARGE:
                    if (opInfo == 0)
                    {
                        //If the operation info equals 0, then the size of the allocation divided by 8 is recorded in the next slot,
                        //allowing an allocation up to 512K - 8
                        var size = reader.ReadUInt16(); //Slots are 16

                        i++;
                        return new UnwindCode.AllocLarge(offset, codeOffset, opInfo, size);
                    }
                    else
                    {
                        //If the operation info equals 1, then the unscaled size of the allocation is recorded in the next two slots
                        //in little-endian format, allowing allocations up to 4GB - 8

                        var sizeLo = reader.ReadInt16();
                        var sizeHi = reader.ReadInt16();

                        var size = sizeLo + (sizeHi << 16);

                        i += 2;
                        return new UnwindCode.AllocLarge(offset, codeOffset, opInfo, size);
                    }

                case UWOP.ALLOC_SMALL:
                    //The size of the allocation is the operation info field * 8 + 8, allowing allocations from 8 to 128 bytes
                    return new UnwindCode.AllocSmall(offset, codeOffset, opInfo * 8 + 8);

                case UWOP.SET_FPREG:
                    //The offset is equal to the Frame Register offset (scaled) field in the UNWIND_INFO * 16
                    return new UnwindCode.SetFpReg(offset, codeOffset, (X64Register) FrameRegister, opInfo, FrameOffset * 16);

                case UWOP.SAVE_NONVOL:
                    i++;
                    return new UnwindCode.SaveNonVolatile(offset, codeOffset, (X64Register) opInfo, reader.ReadUInt16());

                case UWOP.SAVE_NONVOL_FAR:
                    //The operation info is the number of the register. The unscaled stack offset is recorded in the next two unwind operation code slots,
                    i += 2;
                    return new UnwindCode.SaveNonVolatileFar(offset, codeOffset, (X64Register) opInfo, reader.ReadInt16() + (reader.ReadInt16() << 16));

                case UWOP.UWOP_EPILOG:
                    //Contrary to what https://www.winehq.org/pipermail/wine-devel/2019-August/149669.html says,
                    //regardless of whether opInfo was 0 or 1 it didn't seem like there was another slot after this one
                    //that needed to be read
                    if (Version == 2)
                        return new UnwindCode.Epilog(offset, codeOffset, opInfo);
                    else
                        throw new InvalidOperationException($"Don't know how to handle UWOP_EPILOG when using version {Version}");

                case UWOP.SAVE_XMM128:
                    i++;
                    return new UnwindCode.SaveXmm128(offset, codeOffset, (X64Register) opInfo, reader.ReadUInt16());

                case UWOP.SAVE_XMM128_FAR:
                    i += 2;
                    return new UnwindCode.SaveXmm128Far(offset, codeOffset, (X64Register) opInfo, reader.ReadInt16() + (reader.ReadInt16() << 16));

                case UWOP.PUSH_MACHFRAME:
                    return new UnwindCode.PushMachFrame(offset, codeOffset, opInfo);

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(UWOP)} '{unwindOp}'.");
            }
        }
#endif

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
            writer.NewStruct(nameof(UNWIND_INFO), this, ViewKind.UnwindInfo, StructSize);

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
                s.WriteInline(FunctionEntry);
            }

            return s.ToArray();
        }
    }
}
