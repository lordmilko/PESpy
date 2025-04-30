using PESpy.View;

namespace PESpy.LIB
{
    //Name is made up
    public class FirstLinkerMember : IValue, IViewable //Don't know if we can guarantee it will exist
    {
        private ImageArchiveMemberHeader archiveHeader;

        public ref readonly ImageArchiveMemberHeader ArchiveHeader => ref archiveHeader;

        public int NumberOfSymbols { get; }

        public int[] Offsets { get; }

        public RawValue<AnsiString>[] StringTable { get; }

        public int Offset { get; }

        internal FirstLinkerMember(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;
            archiveHeader = new ImageArchiveMemberHeader(chunk);

            var read = ImageArchiveMemberHeader.StructSize;

            var numberOfSymbols = chunk.PeekBigEndianInt32(read);
            NumberOfSymbols = numberOfSymbols;
            read += 4;

            var offsets = new int[numberOfSymbols];

            for (var i = 0; i < numberOfSymbols; i++)
                offsets[i] = chunk.PeekBigEndianInt32(read + (i * 4));

            Offsets = offsets;

            read += (numberOfSymbols * 4);

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
            //It's more of a region but we want to have named children for stuff like the Number Of Symbols
            using var s = writer.CreateStruct("First Linker Member", this, ViewKind.FirstLinkerMember);

            s.WriteInline(ArchiveHeader);
            s.WriteField("Number Of Symbols", NumberOfSymbols);
            s.WriteField("Offsets", Offsets);

            foreach (var value in StringTable)
                s.WriteInlineAnsiNullTerminated(value);
        }
    }
}
