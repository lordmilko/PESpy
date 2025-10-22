using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    //https://github.com/dotnet/runtime/blob/27b25483e06a14af2aaf6f6b6b9b6e527a3b69bf/src/coreclr/nativeaot/Runtime/DebugHeader.cpp#L65

    /// <summary>
    /// Describes the debug information in a NativeAOT executable.<para/>
    /// Most of the information present in this structure is only valid in a live debug target.
    /// </summary>
    public class DotNetRuntimeDebugHeader : IValue, IViewable
    {
        public int AotSignature = 0x48444E44; //DNDH
        private const int CookieOffset = 0;
        private const int MajorVersionOffset = 4;
        private const int MinorVersionOffset = 6;
        private const int FlagsOffset = 8;
        private const int ReservedPadding1Offset = 12;
        private const int DebugTypeEntriesOffset = 16;
        private int GlobalValueEntriesOffset => 16 + chunk.PointerSize;

        public int Cookie => chunk.PeekInt32(CookieOffset);

        public short MajorVersion => chunk.PeekInt16(MajorVersionOffset);

        public short MinorVersion => chunk.PeekInt16(MinorVersionOffset);

        public int Flags => chunk.PeekInt32(FlagsOffset);

        public int ReservedPadding1 => chunk.PeekInt32(ReservedPadding1Offset);

        //This information seems to be populated at runtime by PopulateDebugHeaders()

        #region DebugTypeEntries

        private VA<DebugTypeEntry[]> debugTypeEntries;

        public VA<DebugTypeEntry[]> DebugTypeEntries
        {
            get
            {
                if (debugTypeEntries.ListedAddress == 0)
                {
                    var peFile = chunk.PEFile();

                    var debugTypeEntriesAddress = (long) chunk.PeekPointer(DebugTypeEntriesOffset);

                    if (peFile.IsLoadedImage)
                    {
                        if (debugTypeEntriesAddress != 0)
                        {
                            var actualOffset = (int) (debugTypeEntriesAddress - peFile.OptionalHeader.ImageBase);

                            if (peFile.TryGetValueChunkFromSection(actualOffset, out var valueChunk))
                            {
                                using var results = new PooledList<DebugTypeEntry>();

                                var read = 0;
                                var ptrSize = chunk.PointerSize;

                                while (true)
                                {
                                    //The last entry is null

                                    var entry = new DebugTypeEntry(valueChunk.Slice(read));

                                    results.Add(entry);

                                    if (entry.TypeName.ListedAddress == 0)
                                        break;

                                    read += (2 * ptrSize) + 8;
                                }

                                debugTypeEntries = new VA<DebugTypeEntry[]>(debugTypeEntriesAddress, actualOffset, results.ToArray());
                            }
                            else
                                debugTypeEntries = new VA<DebugTypeEntry[]>(debugTypeEntriesAddress);
                        }
                        else
                            debugTypeEntries = default;
                    }
                    else
                        debugTypeEntries = new VA<DebugTypeEntry[]>(debugTypeEntriesAddress);
                }

                return debugTypeEntries;
            }
        }

        #endregion
        #region GlobalValueEntries

        private VA<GlobalValueEntry[]> globalValueEntries;

        public VA<GlobalValueEntry[]> GlobalValueEntries
        {
            get
            {
                if (globalValueEntries.ListedAddress == 0)
                {
                    var peFile = chunk.PEFile();

                    var globalEntriesAddress = (long) chunk.PeekPointer(GlobalValueEntriesOffset);

                    if (peFile.IsLoadedImage)
                    {
                        if (globalEntriesAddress != 0)
                        {
                            var actualOffset = (int) (globalEntriesAddress - peFile.OptionalHeader.ImageBase);

                            if (peFile.TryGetValueChunkFromSection(actualOffset, out var valueChunk))
                            {
                                using var results = new PooledList<GlobalValueEntry>();

                                var read = 0;
                                var ptrSize = chunk.PointerSize;

                                while (true)
                                {
                                    //The last entry is null

                                    var entry = new GlobalValueEntry(valueChunk.Slice(read));

                                    results.Add(entry);

                                    if (entry.Name.ListedAddress == 0)
                                        break;

                                    read += (2 * ptrSize);
                                }

                                globalValueEntries = new VA<GlobalValueEntry[]>(globalEntriesAddress, actualOffset, results.ToArray());
                            }
                            else
                                globalValueEntries = new VA<GlobalValueEntry[]>(globalEntriesAddress);
                        }
                        else
                            globalValueEntries = default;
                    }
                    else
                        globalValueEntries = new VA<GlobalValueEntry[]>(globalEntriesAddress);
                }

                return globalValueEntries;
            }
        }

        #endregion

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize => FixedStructSize + (2 * chunk.PointerSize);

        internal const int FixedStructSize =
            sizeof(int) + //Cookie
            sizeof(short) + //MajorVersion
            sizeof(short) + //MinorVersion
            sizeof(int) + //Flags
            sizeof(int); //ReservePadding1

        private readonly MemoryChunk chunk;

        internal DotNetRuntimeDebugHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            if (DebugTypeEntries.IsValid)
            {
                using var r = writer.CreateRegion(
                    DebugTypeEntries.ActualOffset,
                    FixedStructSize,
                    "DebugTypeEntries",
                    ViewKind.DebugTypeEntries,
                    false
#if DEBUG
                    , DebugTypeEntries.ListedAddress
#endif
                );

                r.WriteValues(DebugTypeEntries.Value);
            }

            if (GlobalValueEntries.IsValid)
            {
                using var r = writer.CreateRegion(
                    GlobalValueEntries.ActualOffset,
                    FixedStructSize + chunk.PointerSize,
                    "GlobalValueEntries",
                    ViewKind.GlobalValueEntries,
                    false
#if DEBUG
                    , GlobalValueEntries.ListedAddress
#endif
                );

                r.WriteValues(GlobalValueEntries.Value);
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.DotNetRuntimeDebugHeader, this, ViewKind.DotNetRuntimeDebugHeader, StructSize);

        int IViewable.NumChildren() => 7;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Cookie), CookieOffset, Cookie);
                    break;

                case 1:
                    structWriter.WriteField(nameof(MajorVersion), MajorVersionOffset, MajorVersion);
                    break;

                case 2:
                    structWriter.WriteField(nameof(MinorVersion), MinorVersionOffset, MinorVersion);
                    break;

                case 3:
                    structWriter.WriteField(nameof(Flags), FlagsOffset, Flags);
                    break;

                case 4:
                    structWriter.WriteField(nameof(ReservedPadding1), ReservedPadding1Offset, ReservedPadding1);
                    break;

                case 5:
                    structWriter.WriteVAPointerField(nameof(DebugTypeEntries), DebugTypeEntriesOffset, DebugTypeEntries);
                    break;

                case 6:
                    structWriter.WriteVAPointerField(nameof(GlobalValueEntries), GlobalValueEntriesOffset, GlobalValueEntries);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
