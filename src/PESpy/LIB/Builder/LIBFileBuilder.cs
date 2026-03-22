using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PESpy.LIB;
using PESpy.Native;

namespace PESpy
{
    internal partial class LIBFileBuilder
    {
        /* - IMAGE_ARCHIVE_START !<arch>\n
         * - FirstLinkerMember
         * - SecondLinkerMember
         * - LongNamesMember
         * - ImportLibrary
         * 
         * Not sure where HybridMap member goes; not currently supported
         * 
         * FirstLinkerMember sorts the symbols by their physical offsets within the lib file. e.g. If the first symbol is located at 0x1000 and
         * the second symbol is located at 0x2000, the order will be 0x1000, 0x200
         * 
         * SecondLinkerMember sorts the symbols by their name. The Offsets member is still the same, but StringTable is now sorted by name
         * and is a parallel array with Indices, which then maps into Offsets to get the given name. The names in the StringTable seem to be sorted
         * a bit weird, but they're sorted by Ordinal, where each character is compared based on its numeric value. Thus, the way _ is sorted
         * is not what you'd expect
         * 
         * You can have multiple names that all point to the same offset
         */

        //I'm assuming that both the first and second linker member will have the same header
        public ImageArchiveMemberHeaderBuilder ArchiveHeader { get; }

        public bool HasSecondLinkerMember { get; set; }

        //Each builder may have multiple symbol names attach to it
        public ImportLibraryList ImportLibrary { get; }

        private Dictionary<string, ImportLibraryMemberBuilder> _nameToLibraryMap = new Dictionary<string, ImportLibraryMemberBuilder>();
        private Dictionary<string, uint> _nameToFirstLinkerOffsetIndexMap = new Dictionary<string, uint>();
        private Dictionary<string, uint> _nameToSecondLinkerOffsetIndexMap = new Dictionary<string, uint>();

        public LIBFileBuilder()
        {
        }

        public LIBFileBuilder(LIBFile libFile)
        {
            //SecondLinkerMember should contain the same symbols, just with extra info to sort by name
            var firstLinkerMember = libFile.FirstLinkerMember;

            ArchiveHeader = new ImageArchiveMemberHeaderBuilder(firstLinkerMember.ArchiveHeader, ArchiveMemberKind.FirstLinkerMember);
            HasSecondLinkerMember = libFile.SecondLinkerMember != null;

            //FirstLinkerMember orders offsets by module. We don't know what the modules are if the library is based on a def file,
            //so we'll just capture the original order so we can replay it when we get to the end

            var offsetToBuilderMap = new Dictionary<int, ImportLibraryMemberBuilder>();

            var firstLinkerOffsetMap = new Dictionary<int, List<uint>>();
            Dictionary<int, uint> secondLinkerOffsetMap = null;

            for (var i = 0; i < firstLinkerMember.Offsets.Length; i++)
            {
                var offset = firstLinkerMember.Offsets[i];
                _nameToFirstLinkerOffsetIndexMap[firstLinkerMember.StringTable[i].Value.ToString()] = (uint) i;

                if (!firstLinkerOffsetMap.TryGetValue(offset, out var list))
                {
                    list = new List<uint>();
                    firstLinkerOffsetMap[offset] = list;
                }

                list.Add((uint) i);
            }

            if (HasSecondLinkerMember)
            {
                //I don't understand what the order of the offsets in second linker member is. I thought it was supposed to be
                //sorted by name, but we've got cases where there's multiple series of items
                var secondLinkerMember = libFile.SecondLinkerMember;

                //In the second linker offset, each offset is only listed once, and then multiple name indices refer to that index

                secondLinkerOffsetMap = new Dictionary<int, uint>();

                for (var i = 0; i < secondLinkerMember.Offsets.Length; i++)
                {
                    var offset = secondLinkerMember.Offsets[i];
                    _nameToSecondLinkerOffsetIndexMap[secondLinkerMember.StringTable[i].Value.ToString()] = (uint) i;

                    secondLinkerOffsetMap.Add(offset, (uint) i);
                }
            }

            var importLibraries = new List<ImportLibraryMemberBuilder>();

            //The spec (https://learn.microsoft.com/en-us/windows/win32/debug/pe-format) says that the elements in FirstLinkerMember
            //must be arranged in ascending order. However, I've observed that actually this is not always the case. Symbols within
            //kernel32.dll can be out of order in the FirstLinkerMember. Everything appears to be correct within the SecondLinkerMember
            foreach (var importLibrary in libFile.ImportLibrary)
            {
                ImportLibraryMemberBuilder builder;

                var firstLinkerIndex = firstLinkerOffsetMap[importLibrary.Offset];
                var secondLinkerIndex = secondLinkerOffsetMap == null ? uint.MaxValue : secondLinkerOffsetMap[importLibrary.Offset];

                if (importLibrary.IsLong)
                    builder = new LongImportLibraryMemberBuilder((LongImportLibraryMember) importLibrary, this, firstLinkerIndex, secondLinkerIndex);
                else
                    builder = new ShortImportLibraryMemberBuilder((ShortImportLibraryMember) importLibrary, this, firstLinkerIndex, secondLinkerIndex);

                offsetToBuilderMap[importLibrary.Offset] = builder;
                importLibraries.Add(builder);
            }

            ImportLibrary = new ImportLibraryList(importLibraries, this);

            for (var i = 0; i < firstLinkerMember.NumberOfSymbols; i++)
            {
                var offset = firstLinkerMember.Offsets[i];
                var name = firstLinkerMember.StringTable[i];
                var builder = offsetToBuilderMap[offset];
                builder.Names.AddInternal(name.ToString());
            }
        }

