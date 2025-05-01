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

        public int Offset { get; }

        public int Cookie { get; }
        public short MajorVersion { get; }
        public short MinorVersion { get; }
        public int Flags { get; }
        public int ReservedPadding1 { get; }

        public VA<DebugTypeEntry[]> DebugTypeEntries { get; }

        public VA<GlobalValueEntry[]> GlobalValueEntries { get; }

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

                    var debugTypeEntries = new List<DebugTypeEntry>();

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

                    var globalValueEntries = new List<GlobalValueEntry>();

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
                using var r = writer.CreateRegion(DebugTypeEntries.ActualOffset, "DebugTypeEntries", ViewKind.DebugTypeEntries);

                r.WriteValues(DebugTypeEntries.Value);
            }

            if (GlobalValueEntries.IsValid)
            {
                using var r = writer.CreateRegion(GlobalValueEntries.ActualOffset, "GlobalValueEntries", ViewKind.GlobalValueEntries);

                r.WriteValues(GlobalValueEntries.Value);
            }
        }
    }
}
