using System;
using System.Collections.Generic;
using PESpy.View;

namespace PESpy.LIB
{
    //Name is made up
    public class LongImportLibraryMember : IImportLibraryMember, IValue, IViewable //Essentially, it's an obj file, and follows the same format
    {
        public AnsiString Name { get; }

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

        private readonly object c13SymbolMemoryLock = new object();
        private readonly HashSet<int> c13RegisteredSymbolMemory = new HashSet<int>();

        private readonly MemoryChunk chunk;

        internal unsafe LongImportLibraryMember(in MemoryChunk chunk, AnsiString name)
        {
            //Peek ImageArchiveMemberHeader.Size. The size does not include the size of the ImageArchiveMemberHeader itself
            var size = chunk.PeekSpacePaddedInt32(48, 10) + ImageArchiveMemberHeader.StructSize;

            var parent = (GlobalMemoryBlock) chunk.block;
            var block = new GlobalSubMemoryBlock(parent.LocalPointer + chunk.RelativeOffset, size, chunk.RelativeOffset, this, parent);

            this.chunk = new MemoryChunk(block, 0);
            Name = name;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //Relocations need to know what our machine is, which they'll be able to get from the GlobalSubMemoryBlock of their chunk

            //These will be encapsulated inside a region by the merger
            writer.WriteGlobal(ArchiveHeader);
            writer.WriteGlobal(FileHeader);
            writer.WriteGlobal(SectionHeaders);

            foreach (var item in SectionData)
            {
                if (item != null)
                {
                    if (item is RawValue<FixedUtf8String> s)
                        writer.WriteGlobal(s.Offset, s.Value, s.Value.Length + 1, ViewKind.Value); //todo: use more specific view kind
                    else
                        writer.WriteGlobal((IViewable) item);
                }
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter) => throw new NotSupportedException();

        internal ISymbolAccessor? RegisterC13SymbolMemory(MemoryChunk dataChunk)
        {
            lock (c13SymbolMemoryLock)
            {
                if (c13RegisteredSymbolMemory.Add(dataChunk.AbsoluteOffset))
                {
                    var symbolAccessor = new LongImportLibraryMemberSymbolAccessor(this, false);

                    //We're being called from OBJSymbolsTable.C13SubSections which only runs when the signature is C13
                    SymbolMemoryTracker.RegisterCVSymbolMemory(chunk, symbolAccessor);

                    return symbolAccessor;
                }

                return null;
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
