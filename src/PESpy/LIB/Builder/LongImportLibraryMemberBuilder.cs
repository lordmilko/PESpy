using System;
using System.Collections.Generic;
using System.Linq;
using PESpy.LIB;
using PESpy.OBJ;

namespace PESpy
{
    internal class LongImportLibraryMemberBuilder : ImportLibraryMemberBuilder
    {
        public ImageFileHeaderBuilder FileHeader { get; }

        //Also contains the data
        public ImageSectionHeaderBuilder[] SectionHeaders { get; }

        public bool IsBad => _badBytes != null;

        private byte[] _badBytes;

        public LongImportLibraryMemberBuilder(LongImportLibraryMember importLibrary, LIBFileBuilder libFileBuilder) : this(importLibrary, libFileBuilder, new List<uint> { uint.MaxValue }, uint.MaxValue)
        {
        }

        internal LongImportLibraryMemberBuilder(LongImportLibraryMember importLibrary, LIBFileBuilder libFileBuilder, List<uint> firstLinkerIndex, uint secondLinkerIndex) : base(importLibrary.ArchiveHeader, libFileBuilder, firstLinkerIndex, secondLinkerIndex)
        {
            if (importLibrary.FileName.Length > 0)
                ArchiveHeader.Name = importLibrary.FileName.ToString();

            FileHeader = new ImageFileHeaderBuilder(importLibrary.FileHeader);

            var sectionHeaders = new ImageSectionHeaderBuilder[importLibrary.SectionHeaders.Length];

            for (var i = 0; i < sectionHeaders.Length; i++)
            {
                sectionHeaders[i] = new ImageSectionHeaderBuilder(importLibrary.SectionHeaders[i], importLibrary.SectionData[i]);
            }

            SectionHeaders = sectionHeaders;

            //In lib files from Windows 3.1, you can have junk data in the first member e.g. KERNEL32_IMPORT_DESCRIPTOR. The sections don't properly point to their data, which messes up our ability to rewrite the original file properly.
            //As such, we'll say if we're an _IMPORT_DESCRIPTOR member which has any section headers that don't start with a ".", we're junk and will just replay the original bytes when we go to reserialize the member

            if (importLibrary.SymbolName.EndsWith("_IMPORT_DESCRIPTOR") && sectionHeaders.Any(s => !s.Name.StartsWith(".")))
            {
                _badBytes = new byte[importLibrary.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize];
                importLibrary.CopyTo(_badBytes);
            }
        }

        public override void WriteTo(FileWriter writer, Dictionary<string, int> nameToOffsetMap)
        {
            if (_badBytes != null)
            {
                writer.WriteBytes(_badBytes);
                return;
            }

            writer.Skip(ImageArchiveMemberHeader.StructSize);

            //The size does not contain the size of the ImageArchiveMemberHeader
            var start = writer.Position;

            //Collect the total number of symbols and aux symbols
            var numSymbols = FileHeader.PointerToSymbolTable.Symbols.Count;

            foreach (var symbol in FileHeader.PointerToSymbolTable.Symbols)
                numSymbols += symbol.AuxSymbols.Count;

            var fileHeaderStart = writer.Position;
            FileHeader.WriteTo(writer, numSymbols);

            var sectionHeaderOffsets = new int[SectionHeaders.Length];

            //Start by just writing the section headers
            for (var i = 0; i < SectionHeaders.Length; i++)
            {
                var sectionHeader = SectionHeaders[i];
                sectionHeaderOffsets[i] = writer.Position;

                sectionHeader.WriteTo(writer);
            }

            //Now write the section's data, followed by any relocations
            //I guess line numbers would then follow if we had any
            for (var i = 0; i < SectionHeaders.Length; i++)
            {
                var sectionHeader = SectionHeaders[i];

                var sectionData = sectionHeader.SectionData;

                if (sectionData is OBJSymbolsTable t)
                {
                    //todo: temp
                    var bytes = new byte[t.Length];
                    t.CopyTo(bytes);
                    writer.WriteBytes(bytes);
                }
                else if (sectionData is RawValue<NativeSpan<byte>> b)
                {
                    //temp
                    writer.WriteBytes(b.Value.ToArray());
                }
                else if (sectionData is RawValue<FixedUtf8String> u)
                {
                    writer.WriteFixedUtf8String(u.Value.AsSpan());
                }
                else if (sectionData == null)
                    continue;
                else
                    throw new NotImplementedException();

                if (sectionHeader.PointerToRelocations.Count > 0)
                {
                    var pointerToRelocations = writer.Position - start;

                    foreach (var imageRelocation in sectionHeader.PointerToRelocations)
                        imageRelocation.WriteTo(writer);

                    writer.WriteUInt32At((uint) pointerToRelocations, sectionHeaderOffsets[i] + 24);
                }

                if (sectionHeader.PointerToLineNumbers.Count > 0)
                    throw new NotImplementedException();
            }

            //At the very end, if there was a symbol table, write that
            if (FileHeader.PointerToSymbolTable != null)
            {
                var pointerToStringTable = (uint) (writer.Position - start);

                writer.WriteUInt32At(value: pointerToStringTable, position: fileHeaderStart + 8);

                FileHeader.PointerToSymbolTable.WriteTo(writer, numSymbols);
            }

            var end = writer.Position;
            var size = end - start;

            writer.Seek(start - ImageArchiveMemberHeader.StructSize);

            //The size does not include the size of the ImageArchiveMemberHeader
            ArchiveHeader.WriteTo(writer, size, nameToOffsetMap);

            writer.Seek(end);
        }
    }
}