        public void RemoveSymbol(string name)
        {
            //Remove any exact matches, or matches in the form _<name>@<number>

            //Do contains on this to also check for imports. This isn't really the safest thing as you could legitimately have a name containing an underscore
            var stdcallName = $"_{name}@";

            using var names = new PooledList<string>();

            foreach (var kv in _nameToLibraryMap)
            {
                if (kv.Key == name || kv.Key.Contains(stdcallName))
                {
                    //If it's a long import library member, all symbols should be removed
                    if (kv.Value is ShortImportLibraryMemberBuilder s)
                    {
                        throw new NotImplementedException();
                    }

                    names.Add(kv.Key);
                }
                else if (kv.Key.EndsWith("_EXPORTS"))
                {
                    //Not sure if it's guaranteed that this must be a long import library member
                    var coffSymbolTable = ((LongImportLibraryMemberBuilder) kv.Value).FileHeader.PointerToSymbolTable;

                    coffSymbolTable.Symbols.RemoveAll(s => s.Name == name || s.Name.Contains(stdcallName));
                    coffSymbolTable.Strings.RemoveAll(s => s == name || s.Contains(stdcallName));
                }
            }

            for (var i = 0; i < names.Count; i++)
            {
                var n = names[i];

                var importLibrary = _nameToLibraryMap[n];

                //Decrease the offsets of every symbol whose offset is after us
                if (importLibrary.SecondLinkerIndex != uint.MaxValue)
                {
                    foreach (var item in ImportLibrary)
                    {
                        if (item.SecondLinkerIndex != uint.MaxValue && item.SecondLinkerIndex > importLibrary.SecondLinkerIndex)
                            item.SecondLinkerIndex--;
                    }
                }

                //We don't need to update the index for FirstLinkerMember, because unlike with SecondLinkerMember we don't actually serialize it

                //I don't think we have to worry about checking alignment; that will be handled during serialization

                importLibrary.Names.Remove(n);

                if (importLibrary.Names.Count == 0)
                    ImportLibrary.Remove(importLibrary);
            }
        }

        public void WriteTo(Stream stream) => WriteTo(new FileWriter(stream));

