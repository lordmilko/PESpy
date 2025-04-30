using PESpy.View;

﻿#if PEFAST
namespace PESpy.LIB
{
    //Name is made up
    public class LongImportLibraryMember : IImportLibraryMember, IValue, IViewable //Essentially, it's an obj file, and follows the same format
    {
        public ImageArchiveMemberHeader ArchiveHeader => new ImageArchiveMemberHeader(chunk);

        private ImageFileHeader? fileHeader;

        public ImageFileHeader FileHeader =>
            fileHeader ??= new ImageFileHeader(chunk.Slice(ImageArchiveMemberHeader.StructSize));

        private ImageSectionHeader[]? sectionHeaders;

        public ImageSectionHeader[] SectionHeaders
        {
            get
            {
                if (sectionHeaders == null)
                {
                    var results = new ImageSectionHeader[FileHeader.NumberOfSections];

                    for (var i = 0; i < FileHeader.NumberOfSections; i++)
                        results[i] = new ImageSectionHeader(chunk.Slice(ImageArchiveMemberHeader.StructSize + ImageFileHeader.StructSize + (i * ImageSectionHeader.StructSize)));

                    sectionHeaders = results;
                }

                return sectionHeaders;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal LongImportLibraryMember(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Import Library Member (Long)", this, ViewKind.LongImportLibraryMember);

            s.WriteInline(ArchiveHeader);
            s.WriteInline(FileHeader);
            s.WriteInline(SectionHeaders);
        }

        public override string ToString()
        {
            return ArchiveHeader.Name.ToString();
        }
    }
}
#endif
