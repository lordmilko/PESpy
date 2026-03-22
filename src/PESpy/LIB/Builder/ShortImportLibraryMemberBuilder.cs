using System;
using System.Collections.Generic;
using ClrDebug;
using PESpy.LIB;

namespace PESpy
{
    internal class ShortImportLibraryMemberBuilder : ImportLibraryMemberBuilder
    {
        public ImportObjectHeaderBuilder ImportHeader { get; set; }

        public string ImportName { get; set; }

        public string DllName { get; set; }

        public ShortImportLibraryMemberBuilder(ShortImportLibraryMember importLibrary, LIBFileBuilder libFileBuilder) : base(importLibrary.ArchiveHeader, libFileBuilder, new List<uint> { uint.MaxValue }, uint.MaxValue)
        {
        }

        internal ShortImportLibraryMemberBuilder(
            ShortImportLibraryMember importLibrary,
            LIBFileBuilder libFileBuilder,
            List<uint> firstLinkerIndex,
            uint secondLinkerIndex) : base(importLibrary.ArchiveHeader, libFileBuilder, firstLinkerIndex, secondLinkerIndex)
        {
            if (importLibrary.FileName.Length > 0)
                ArchiveHeader.Name = importLibrary.FileName.ToString();

            ImportHeader = new ImportObjectHeaderBuilder(importLibrary.ImportHeader);
            ImportName = importLibrary.ImportName.ToString();
            DllName = importLibrary.DllName.ToString();
        }

        public override void WriteTo(FileWriter writer, Dictionary<string, int> nameToOffsetMap)
        {
            writer.Skip(ImageArchiveMemberHeader.StructSize);

            //The size does not contain the size of the ImageArchiveMemberHeader
            var start = writer.Position;

            ImportHeader.WriteTo(writer);
            writer.WriteNullTerminatedAnsiString(ImportName);
            writer.WriteNullTerminatedAnsiString(DllName);

            var end = writer.Position;
            var size = writer.Position - start;

            writer.Seek(start - ImageArchiveMemberHeader.StructSize);

            //The size does not include the size of the ImageArchiveMemberHeader
            ArchiveHeader.WriteTo(writer, size, nameToOffsetMap);

            writer.Seek(end);
        }
    }
}