        public void WriteTo(FileWriter writer)
        {
            writer.WriteFixedAnsiString(IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_START);

            var importLibraryOffsets = new int[ImportLibrary.Count];

            var firstLinkerStart = ReserveFirstLinkerMember(writer);
            var secondLinkerStart = ReserveSecondLinkerMember(writer);

            var nameToOffsetMap = WriteLongNamesMember(writer);
            var symbolOffsets = new List<(string name, int offset)>();
            WriteImportLibrary(writer, nameToOffsetMap, symbolOffsets);

            WriteFirstLinkerMember(writer, firstLinkerStart, symbolOffsets);
            WriteSecondLinkerMember(writer, secondLinkerStart, symbolOffsets);
        }

        private int ReserveFirstLinkerMember(FileWriter writer)
        {
            var start = writer.Position;

            var totalUsed =
                ImageArchiveMemberHeader.StructSize +
                sizeof(int) + //NumberOfSymbols
                (sizeof(int) * _nameToLibraryMap.Count); //Use the number of names, not the number offets; you can potentially have multiple names that point to the same offset, so we need to capture the fact a given record exists twice

            //Measure the length of all names

            foreach (var name in _nameToLibraryMap.Keys)
                totalUsed += name.Length + 1;

            writer.Skip(totalUsed);

            return start;
        }

        private int ReserveSecondLinkerMember(FileWriter writer)
        {
            if (!HasSecondLinkerMember)
                return -1;

            var start = writer.Position;

            var totalUsed =
                ImageArchiveMemberHeader.StructSize +
                sizeof(int) + //NumberOfMembers
                (sizeof(int) * ImportLibrary.Count) + //Offsets
                sizeof(int) + //NumberOfSymbols
                (sizeof(short) * _nameToLibraryMap.Count); //Indices

            foreach (var name in _nameToLibraryMap.Keys)
                totalUsed += name.Length + 1;

            writer.Skip(totalUsed);

            return start;
        }

        private void WriteFirstLinkerMember(
            FileWriter writer,
            int firstLinkerStart,
            List<(string name, int offset)> symbolOffsets)
        {
            writer.Seek(firstLinkerStart);

            writer.Skip(ImageArchiveMemberHeader.StructSize);

            writer.WriteBigEndianInt32(symbolOffsets.Count);

            //In the FirstLinkerMember, the offsets are sorted by "module". In the case
            //of a def file, we don't have that, so we need to capture the points where numbers
            //stopped ascending from the original FirstLinkerMember and sort based on those

            var offsets = new List<(int offset, string name, uint index)>();

            var globalOffset = 0;

            foreach (var importLibrary in ImportLibrary)
            {
                for (var i = 0; i < importLibrary.Names.Count; i++)
                {
                    var name = importLibrary.Names[i];
                    var symbolOffset = symbolOffsets[globalOffset];
                    Debug.Assert(symbolOffset.name == name);

                    var index = importLibrary.FirstLinkerIndex[i];

                    offsets.Add((symbolOffset.offset, name, index));

                    globalOffset++;
                }
            }

            offsets.Sort((a, b) => a.index.CompareTo(b.index));

            //todo: maybe we should have two groups: sort based on the filename and then within
            //that sort based on this synthetic module name?
            foreach (var item in offsets)
                writer.WriteBigEndianInt32(item.offset);

            foreach (var item in offsets)
                writer.WriteNullTerminatedAnsiString(item.name);

            var size = writer.Position - firstLinkerStart - ImageArchiveMemberHeader.StructSize;

            writer.Seek(firstLinkerStart);

            ArchiveHeader.WriteTo(writer, size, null);

            writer.Seek(firstLinkerStart);
        }

