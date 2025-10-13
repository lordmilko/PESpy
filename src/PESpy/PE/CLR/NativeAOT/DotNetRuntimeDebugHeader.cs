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

        public int Cookie => chunk.PeekInt32(0);

        public short MajorVersion => chunk.PeekInt16(4);

        public short MinorVersion => chunk.PeekInt16(6);

        public int Flags => chunk.PeekInt32(8);

        public int ReservedPadding1 => chunk.PeekInt32(12);

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

                    var debugTypeEntriesAddress = (long) chunk.PeekPointer(16);

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

                    var globalEntriesAddress = (long) chunk.PeekPointer(16 + chunk.PointerSize);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Cookie), Cookie);
            s.WriteField(nameof(MajorVersion), MajorVersion);
            s.WriteField(nameof(MinorVersion), MinorVersion);
            s.WriteField(nameof(Flags), Flags);
            s.WriteField(nameof(ReservedPadding1), ReservedPadding1);
            s.WriteVAPointerField(nameof(DebugTypeEntries), DebugTypeEntries);
            s.WriteVAPointerField(nameof(GlobalValueEntries), GlobalValueEntries);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
