using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using ClrDebug.DIA;
using ClrDebug.PDB;
﻿using PESpy.View;

namespace PESpy.PDB
{
    public static partial class MsfStream
    unsafe struct MemoryBuffer
    internal unsafe struct MemoryBuffer
    {
        public readonly byte* Buffer;
        public int Offset;
        public readonly int Length;

        internal MemoryBuffer(byte* buffer, int length)
        {
            Buffer = buffer;
            Offset = 0;
            Length = length;
        }
    }

    public static unsafe partial class MsfStream
    {
        public class PSGSI : GSI, IValue, IViewable, IDisposable
        {
            //The base chunk already has the PSGSIHDR sliced from it
            private int AddressMapOffset
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => psgsiHdr.cbSymHash;
            }

            private int ThunkMapOffset
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => AddressMapOffset + psgsiHdr.cbAddrMap;
            }

            private int SectionMapOffset
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ThunkMapOffset + (PSGsiHdr.nThunks * sizeof(int));
            }

            private readonly PSGSIHDR psgsiHdr;

            public ref readonly PSGSIHDR PSGsiHdr => ref psgsiHdr;

            //PSGSI contains two ways of retrieving symbols: via the GSI hash records, and via the address map.
            //The address map simply contains a list of offsets into snSymRecs, which is the same as what the hash
            //records do. In my test on ntdll, the address map had the same number of entries as the hash records
            public NativeSpan<int> AddressMap => chunk.PeekNativeSpan<int>(AddressMapOffset, PSGsiHdr.cbAddrMap / sizeof(int));

            //We expect that AddressMapSymbols should contain the same records as GSI.Symbols, but the big difference is that
            //these are already sorted
            public AddressMapSymTypeList? AddressMapSymbols { get; }

            /// <summary>
            /// Gets the RVAs of all functions in the PE File that have a thunk that points to them.<para/>
            /// The order of thunks in the PE File matches the order of RVAs in this list. Thunks are commonly found
            /// in Debug builds, which can be used to facilitate EnC. A thunk will typically contain a simple jump to
            /// the target function. The size of each thunk depends on the CPU architecture in use. The offset of the thunk
            /// that corresponds to an RVA at a given index in this list can be found by multiplying its index by
            /// <see cref="PSGSIHDR.cbSizeOfThunk"/> and adding this to <see cref="PSGSIHDR.offThunkTable"/> in
            /// <see cref="PSGSIHDR.isectThunkTable"/>.
            /// </summary>
            public NativeSpan<int> ThunkMap => chunk.PeekNativeSpan<int>(ThunkMapOffset, PSGsiHdr.nThunks);

            private ThunkEntry[]? thunkEntries;

            /// <summary>
            /// Gets the thunks described by this globals stream.<para/>
            /// For each item in <see cref="ThunkMap"/>, returns a pair of values containing the fake thunk symbol described
            /// by the thunk map, and the target symbol that that thunk jumps to.
            /// </summary>
            public ThunkEntry[]? ThunkEntries
            {
                get
                {
                    if (thunkEntries == null)
                    {
                        var addressMapSymbols = AddressMapSymbols;

                        if (addressMapSymbols == null)
                            return null;

                        var thunkMap = ThunkMap;

                        if (thunkMap.Length == 0)
                            return null;

                        var result = new ThunkEntry[thunkMap.Length];

                        var thunkSectionNumber = psgsiHdr.isectThunkTable;

                        for (var i = 0; i < thunkMap.Length; i++)
                        {
                            var targetRVA = thunkMap[i];

                            GetTargetSectionForRVA(targetRVA, out var targetSectionNumber, out var targetRelativeOffset);
                            var thunkRelativeOffset = psgsiHdr.offThunkTable + (i * psgsiHdr.cbSizeOfThunk);

                            var thunkSymType = GetFakeThunkSymbol(targetSymType, thunkRelativeOffset, thunkSectionNumber, targetDisplacement);

                            result[i] = new ThunkEntry(thunkSymType, (targetSymType, targetDisplacement));
                        }

                        thunkEntries = result;
                    }

                    return thunkEntries;
                }
            }

            /* The section map is read in PSGSI1::readThunkMap. This method bails out early if it sees that nThunks is 0.
             * However, it's possible to have a dodgy PDB wherein nThunks is 0 but nSects is _not_ 0, and so many sections
             * are listed that it takes you past the end of the PDB! Attempting to call PSGSI1::getEnumThunk on such a PDB
             * will cause a crash, as readThunkMap still returns TRUE when nThunks is 0, which causes an AV when getEnumThunk
             * then tries to read into bufSectMap, which is empty. The section map is tightly connected with the thunk map,
             * with PSGSI1::pbInThunkTable calling offThunkMap followed by mapOff. As such, we'll say that if nThunks is 0,
             * don't trust whatever is listed in nSects */
            public unsafe NativeSpan<SO> SectionMap
            {
                get
                {
                    if (PSGsiHdr.nThunks == 0)
                        return default;

                    return chunk.PeekNativeSpan<SO>(SectionMapOffset, PSGsiHdr.nSects);
                }
            }

            private SYMTYPE*[]? fakeThunkSymbols;
            private MemoryBuffer[]? fakeThunkStorage;

            internal unsafe PSGSI(in MemoryChunk chunk) : base(chunk.Slice(PSGSIHDR.StructSize), chunk.PeekInt32(0)) //cbSymHash
            {
                psgsiHdr = new PSGSIHDR(chunk);

                //Following the PSGSIHDR we have the exact same hash info that is found in GSI. This is read
                //by the base GSI ctor

                if (psgsiHdr.cbAddrMap > 0)
                {
                    var pdbFile = chunk.PDBFile();

                    if (!pdbFile.TryGetStreamChunk(pdbFile.DBI!.DbiHdr.snSymRecs, out var symbolChunk))
                        throw new InvalidOperationException("Couldn't retrieve section for DbiHdr.snSymRecs for global symbols");

                    AddressMapSymbols = new AddressMapSymTypeList(AddressMap, symbolChunk.Pointer);
                }
            //Given the relative offset of a thunk, get the RVA of the function it points to
            //PSGSI1::offThunkMap + PSGSI1::iThunk
            public int GetTargetRVAForThunkOffset(int thunkRelativeOffset)
            {
                var thunkIndex = GetThunkIndex(thunkRelativeOffset);

                return ThunkMap[thunkIndex];
            }

            //The thunk table may not start at the exact beginning of the section its in.
            //Then, each thunk is a fixed size (e.g. a 5 byte jmp)
            //PSGSI1::iThunk
            internal int GetThunkIndex(int thunkRelativeOffset) => (thunkRelativeOffset - psgsiHdr.offThunkTable) / psgsiHdr.cbSizeOfThunk;

            public bool TryGetTargetRVAForThunkRVA(int thunkRVA, out int targetRVA)
            {
                if (chunk.PDBFile().TryGetSectionAndOffset(thunkRVA, out var thunkSectionNumber, out var thunkRelativeOffset))
                {
                    if (!IsAddressInThunkTable(thunkSectionNumber, thunkRelativeOffset))
                    {
                        //Don't know what this is, but it's not a thunk!
                        targetRVA = default;
                        return false;
                    }

                    targetRVA = GetTargetRVAForThunkOffset(thunkRelativeOffset);
                    return true;
                }

                targetRVA = default;
                return false;
            }
            //Gets whether the given isect + off points to a location that exists inside the thunk table (the region where all the thunks are)
            //PSGSI1::fInThunkTable
            public bool IsAddressInThunkTable(int isect, int off) => off >= psgsiHdr.offThunkTable && off < (psgsiHdr.offThunkTable + (psgsiHdr.cbSizeOfThunk * psgsiHdr.nThunks)) && isect == psgsiHdr.isectThunkTable;
            //Given an RVA, gets the section index and relative offset within that section.
            //This is kind of similar to analyzing the section headers, but there's no guarantee those will exist
            //PSGSI1::mapOff
            public void GetTargetSectionForRVA(int rva, out ushort sectionNumber, out int relativeOffset)
            {
                int i = 0;

                var sectionMap = SectionMap;

                SO item = default;

                for (; i < sectionMap.Length - 1; i++)
                {
                    item = sectionMap[i];

                    if (rva >= item.off && rva < sectionMap[i + 1].off)
                    {
                        sectionNumber = item.isect;
                        relativeOffset = rva - item.off;
                        return;
                    }
                }

                item = sectionMap[i];

                //Just return the last one. I think it's assume there'll be at least 1 section
                sectionNumber = item.isect;
                relativeOffset = rva - item.off;
            }

            //PSGSI1::pbInThunkTable
            public bool TryGetThunkSymbol(int thunkRelativeOffset, int thunkSectionNumber, out SymType symType, out int displacement)
            {
                if (AddressMapSymbols == null)
                {
                    symType = default;
                    displacement = default;
                    return false;
                }

                symType = default;
                displacement = default;

                if (!IsAddressInThunkTable(thunkSectionNumber, thunkRelativeOffset))
                    return false; //Not a thunk symbol

                //thunkRelativeOffset may not be aligned to the beginning of the actual thunk. I think that PDB1 inadvertantly handles this in PSGSI1::EnumPubsByAddr::get
                var alignedThunkRelativeOffset = (GetThunkIndex(thunkRelativeOffset) * psgsiHdr.cbSizeOfThunk) + psgsiHdr.offThunkTable;
                var disp = thunkRelativeOffset - alignedThunkRelativeOffset; //I don't understand the logic of PSGSI1::pbInThunkTable; it seems to always set pdisp to 0, but DbgHelp shows an offset after the name anyway. Maybe this is something special DbgHelp does
                thunkRelativeOffset = alignedThunkRelativeOffset;

                var target = GetTargetRVAForThunkOffset(thunkRelativeOffset);

                //Check that the target of this thunk is also not a thunk
                GetTargetSectionForRVA(target, out var targetSectionNumber, out var targetRelativeOffset);

                if (IsAddressInThunkTable(targetSectionNumber, targetRelativeOffset))
                    return false;

                //Get the nearest symbol to the thunk's target
                if (!AddressMapSymbols.GetNearestSymbol(targetRelativeOffset, targetSectionNumber, out var targetSymType, out var targetDisplacement))
                    return false;

                //Synthesize a fake symbol for thunk

                //bufThunkSym provides temporary storage. Each new thunk that is allocated overwrites this, so it's up to the caller
                //to create their own copy of the symbol. For each thunk, a fake Incremental Link Table symbol is created.
                symType = GetFakeThunkSymbol(targetSymType, thunkRelativeOffset, thunkSectionNumber, targetDisplacement);
                displacement = disp; //The displacement against the thunk (if the address was not cbSizeOfThunk aligned)
                return true;
            }

            private SymType GetFakeThunkSymbol(SymType targetSymType, int thunkRelativeOffset, int thunkSectionNumber, int targetDisplacement)
            {
                if (fakeThunkSymbols == null)
                {
                    //Ensure that someone else doesn't overwrite it while we're potentially trying to insert into the one we newly created
                    Interlocked.CompareExchange(ref fakeThunkSymbols, new SYMTYPE*[psgsiHdr.nThunks], null);
                }

                var thunkIndex = GetThunkIndex(thunkRelativeOffset);

                SYMTYPE* result = fakeThunkSymbols[thunkIndex];

                if (result == default)
                {
                    //First, how big is the name going to be
                    var extraLength = 5 + 11 + 2; //"@ILT+" is 5, %d is a signed integer which may be up to 11 characters if negative, and the () is 2

                    if (targetDisplacement != 0)
                    {
                        extraLength += 1 + 11; //1 for the + and 11 for the max signed integer
                    }
                    fakeThunkSymbols[thunkIndex] = result;
                }

                return result;
            }

            private SYMTYPE* WritePubSym32Thunk(SymType targetSymType, int thunkRelativeOffset, int thunkSectionNumber, int targetDisplacement, int extraLength, bool utf8)
            {
                var pubSym32 = (PubSym32) targetSymType;

                //This will be more than we actually need; once we've written the name, we'll see what we actually need
                var maxRecordLength = targetSymType.reclen + extraLength + Demangler.MaxSymbolName;

                var buffer = (PUBSYM32*) AcquireFakeThunkStorage(maxRecordLength);

                buffer->rectyp = pubSym32.rectyp;
                buffer->pubsymflags = pubSym32.pubsymflags;
                buffer->off = thunkRelativeOffset;
                buffer->seg = (ushort) thunkSectionNumber;

                var thunkNameLength = WriteFakeThunkName(
                    (byte*) buffer,
                    maxRecordLength,
                    PubSym32.FixedStructSize,
                    thunkRelativeOffset,
                    pubSym32.name,
                    targetDisplacement,
                    true
                );

                //Now we need to tell the symbol record how big its data actually comes to
                buffer->reclen = (ushort) (PubSym32.FixedStructSize + thunkNameLength);

                return (SYMTYPE*) buffer;
            }

            private SYMTYPE* AcquireFakeThunkStorage(int maxRecordLength)
            {
                while (true)
                {
                    var local = Volatile.Read(ref fakeThunkStorage);

                    if (local != null)
                    {
                        ref var currentBuffer = ref local[local.Length - 1];

                        //Try allocate some storage in the buffer
                        while (true)
                        {
                            var oldOffset = Volatile.Read(ref currentBuffer.Offset);
                            var newOffset = oldOffset + maxRecordLength;

                            if (newOffset >= currentBuffer.Length)
                                break; //We've run out of room. We need to allocate a new buffer

                            var previous = Interlocked.CompareExchange(ref currentBuffer.Offset, newOffset, oldOffset);

                            if (previous == oldOffset)
                                return (SYMTYPE*) (currentBuffer.Buffer + oldOffset); //Nobody changed the offset on us
                        }
                    }

                    //If we haven't returned prior to reaching this, we need to allocate some new storage
                    GrowFakeThunkStorage(maxRecordLength);
                }
            }

            private void GrowFakeThunkStorage(int requiredSize)
            {
                var current = Volatile.Read(ref fakeThunkStorage);

                var newCount = current == null ? 1 : current.Length + 1;

                var newStorage = new MemoryBuffer[newCount];

                if (current != null)
                    Array.Copy(current, newStorage, current.Length);

                var size = Math.Max(ushort.MaxValue, requiredSize);
                var buffer = (byte*) Marshal.AllocHGlobal(size);

                newStorage[newStorage.Length - 1] = new MemoryBuffer(buffer, size);

                var previous = Interlocked.CompareExchange(ref fakeThunkStorage, newStorage, current);

                if (previous == current)
                {
                    //We won the race. Register this storage with the PDB so that symbols can resolve RVAs, etc without holding
                    //a direct reference to the PDBFile
                    var pdbFile = chunk.PDBFile();
                    SymbolMemoryTracker.RegisterPDBSymbolMemory(pdbFile.globalBlock, buffer, size);
                }
                else
                {
                    //Someone raced with us. Free our allocated buffer, return to the caller, and have them retry with the new storage
                    //that was allocated by the other thread
                    Marshal.FreeHGlobal((IntPtr) buffer);
                }
            }

            private int WriteFakeThunkName(
                byte* buffer,
                int maxRecordLength,
                int fixedStructSize,
                int thunkRelativeOffset,
                SymString targetName,
                int targetDisplacement,
                bool utf8)
            {
                var name = new Span<byte>(buffer + fixedStructSize, maxRecordLength - fixedStructSize);

                var i = 0;

                if (!utf8)
                    i++; //ST strings are length prefixed; leave room for this

                //Construct a name like @ILT+5(foo) where foo is the name of the symbol that this Incremental Linking Table
                //entry jumps to, and 5 is the offset within the thunk table at which this thunk resides

                name[i++] = (byte) '@';
                name[i++] = (byte) 'I';
                name[i++] = (byte) 'L';
                name[i++] = (byte) 'T';
                name[i++] = (byte) '+';

                var offsetInThunkTable = (thunkRelativeOffset - psgsiHdr.offThunkTable);
                var wroteOffsetInThunkTable = Utf8Formatter.TryFormat(offsetInThunkTable, name.Slice(i), out var offsetInThunkTableBytesWritten);

                Debug.Assert(wroteOffsetInThunkTable);
                i += offsetInThunkTableBytesWritten;

                name[i++] = (byte) '(';

                i += Demangler.ParseString(targetName, name.Slice(i), UNDNAME.UNDNAME_NAME_ONLY);

                if (targetDisplacement != 0)
                {
                    var wroteTargetDisplacement = Utf8Formatter.TryFormat(targetDisplacement, name.Slice(i), out var targetDisplacementBytesWritten);
                    i += targetDisplacementBytesWritten;
                }

                name[i++] = (byte) ')';

                if (utf8)
                    name[i++] = 0;
                else
                    name[0] = (byte) (i - 1); //Set the ST string length prefix. -1 because i skipped over index 0 which will store the length

                return i;
            }

            public bool TryGetNearestSymbol(int relativeOffset, int sectionNumber, out SymType symType, out int displacement)
            {
                if (AddressMapSymbols == null)
                {
                    symType = default;
                    displacement = default;
                    return false;
                }

                //If this address points to a thunk we need to synthesize a fake symbol for this
                if (TryGetThunkSymbol(relativeOffset, sectionNumber, out symType, out displacement))
                    return true;

                //Get the nearest symbol from the address map

                return AddressMapSymbols.GetNearestSymbol(relativeOffset, sectionNumber, out symType, out displacement);
            }

            protected override unsafe void WriteGlobals(ViewWriter writer)
            {
                //The base chunk already has the PSGSIHDR sliced from it

                writer.WriteGlobal(PSGsiHdr);

                base.WriteGlobals(writer);

                var addressMap = AddressMap;

                if (addressMap.Length > 0)
                {
                    var localThunk = chunk.Slice(AddressMapOffset);
                    writer.WriteGlobalField(localThunk.AbsoluteOffset, "Address Map", addressMap, psgsiHdr.cbAddrMap);
                }

                var thunkMap = ThunkMap;

                if (thunkMap.Length > 0)
                {
                    var localThunk = chunk.Slice(ThunkMapOffset);
                    writer.WriteGlobalField(localThunk.AbsoluteOffset, "Thunk Map", thunkMap, psgsiHdr.nThunks * sizeof(int));
                }

                var sectionMap = SectionMap;

                if (sectionMap.Length > 0)
                {
                    var localThunk = chunk.Slice(SectionMapOffset);
                    writer.WriteGlobalField(localThunk.AbsoluteOffset, "Section Map", sectionMap, psgsiHdr.nSects * sizeof(SO));
                }
            }

            public void Dispose()
            {
                if (fakeThunkStorage != null)
                {
                    for (var i = 0; i < fakeThunkStorage.Length; i++)
                    {
                        var item = fakeThunkStorage[i];
                        Marshal.FreeHGlobal((IntPtr) item.Buffer);
                    }

                    fakeThunkSymbols = default;
                }
            }
        }
    }
}
