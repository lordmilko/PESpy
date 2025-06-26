using System;
using PESpy.View;

#if PEFAST
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

        private IValue?[]? sectionData;

        public IValue?[] SectionData
        {
            get
            {
                if (sectionData == null)
                {
                    var sections = SectionHeaders;

                    var results = new IValue?[sections.Length];

                    for (var i = 0; i < results.Length; i++)
                    {
                        ref var section = ref sections[i];

                        var sectionChunk = chunk.Slice(section.PointerToRawData + ImageArchiveMemberHeader.StructSize);

                        results[i] = OBJFile.GetDataForSection(sectionChunk, section.Name, section.SizeOfRawData);
                    }

                    sectionData = results;
                }

                return sectionData;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal unsafe LongImportLibraryMember(in MemoryChunk chunk)
        {
            //Peek ImageArchiveMemberHeader.Size. The size does not include the size of the ImageArchiveMemberHeader itself
            var size = chunk.PeekSpacePaddedInt32(48, 10) + ImageArchiveMemberHeader.StructSize;

            var parent = (GlobalMemoryBlock) chunk.block;
            var block = new GlobalSubMemoryBlock(parent.LocalPointer + chunk.RelativeOffset, size, chunk.RelativeOffset, this, parent);

            this.chunk = new MemoryChunk(block, 0);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //Relocations need to know what our machine is, so we must temporarily set it
            var l = (LIBViewWriter) writer;
            l.Machine = FileHeader.Machine;

            //These will be encapsulated inside a region by the merger
            writer.WriteGlobal(ArchiveHeader);
            writer.WriteGlobal(FileHeader);
            writer.WriteGlobal(SectionHeaders);

            foreach (var item in SectionData)
            {
                if (item != null)
                    writer.WriteGlobal((IViewable) item);
            }

            l.Machine = default;
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();

        public override string ToString()
        {
            return ArchiveHeader.Name.ToString();
        }
    }
}
#endif
