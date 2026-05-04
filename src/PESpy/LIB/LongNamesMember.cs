using PESpy.View;

namespace PESpy.LIB
{
    //Store all ImageArchiveMemberHeader.Name names longer than 16 bytes
    public class LongNamesMember : IValue, IViewable
    {
        private readonly ImageArchiveMemberHeader archiveHeader;

        public ImageArchiveMemberHeader ArchiveHeader => archiveHeader;

        public RawValue<AnsiString>[] Names { get; }

        public long Offset => chunk.AbsoluteOffset;

        public int StructSize => ImageArchiveMemberHeader.StructSize + ArchiveHeader.Size;

        private readonly MemoryChunk chunk;

        internal LongNamesMember(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            archiveHeader = new ImageArchiveMemberHeader(chunk);

            var read = ImageArchiveMemberHeader.StructSize;

            var size = archiveHeader.Size + ImageArchiveMemberHeader.StructSize;

            using var names = new PooledList<RawValue<AnsiString>>();

            while (read < size)
            {
                var str = chunk.PeekAnsiNullTerminatedString(read);
                names.Add(new RawValue<AnsiString>(chunk.AbsoluteOffset + read, str));
                read += str.Length + 1;
            }

            Names = names.ToArray();
        }

        public AnsiString GetName(int offset)
        {
            if (offset < archiveHeader.Size)
                return chunk.PeekAnsiNullTerminatedString(ImageArchiveMemberHeader.StructSize + offset);

            return default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.LongNamesMember, StructSize);

        int IViewable.NumChildren() => 1 + Names.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteInline(ArchiveHeader);
                    break;

                default:
                    var i = index - 1;

                    structWriter.WriteInlineAnsiNullTerminated(Names[i]);
                    break;
            }
        }
    }
}