        private void WriteSecondLinkerMember(
            FileWriter writer,
            int secondLinkerStart,
            List<(string name, int offset)> symbolOffsets)
        {
            if (!HasSecondLinkerMember)
                return;

            writer.Seek(secondLinkerStart);

            writer.Skip(ImageArchiveMemberHeader.StructSize);

            var uniqueOffsets = new List<(int offset, string name, uint index)>();
            var allOffsets = new List<(int offset, string name, uint index)>();

            var globalOffset = 0;

            foreach (var importLibrary in ImportLibrary)
            {
                for (var i = 0; i < importLibrary.Names.Count; i++)
                {
                    var name = importLibrary.Names[i];
                    var symbolOffset = symbolOffsets[globalOffset];
                    Debug.Assert(symbolOffset.name == name);

                    if (i == 0)
                        uniqueOffsets.Add((symbolOffset.offset, name, importLibrary.SecondLinkerIndex));

                    allOffsets.Add((symbolOffset.offset, name, importLibrary.SecondLinkerIndex));

                    globalOffset++;
                }
            }

            uniqueOffsets.Sort((a, b) => a.index.CompareTo(b.index));
            allOffsets.Sort((a, b) => a.index.CompareTo(b.index));

            writer.WriteUInt32((uint) uniqueOffsets.Count); //NumberOfMembers

            //Offsets
            foreach (var item in uniqueOffsets)
                writer.WriteUInt32((uint) item.offset);

            writer.WriteUInt32((uint) symbolOffsets.Count); //NumberOfSymbols

            allOffsets.Sort((a, b) => StringComparer.Ordinal.Compare(a.name, b.name));

            foreach (var item in allOffsets)
                writer.WriteUInt16((ushort) (item.index + 1));

            //StringTable

            foreach (var item in allOffsets)
                writer.WriteNullTerminatedAnsiString(item.name);

            var size = writer.Position - secondLinkerStart - ImageArchiveMemberHeader.StructSize;

            writer.Seek(secondLinkerStart);

            //Both first linker member and second linker member use IMAGE_ARCHIVE_LINKER_MEMBER (single slash)
            ArchiveHeader.WriteTo(writer, size, null);
        }

        private Dictionary<string, int> WriteLongNamesMember(FileWriter writer)
        {
            //Skip over the archive header on the assumption we might need a long names member

            writer.Skip(ImageArchiveMemberHeader.StructSize);

            var start = writer.Position;

            var nameToOffsetMap = new Dictionary<string, int>();

            foreach (var importLibrary in ImportLibrary)
            {
                var name = importLibrary.ArchiveHeader.Name;

                if (name.Length > 15 && !nameToOffsetMap.ContainsKey(name)) //Names have to end with / so if it's greater than 15 it must be a long name
                {
                    nameToOffsetMap[name] = writer.Position - start;
                    writer.WriteNullTerminatedAnsiString(name);
                }
            }

            if (nameToOffsetMap.Count > 0)
            {
                //We need a long names member, so rewind and write the header

                var end = writer.Position;

                writer.Seek(start - ImageArchiveMemberHeader.StructSize);

                var size = end - start;

                var archiveHeader = new ImageArchiveMemberHeaderBuilder(ArchiveMemberKind.LongNamesMember)
                {
                    Name = IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_LONGNAMES_MEMBER,
                    Date = ArchiveHeader.Date,
                    UserID = ArchiveHeader.UserID,
                    GroupID = ArchiveHeader.GroupID,
                    Mode = ArchiveHeader.Mode,
                    DateMissing = ArchiveHeader.DateMissing,
                };

                archiveHeader.WriteTo(writer, size, null);

                //Seek back to the end
                writer.Seek(end);
            }
            else
            {
                writer.Seek(start - ImageArchiveMemberHeader.StructSize);
            }

            return nameToOffsetMap;
        }

        private void WriteImportLibrary(
            FileWriter writer,
            Dictionary<string, int> nameToOffsetMap,
            List<(string name, int offset)> symbolOffsets)
        {
            foreach (var importLibrary in ImportLibrary)
            {
                //https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#archive-member-headers
                //Each member header starts on the first even address after the end of the previous archive member, one byte '\n' (IMAGE_ARCHIVE_PAD)
                //may be inserted after an archive member to make the following member start on an even address.
                writer.Align(2, IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_PAD);

                foreach (var name in importLibrary.Names)
                    symbolOffsets.Add((name, writer.Position));

                importLibrary.WriteTo(writer, nameToOffsetMap);
            }
        }
    }
}
