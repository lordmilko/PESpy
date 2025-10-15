using PESpy.View;

namespace PESpy.LIB
{
    //Name is made up
    public class FirstLinkerMember : IValue, IViewable //Don't know if we can guarantee it will exist
    {
        private const int ArchiveHeaderOffset = 0;
        private const int NumberOfSymbolsOffset = ImageArchiveMemberHeader.StructSize;
        private const int OffsetsOffset = ImageArchiveMemberHeader.StructSize + sizeof(int);

        private ImageArchiveMemberHeader archiveHeader;

        public ref readonly ImageArchiveMemberHeader ArchiveHeader => ref archiveHeader;

        public int NumberOfSymbols { get; }

        public int[] Offsets { get; }

        public RawValue<AnsiString>[] StringTable { get; }

        public int Offset { get; }

        public int StructSize => ImageArchiveMemberHeader.StructSize + ArchiveHeader.Size;

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        //It's more of a region but we want to have named children for stuff like the Number Of Symbols
        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FirstLinkerMember, this, ViewKind.FirstLinkerMember, StructSize);

        int IViewable.NumChildren => 3 + StringTable.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteInline(ArchiveHeader);
                    break;

                case 1:
                    structWriter.WriteField("Number Of Symbols", NumberOfSymbolsOffset, NumberOfSymbols);
                    break;

                case 2:
                    structWriter.WriteField("Offsets", OffsetsOffset, Offsets);
                    break;

                default:
                    var i = index - 3;

                    structWriter.WriteInlineAnsiNullTerminated(StringTable[i]);
                    break;
            }
        }
    }
}
