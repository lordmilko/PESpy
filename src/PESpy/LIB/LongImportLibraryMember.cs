using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using PESpy.View;

namespace PESpy.LIB
{
    //Name is made up
    public class LongImportLibraryMember : IImportLibraryMember, IValue, IViewable //Essentially, it's an obj file, and follows the same format
    {
        public AnsiString FileName { get; }

        public AnsiString SymbolName { get; }

        public ImageArchiveMemberHeader ArchiveHeader => new ImageArchiveMemberHeader(chunk);

        public bool IsLong => true;

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

                        //Immediately after the archive header is the file header so if the pointer is 0
                        //even if it says there is a size, there's nothing for us to read
                        if (section.PointerToRawData == 0)
                            continue;

                        var sectionChunk = chunk.Slice(section.PointerToRawData + ImageArchiveMemberHeader.StructSize);

                        results[i] = OBJFile.GetDataForSection(sectionChunk, section.Name, section.SizeOfRawData);
                    }

                    sectionData = results;
                }

                return sectionData;
            }
        }

        public IEnumerable<T> GetSectionData<T>(string name) where T : class
        {
            for (var i = 0; i < SectionHeaders.Length; i++)
            {
                if (SectionHeaders[i].Name == name)
                    yield return Unsafe.As<T>(SectionData[i]);
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly object c13SymbolMemoryLock = new object();
        private readonly HashSet<int> c13RegisteredSymbolMemory = new HashSet<int>();

        private readonly MemoryChunk chunk;

        internal unsafe LongImportLibraryMember(in MemoryChunk chunk, AnsiString fileName, Dictionary<int, AnsiString> symbolNameMap)
        {
            //Peek ImageArchiveMemberHeader.Size. The size does not include the size of the ImageArchiveMemberHeader itself
            var size = chunk.PeekSpacePaddedInt32(48, 10) + ImageArchiveMemberHeader.StructSize;

            var parent = (GlobalMemoryBlock) chunk.block;
            var block = new GlobalSubMemoryBlock(parent.LocalPointer + chunk.RelativeOffset, size, chunk.RelativeOffset, this, parent);

            this.chunk = new MemoryChunk(block, 0);
            FileName = fileName;

            //May not be present, e.g. you can have *.res files that don't have any symbol
            if (symbolNameMap.TryGetValue(Offset, out var symbolName))
                SymbolName = symbolName;
        }

        public unsafe void CopyTo(Span<byte> span) => new Span<byte>(chunk.Pointer, chunk.Remaining).CopyTo(span);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //Relocations need to know what our machine is, which they'll be able to get from the GlobalSubMemoryBlock of their chunk

            //These will be encapsulated inside a region by the merger
            writer.WriteGlobal(ArchiveHeader);
            writer.WriteGlobal(FileHeader);

            var sectionHeaders = SectionHeaders;

            writer.WriteGlobal(sectionHeaders);

            OBJFile.WriteGlobals(writer, sectionHeaders, SectionData);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) => null;

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

        internal ICodeViewAccessor? RegisterC13SymbolMemory(MemoryChunk dataChunk, ICodeViewModuleAccessor codeViewModuleAccessor)
        {
            lock (c13SymbolMemoryLock)
            {
                if (c13RegisteredSymbolMemory.Add(dataChunk.AbsoluteOffset))
                {
                    var codeViewAccessor = new LongImportLibraryMemberSymbolAccessor(this, false);

                    //We're being called from OBJSymbolsTable.C13SubSections which only runs when the signature is C13
                    SymbolMemoryTracker.RegisterCVSymbolMemory(chunk, codeViewAccessor, codeViewModuleAccessor);

                    return codeViewAccessor;
                }

                return null;
            }
        }

        public void SaveAs(string path)
        {
            File.WriteAllBytes(path, chunk.PeekNativeSpan<byte>(ImageArchiveMemberHeader.StructSize, chunk.Remaining - ImageArchiveMemberHeader.StructSize).ToArray());
        }

        public override string ToString()
        {
            if (SymbolName.Length == 0)
                return FileName.ToString();

            if (FileName.Length == 0)
                return SymbolName.ToString();

            return $"{FileName} -> {SymbolName}";
        }
    }
}
