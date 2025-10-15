using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.LIB
{
    //Name is made up
    public class SecondLinkerMember : IValue, IViewable //Don't know if we can guarantee it will exist
    {
        private const int ArchiveHeaderOffset = 0;
        private const int NumberOfMembersOffset = ImageArchiveMemberHeader.StructSize;
        private const int OffsetsOffset = ImageArchiveMemberHeader.StructSize + 4;
        private int NumberOfSymbolsOffset => ImageArchiveMemberHeader.StructSize + 4 + (NumberOfMembers * 4);
        private int IndicesOffset => ImageArchiveMemberHeader.StructSize + 4 + (NumberOfMembers * 4) + 4;

        private readonly ImageArchiveMemberHeader archiveHeader;

        public ref readonly ImageArchiveMemberHeader ArchiveHeader => ref archiveHeader;

        public int NumberOfMembers => chunk.PeekInt32(NumberOfMembersOffset);

        public NativeSpan<int> Offsets => chunk.PeekNativeSpan<int>(OffsetsOffset, NumberOfMembers);

        public int NumberOfSymbols => chunk.PeekInt32(NumberOfSymbolsOffset);

        public NativeSpan<short> Indices => chunk.PeekNativeSpan<short>(IndicesOffset, NumberOfSymbols);

        public RawValue<AnsiString>[] StringTable { get; }

        public int Offset => chunk.AbsoluteOffset;

        public int StructSize => ImageArchiveMemberHeader.StructSize + ArchiveHeader.Size;

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        //It's more of a region but we want to have named children for stuff
        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.SecondLinkerMember, this, ViewKind.SecondLinkerMember, StructSize);

        int IViewable.NumChildren => 5 + StringTable.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteInline(ArchiveHeader);
                    break;

                case 1:
                    structWriter.WriteField("Number Of Members", NumberOfMembersOffset, NumberOfMembers);
                    break;

                case 2:
                    structWriter.WriteField("Offsets", OffsetsOffset, Offsets);
                    break;

                case 3:
                    structWriter.WriteField("Number Of Symbols", NumberOfSymbolsOffset, NumberOfSymbols);
                    break;

                case 4:
                    structWriter.WriteField("Indices", IndicesOffset, Indices);
                    break;

                case 5:
                    var i = index - 5;

                    structWriter.WriteInlineAnsiNullTerminated(StringTable[i]);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
