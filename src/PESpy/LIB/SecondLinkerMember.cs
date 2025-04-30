using System;
using PESpy.View;

namespace PESpy.LIB
{
    //Name is made up
    public class SecondLinkerMember : IValue, IViewable //Don't know if we can guarantee it will exist
    {
        private readonly ImageArchiveMemberHeader archiveHeader;

        public ref readonly ImageArchiveMemberHeader ArchiveHeader => ref archiveHeader;

        public int NumberOfMembers => chunk.PeekInt32(ImageArchiveMemberHeader.StructSize);

        public Span<int> Offsets => chunk.PeekSpan<int>(ImageArchiveMemberHeader.StructSize + 4, NumberOfMembers);

        public int NumberOfSymbols => chunk.PeekInt32(ImageArchiveMemberHeader.StructSize + 4 + (NumberOfMembers * 4));

        public Span<short> Indices => chunk.PeekSpan<short>(ImageArchiveMemberHeader.StructSize + 4 + (NumberOfMembers * 4) + 4, NumberOfSymbols);

        public RawValue<AnsiString>[] StringTable { get; }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal SecondLinkerMember(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            archiveHeader = new ImageArchiveMemberHeader(chunk);

            var numberOfSymbols = NumberOfSymbols;

            var read = ImageArchiveMemberHeader.StructSize + 4 + (NumberOfMembers * 4) + 4 + (numberOfSymbols * 2);

            var stringTable = new RawValue<AnsiString>[numberOfSymbols];

            for (var i = 0; i < numberOfSymbols; i++)
            {
                var str = chunk.PeekAnsiNullTerminatedString(read);
                stringTable[i] = new RawValue<AnsiString>(chunk.AbsoluteOffset + read, str);
                read += str.Length + 1;
            }

            StringTable = stringTable;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            //It's more of a region but we want to have named children for stuff
            using var s = writer.CreateStruct("Second Linker Member", this, ViewKind.FirstLinkerMember);

            s.WriteInline(ArchiveHeader);
            s.WriteField("Number Of Members", NumberOfMembers);
            s.WriteField("Offsets", Offsets);
            s.WriteField("Number Of Symbols", NumberOfSymbols);
            s.WriteField("Indices", Indices);

            foreach (var value in StringTable)
                s.WriteInlineAnsiNullTerminated(value);
        }
    }
}
