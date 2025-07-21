using System;
using System.Collections.Generic;
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

#if PEFAST
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
#else
        public int Cookie { get; }
        public short MajorVersion { get; }
        public short MinorVersion { get; }
        public int Flags { get; }
        public int ReservedPadding1 { get; }

        public VA<DebugTypeEntry[]> DebugTypeEntries { get; }

        public VA<GlobalValueEntry[]> GlobalValueEntries { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;

        internal DotNetRuntimeDebugHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal DotNetRuntimeDebugHeader(IFileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            Cookie = reader.ReadInt32();

            if (Cookie != AotSignature)
                throw new NotImplementedException("Don't know how to handle AOT signature not being correct");

            MajorVersion = reader.ReadInt16();
            MinorVersion = reader.ReadInt16();
            Flags = reader.ReadInt32();
            ReservedPadding1 = reader.ReadInt32();

            var is32Bit = peFile.OptionalHeader.Magic == PEMagic.PE32;

            var debugTypeEntriesAddress = is32Bit ? reader.ReadUInt32() : reader.ReadInt64();
            var globalEntriesAddress = is32Bit ? reader.ReadUInt32() : reader.ReadInt64();

            //This information seems to be populated at runtime by PopulateDebugHeaders()

            //We take a shortcut in resolving these values, in that we assume they must be loaded so don't need to lookup which section they're in.
            //This shortcut falls apart if we start trying to read these in an unloaded image. However, as stated above, these values are only
            //populated at runtime
            if (peFile.IsLoadedImage)
            {
                if (debugTypeEntriesAddress != 0)
                {
                    var actualOffset = (int) (debugTypeEntriesAddress - peFile.OptionalHeader.ImageBase);
                    reader.Seek(actualOffset);

                    using var debugTypeEntries = new PooledList<DebugTypeEntry>();

                    while (true)
                    {
                        //The last entry is null

                        var entry = new DebugTypeEntry(reader, peFile, is32Bit);

                        debugTypeEntries.Add(entry);

                        if (entry.TypeName.ListedAddress == 0)
                            break;
                    }

                    DebugTypeEntries = new VA<DebugTypeEntry[]>(debugTypeEntriesAddress, actualOffset, debugTypeEntries.ToArray());
                }
                else
                    DebugTypeEntries = new VA<DebugTypeEntry[]>(debugTypeEntriesAddress);

                if (globalEntriesAddress != 0)
                {
                    var actualOffset = (int) (globalEntriesAddress - peFile.OptionalHeader.ImageBase);
                    reader.Seek(actualOffset);

                    using var globalValueEntries = new PooledList<GlobalValueEntry>();

                    while (true)
                    {
                        //The last entry is null

                        var entry = new GlobalValueEntry(reader, peFile, is32Bit);

                        globalValueEntries.Add(entry);

                        if (entry.Name.ListedAddress == 0)
                            break;
                    }

                    GlobalValueEntries = new VA<GlobalValueEntry[]>(globalEntriesAddress, actualOffset, globalValueEntries.ToArray());
                }
                else
                    GlobalValueEntries = new VA<GlobalValueEntry[]>(globalEntriesAddress);
            }
            else
            {
                DebugTypeEntries = new VA<DebugTypeEntry[]>(debugTypeEntriesAddress);
                GlobalValueEntries = new VA<GlobalValueEntry[]>(globalEntriesAddress);
            }
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(DotNetRuntimeDebugHeader), this, ViewKind.DotNetRuntimeDebugHeader);

            s.WriteField(nameof(Cookie), Cookie);
            s.WriteField(nameof(MajorVersion), MajorVersion);
            s.WriteField(nameof(MinorVersion), MinorVersion);
            s.WriteField(nameof(Flags), Flags);
            s.WriteField(nameof(ReservedPadding1), ReservedPadding1);
            s.WritePointerField(nameof(DebugTypeEntries), DebugTypeEntries.ListedAddress);
            s.WritePointerField(nameof(GlobalValueEntries), GlobalValueEntries.ListedAddress);

            if (DebugTypeEntries.IsValid)
            {
                using var r = viewWriter.CreateRegion(DebugTypeEntries.ActualOffset, "DebugTypeEntries", ViewKind.DebugTypeEntries);

                r.WriteValues(DebugTypeEntries.Value);
            }

            if (GlobalValueEntries.IsValid)
            {
                using var r = viewWriter.CreateRegion(GlobalValueEntries.ActualOffset, "GlobalValueEntries", ViewKind.GlobalValueEntries);

                r.WriteValues(GlobalValueEntries.Value);
            }

            return s.ToArray();
        }
    }
}
